// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.Extensions.Configuration;

namespace AgentCoreSystem.Module.Agents.Infrastructure.Services.Workspace
{
    /// <summary>
    /// Triển khai dịch vụ quản lý Workspace, tệp tin và bảo vệ Path Traversal Guard cho Agent.
    /// </summary>
    public class WorkspaceManager(IConfiguration configuration) : IWorkspaceManager
    {
        private readonly string _rootPath = !string.IsNullOrWhiteSpace(configuration["Storage:WorkspacesRootPath"])
            ? Path.GetFullPath(configuration["Storage:WorkspacesRootPath"]!)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Storage", "Workspaces"));

        /// <inheritdoc />
        public string GetAgentWorkspacePath(Guid agentProfileId)
        {
            return Path.Combine(_rootPath, agentProfileId.ToString());
        }

        /// <inheritdoc />
        public string GetVenvPath(Guid agentProfileId)
        {
            return Path.Combine(GetAgentWorkspacePath(agentProfileId), ".venv");
        }

        /// <inheritdoc />
        public string GetPythonExecutablePath(Guid agentProfileId)
        {
            var venvPath = GetVenvPath(agentProfileId);
            return OperatingSystem.IsWindows()
                ? Path.Combine(venvPath, "Scripts", "python.exe")
                : Path.Combine(venvPath, "bin", "python");
        }

        /// <inheritdoc />
        public string GetScriptsPath(Guid agentProfileId)
        {
            return Path.Combine(GetAgentWorkspacePath(agentProfileId), "scripts");
        }

        /// <inheritdoc />
        public string GetArtifactsPath(Guid agentProfileId)
        {
            return Path.Combine(GetAgentWorkspacePath(agentProfileId), "artifacts");
        }

        /// <inheritdoc />
        public string GetLogsPath(Guid agentProfileId)
        {
            return Path.Combine(GetAgentWorkspacePath(agentProfileId), "logs");
        }

        /// <inheritdoc />
        public Task<Result> EnsureWorkspaceDirectoriesAsync(Guid agentProfileId, CancellationToken ct = default)
        {
            var workspacePath = GetAgentWorkspacePath(agentProfileId);
            Directory.CreateDirectory(workspacePath);
            Directory.CreateDirectory(GetScriptsPath(agentProfileId));
            Directory.CreateDirectory(GetArtifactsPath(agentProfileId));
            Directory.CreateDirectory(GetLogsPath(agentProfileId));

            return Task.FromResult(Result.Success());
        }

        /// <inheritdoc />
        public Task<Result<WorkspaceStatsDto>> GetWorkspaceStatsAsync(Guid agentProfileId, CancellationToken ct = default)
        {
            var workspacePath = GetAgentWorkspacePath(agentProfileId);
            if (!Directory.Exists(workspacePath))
            {
                return Task.FromResult(Result<WorkspaceStatsDto>.Success(new WorkspaceStatsDto(
                    agentProfileId,
                    workspacePath,
                    VenvExists: false,
                    TotalSizeBytes: 0,
                    TotalFilesCount: 0,
                    ScriptsCount: 0,
                    ArtifactsCount: 0
                )));
            }

            var venvPath = GetVenvPath(agentProfileId);
            var pythonExe = GetPythonExecutablePath(agentProfileId);
            var venvExists = Directory.Exists(venvPath) && File.Exists(pythonExe);

            var dirInfo = new DirectoryInfo(workspacePath);
            var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            var totalBytes = files.Sum(f => f.Length);

            var scriptsPath = GetScriptsPath(agentProfileId);
            var scriptsCount = Directory.Exists(scriptsPath)
                ? Directory.GetFiles(scriptsPath, "*", SearchOption.TopDirectoryOnly).Length
                : 0;

            var artifactsPath = GetArtifactsPath(agentProfileId);
            var artifactsCount = Directory.Exists(artifactsPath)
                ? Directory.GetFiles(artifactsPath, "*", SearchOption.TopDirectoryOnly).Length
                : 0;

            var stats = new WorkspaceStatsDto(
                agentProfileId,
                workspacePath,
                venvExists,
                totalBytes,
                files.Length,
                scriptsCount,
                artifactsCount
            );

            return Task.FromResult(Result<WorkspaceStatsDto>.Success(stats));
        }

