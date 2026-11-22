// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Services.Python;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.Extensions.Configuration;

namespace AgentCoreSystem.Module.Agents.Infrastructure.Services.Python
{
    /// <summary>
    /// Triển khai thực thi script Python và quản trị .venv với các biện pháp bảo vệ timeout và environment stripping.
    /// </summary>
    public class PythonVenvRunner(
        IWorkspaceManager workspaceManager,
        IConfiguration configuration) : IPythonSandboxRunner
    {
        private static readonly Regex PackageNameRegex = new(@"^[a-zA-Z0-9_\-\.\=\<\>\~\[\]\,]+$", RegexOptions.Compiled);

        private readonly string _hostPythonPath = !string.IsNullOrWhiteSpace(configuration["PythonSandbox:HostPythonPath"])
            ? configuration["PythonSandbox:HostPythonPath"]!
            : "python";

        /// <inheritdoc />
        public async Task<Result<string>> EnsureVenvInitializedAsync(Guid agentProfileId, CancellationToken ct = default)
        {
            await workspaceManager.EnsureWorkspaceDirectoriesAsync(agentProfileId, ct);
            var venvPath = workspaceManager.GetVenvPath(agentProfileId);
            var pythonExe = workspaceManager.GetPythonExecutablePath(agentProfileId);

            if (Directory.Exists(venvPath) && File.Exists(pythonExe))
            {
                return Result.Success(venvPath);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _hostPythonPath,
                Arguments = $"-m venv \"{venvPath}\"",
                WorkingDirectory = workspaceManager.GetAgentWorkspacePath(agentProfileId),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            ConfigureCleanEnvironment(startInfo, agentProfileId, isVenvReady: false);

            using var process = new Process { StartInfo = startInfo };
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                process.Start();
                await process.WaitForExitAsync(linkedCts.Token);

                if (process.ExitCode != 0)
                {
                    var stderr = await process.StandardError.ReadToEndAsync(ct);
                    return Result.Failure<string>(new Error("Python.VenvInitFailed", $"Khởi tạo môi trường ảo thất bại: {stderr}"));
                }

                return Result.Success(venvPath);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return Result.Failure<string>(new Error("Python.VenvTimeout", "Quá thời gian khởi tạo môi trường ảo Python."));
            }
            catch (Exception ex)
            {
                return Result.Failure<string>(Error.Unexpected("Python.VenvError", $"Lỗi không mong muốn khi tạo venv: {ex.Message}"));
            }
        }

        /// <inheritdoc />
        public async Task<Result<string>> InstallPackagesAsync(Guid agentProfileId, IEnumerable<string> packages, CancellationToken ct = default)
        {
            var packageList = packages.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
            if (packageList.Count == 0)
            {
                return Result.Success("Không có thư viện nào được yêu cầu cài đặt.");
            }

            foreach (var pkg in packageList)
            {
                if (!PackageNameRegex.IsMatch(pkg))
                {
                    return Result.Failure<string>(Error.Validation("Python.InvalidPackageName", $"Tên package '{pkg}' không hợp lệ hoặc chứa ký tự nguy hiểm."));
                }
            }

            await EnsureVenvInitializedAsync(agentProfileId, ct);
            var pythonExe = GetEffectivePythonExe(agentProfileId);

            var arguments = $"-m pip install {string.Join(" ", packageList)}";
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = arguments,
                WorkingDirectory = workspaceManager.GetAgentWorkspacePath(agentProfileId),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            ConfigureCleanEnvironment(startInfo, agentProfileId, isVenvReady: true);

            using var process = new Process { StartInfo = startInfo };
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(180));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                process.Start();
                var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
                var stderrTask = process.StandardError.ReadToEndAsync(ct);

                await process.WaitForExitAsync(linkedCts.Token);
                var stdout = await stdoutTask;
                var stderr = await stderrTask;

                if (process.ExitCode != 0)
                {
                    return Result.Failure<string>(new Error("Python.PipInstallFailed", $"Lỗi cài đặt package: {stderr}"));
                }

                return Result.Success(stdout);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return Result.Failure<string>(new Error("Python.PipTimeout", "Quá thời gian thực hiện pip install."));
            }
        }

        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<string>>> ListInstalledPackagesAsync(Guid agentProfileId, CancellationToken ct = default)
        {
            var pythonExe = GetEffectivePythonExe(agentProfileId);
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = "-m pip list --format=freeze",
                WorkingDirectory = workspaceManager.GetAgentWorkspacePath(agentProfileId),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            ConfigureCleanEnvironment(startInfo, agentProfileId, isVenvReady: true);

            using var process = new Process { StartInfo = startInfo };
            try
            {
                process.Start();
                var stdout = await process.StandardOutput.ReadToEndAsync(ct);
                await process.WaitForExitAsync(ct);

                var lines = stdout.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                return Result.Success<IReadOnlyList<string>>(lines);
            }
            catch (Exception ex)
            {
                return Result.Failure<IReadOnlyList<string>>(Error.Unexpected("Python.PipListFailed", $"Không thể đọc danh sách packages: {ex.Message}"));
            }
        }

        /// <inheritdoc />
        public async Task<Result<PythonExecutionResult>> ExecuteScriptAsync(PythonExecutionRequest request, CancellationToken ct = default)
        {
            await workspaceManager.EnsureWorkspaceDirectoriesAsync(request.AgentProfileId, ct);

            // 1. Ghi file script vào thư mục scripts/
            var scriptName = !string.IsNullOrWhiteSpace(request.ScriptName)
                ? request.ScriptName
                : $"exec_{DateTime.UtcNow:yyyyMMdd_HHmmss}.py";

            var writeScriptResult = await workspaceManager.WriteScriptFileAsync(request.AgentProfileId, scriptName, request.ScriptContent, ct);
            if (!writeScriptResult.IsSuccess)
            {
                return Result.Failure<PythonExecutionResult>(writeScriptResult.Error);
            }

            var actualScriptName = writeScriptResult.Value;
            var scriptFullPath = Path.Combine(workspaceManager.GetScriptsPath(request.AgentProfileId), actualScriptName);

            // 2. Snapshot danh sách artifact hiện có trước khi chạy
            var artifactsDir = workspaceManager.GetArtifactsPath(request.AgentProfileId);
            Directory.CreateDirectory(artifactsDir);
            var initialArtifacts = Directory.GetFiles(artifactsDir)
                .Select(Path.GetFileName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 3. Chuẩn bị ProcessStartInfo với Environment Stripping
            var pythonExe = GetEffectivePythonExe(request.AgentProfileId);
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptFullPath}\"",
                WorkingDirectory = workspaceManager.GetAgentWorkspacePath(request.AgentProfileId),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var isVenvReady = File.Exists(workspaceManager.GetPythonExecutablePath(request.AgentProfileId));
            ConfigureCleanEnvironment(startInfo, request.AgentProfileId, isVenvReady);

            // 4. Thực thi tiến trình kèm Timeout Guard
            var timeoutSeconds = Math.Clamp(request.TimeoutSeconds ?? 60, 1, 300);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var stopwatch = Stopwatch.StartNew();
            using var process = new Process { StartInfo = startInfo };

            string stdout = string.Empty;
            string stderr = string.Empty;
            bool isTimeout = false;
            int exitCode = -1;

            try
            {
                process.Start();

                var stdoutTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
                var stderrTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

                await process.WaitForExitAsync(linkedCts.Token);
                stopwatch.Stop();

                exitCode = process.ExitCode;
                stdout = await stdoutTask;
                stderr = await stderrTask;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                isTimeout = timeoutCts.IsCancellationRequested;
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Ignore kill failure
                }

                exitCode = -1;
                stderr = isTimeout
                    ? $"[AgentCoreSystem] Quá thời gian thực thi tối đa ({timeoutSeconds}s). Tiến trình đã bị ngắt an toàn."
                    : "[AgentCoreSystem] Tiến trình bị hủy bởi người dùng hoặc hệ thống.";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                exitCode = -1;
                stderr = $"[AgentCoreSystem] Lỗi khởi chạy tiến trình: {ex.Message}";
            }

            // 5. So sánh snapshot để tìm artifacts mới sinh ra
            var generatedArtifacts = new List<ArtifactFileDto>();
            if (Directory.Exists(artifactsDir))
            {
                var currentFiles = Directory.GetFiles(artifactsDir);
                foreach (var file in currentFiles)
                {
                    var fileName = Path.GetFileName(file);
                    if (!initialArtifacts.Contains(fileName))
                    {
                        var fileInfo = new FileInfo(file);
                        var relativePath = $"artifacts/{fileName}";
                        generatedArtifacts.Add(new ArtifactFileDto(fileName, fileInfo.Length, relativePath));
                    }
                }
            }

            // 6. Ghi nhật ký thực thi vào thư mục logs/
            try
            {
                var logsDir = workspaceManager.GetLogsPath(request.AgentProfileId);
                Directory.CreateDirectory(logsDir);
                var logFile = Path.Combine(logsDir, $"exec_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{actualScriptName}.log");
                var logContent = $"Script: {actualScriptName}\nExitCode: {exitCode}\nDuration: {stopwatch.ElapsedMilliseconds}ms\nIsTimeout: {isTimeout}\nArtifacts: {generatedArtifacts.Count}\n--- STDOUT ---\n{stdout}\n--- STDERR ---\n{stderr}";
                await File.WriteAllTextAsync(logFile, logContent, CancellationToken.None);
            }
            catch
            {
                // Ignore log write failures to protect main response
            }

            var executionResult = new PythonExecutionResult(
                IsSuccess: exitCode == 0 && !isTimeout,
                ExitCode: exitCode,
                Stdout: stdout,
                Stderr: stderr,
                DurationMs: stopwatch.ElapsedMilliseconds,
                IsTimeout: isTimeout,
                GeneratedArtifacts: generatedArtifacts,
                ErrorMessage: exitCode != 0 ? stderr : null
            );

            return Result.Success(executionResult);
        }

        private string GetEffectivePythonExe(Guid agentProfileId)
        {
            var venvExe = workspaceManager.GetPythonExecutablePath(agentProfileId);
            return File.Exists(venvExe) ? venvExe : _hostPythonPath;
        }

        private void ConfigureCleanEnvironment(ProcessStartInfo startInfo, Guid agentProfileId, bool isVenvReady)
        {
            startInfo.Environment.Clear();

            // Danh sách biến môi trường an toàn cần thiết của OS
            var safeVars = new[]
            {
                "SYSTEMROOT",
                "SYSTEMDRIVE",
                "HOMEDRIVE",
                "HOMEPATH",
                "USERPROFILE",
                "TMP",
                "TEMP"
            };

            foreach (var key in safeVars)
            {
                var val = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrWhiteSpace(val))
                {
                    startInfo.Environment[key] = val;
                }
            }

            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (isVenvReady)
            {
                var venvPath = workspaceManager.GetVenvPath(agentProfileId);
                var venvScripts = OperatingSystem.IsWindows()
                    ? Path.Combine(venvPath, "Scripts")
                    : Path.Combine(venvPath, "bin");

                startInfo.Environment["VIRTUAL_ENV"] = venvPath;
                startInfo.Environment["PATH"] = $"{venvScripts}{Path.PathSeparator}{currentPath}";
            }
            else
            {
                startInfo.Environment["PATH"] = currentPath;
            }

            // Đảm bảo Python flush stdout/stderr tức thời
            startInfo.Environment["PYTHONUNBUFFERED"] = "1";
        }
    }
}