        /// <inheritdoc />
        public Task<Result<IReadOnlyList<WorkspaceFileDto>>> ListFilesAsync(
            Guid agentProfileId,
            string? subFolder = null,
            CancellationToken ct = default)
        {
            var workspacePath = GetAgentWorkspacePath(agentProfileId);
            if (!Directory.Exists(workspacePath))
            {
                IReadOnlyList<WorkspaceFileDto> empty = Array.Empty<WorkspaceFileDto>();
                return Task.FromResult(Result<IReadOnlyList<WorkspaceFileDto>>.Success(empty));
            }

            string targetDir = workspacePath;
            if (!string.IsNullOrWhiteSpace(subFolder))
            {
                var validateSub = ValidateAndGetFullPath(workspacePath, subFolder);
                if (!validateSub.IsSuccess)
                {
                    return Task.FromResult(Result.Failure<IReadOnlyList<WorkspaceFileDto>>(validateSub.Error));
                }
                targetDir = validateSub.Value;
            }

            if (!Directory.Exists(targetDir))
            {
                IReadOnlyList<WorkspaceFileDto> empty = Array.Empty<WorkspaceFileDto>();
                return Task.FromResult(Result.Success<IReadOnlyList<WorkspaceFileDto>>(empty));
            }

            var dirInfo = new DirectoryInfo(targetDir);
            IReadOnlyList<WorkspaceFileDto> files = dirInfo.GetFiles("*", SearchOption.AllDirectories)
                .Where(f => !f.FullName.Contains(Path.DirectorySeparatorChar + ".venv" + Path.DirectorySeparatorChar))
                .Select(f =>
                {
                    var relPath = Path.GetRelativePath(workspacePath, f.FullName).Replace('\\', '/');
                    var category = relPath.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase) ? "scripts" :
                                   relPath.StartsWith("artifacts/", StringComparison.OrdinalIgnoreCase) ? "artifacts" :
                                   relPath.StartsWith("logs/", StringComparison.OrdinalIgnoreCase) ? "logs" : "other";

                    return new WorkspaceFileDto(
                        Name: f.Name,
                        RelativePath: relPath,
                        Category: category,
                        SizeBytes: f.Length,
                        LastModifiedUtc: f.LastWriteTimeUtc,
                        Extension: f.Extension
                    );
                })
                .OrderByDescending(f => f.LastModifiedUtc)
                .ToList();

            return Task.FromResult(Result.Success<IReadOnlyList<WorkspaceFileDto>>(files));
        }

        /// <inheritdoc />
        public Task<Result<Stream>> GetFileStreamAsync(
            Guid agentProfileId,
            string relativeFilePath,
            CancellationToken ct = default)
        {
            var workspacePath = GetAgentWorkspacePath(agentProfileId);
            var pathCheck = ValidateAndGetFullPath(workspacePath, relativeFilePath);
            if (!pathCheck.IsSuccess)
            {
                return Task.FromResult(Result.Failure<Stream>(pathCheck.Error));
            }

            var fullPath = pathCheck.Value;
            if (!File.Exists(fullPath))
            {
                return Task.FromResult(Result.Failure<Stream>(Error.NotFound("Workspace.FileNotFound", $"Không tìm thấy tệp tin '{relativeFilePath}'.")));
            }

            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(Result.Success<Stream>(stream));
        }

        /// <inheritdoc />
        public Task<Result> DeleteFileAsync(
            Guid agentProfileId,
            string relativeFilePath,
            CancellationToken ct = default)
        {
            var workspacePath = GetAgentWorkspacePath(agentProfileId);
            var pathCheck = ValidateAndGetFullPath(workspacePath, relativeFilePath);
            if (!pathCheck.IsSuccess)
            {
                return Task.FromResult(Result.Failure(pathCheck.Error));
            }

            var fullPath = pathCheck.Value;
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.FromResult(Result.Success());
        }

        /// <inheritdoc />
        public async Task<Result<string>> WriteScriptFileAsync(
            Guid agentProfileId,
            string scriptName,
            string scriptContent,
            CancellationToken ct = default)
        {
            var scriptsDir = GetScriptsPath(agentProfileId);
            Directory.CreateDirectory(scriptsDir);

            // Chặn path traversal trong scriptName
            var cleanName = Path.GetFileName(scriptName);
            if (string.IsNullOrWhiteSpace(cleanName))
            {
                cleanName = $"script_{DateTime.UtcNow:yyyyMMdd_HHmmss}.py";
            }
            if (!cleanName.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            {
                cleanName += ".py";
            }

            var fullPath = Path.Combine(scriptsDir, cleanName);
            await File.WriteAllTextAsync(fullPath, scriptContent, ct);

            return Result.Success(cleanName);
        }

        private static Result<string> ValidateAndGetFullPath(string baseWorkspace, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return Result.Failure<string>(Error.Validation("Workspace.InvalidPath", "Đường dẫn không hợp lệ."));
            }

            var normalizedBase = Path.GetFullPath(baseWorkspace).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(baseWorkspace, relativePath));

            if (!fullPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<string>(Error.Validation("Workspace.InvalidPath", "Đường dẫn không hợp lệ hoặc vượt ra ngoài không gian làm việc của Agent."));
            }

            return Result.Success(fullPath);
        }
    }
}
