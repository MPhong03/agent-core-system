// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Module.Agents.Application.Workspace.Commands;
using AgentCoreSystem.Module.Agents.Application.Workspace.Queries;
using AgentCoreSystem.Module.Agents.Domain.Enums;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using MediatR;

namespace AgentCoreSystem.Module.Agents.Application.Services.Tools
{
    /// <summary>
    /// Định nghĩa và quản trị các công cụ hệ thống tích hợp sẵn (System Built-In Tools) như Python Execution, Package Manager.
    /// </summary>
    public static class SystemBuiltInTools
    {
        public const string ExecutePythonCodeToolName = "execute_python_code";
        public const string InstallPythonPackagesToolName = "install_python_packages";
        public const string ListWorkspaceFilesToolName = "list_workspace_files";

        public static readonly Guid ExecutePythonCodeId = new("019349c7-0000-7000-8000-000000000001");
        public static readonly Guid InstallPythonPackagesId = new("019349c7-0000-7000-8000-000000000002");
        public static readonly Guid ListWorkspaceFilesId = new("019349c7-0000-7000-8000-000000000003");

        private static readonly List<ToolDefinitionDto> BuiltInTools =
        [
            new ToolDefinitionDto(
                Id: ExecutePythonCodeId,
                Name: ExecutePythonCodeToolName,
                DisplayName: "Thực thi mã nguồn Python",
                Description: "Execute Python code inside your isolated virtual environment sandbox. Use this to calculate complex numbers, parse files, generate data charts, or run algorithms. Files saved in 'artifacts/' directory will be preserved and delivered to the user.",
                Type: ToolType.RestApi,
                EndpointUrl: "system://python/execute",
                HttpMethod: "POST",
                HeadersConfig: "{}",
                ParametersSchema: @"{
  ""type"": ""object"",
  ""properties"": {
    ""code"": { ""type"": ""string"", ""description"": ""The complete, executable Python code script"" },
    ""script_name"": { ""type"": ""string"", ""description"": ""Optional name of the script file, e.g. analyze_sales.py"" },
    ""timeout_seconds"": { ""type"": ""integer"", ""description"": ""Optional execution timeout in seconds (default 60, max 300)"" }
  },
  ""required"": [""code""]
}",
                RequiresConfirmation: true,
                TimeoutSeconds: 60,
                IsActive: true,
                CreatedDate: DateTimeOffset.UtcNow,
                UpdatedDate: null
            ),
            new ToolDefinitionDto(
                Id: InstallPythonPackagesId,
                Name: InstallPythonPackagesToolName,
                DisplayName: "Cài đặt thư viện Python (pip install)",
                Description: "Install required Python libraries using pip into your isolated virtual environment.",
                Type: ToolType.RestApi,
                EndpointUrl: "system://python/install-packages",
                HttpMethod: "POST",
                HeadersConfig: "{}",
                ParametersSchema: @"{
  ""type"": ""object"",
  ""properties"": {
    ""packages"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" },
      ""description"": ""List of package names to install, e.g. ['pandas', 'matplotlib', 'requests']""
    }
  },
  ""required"": [""packages""]
}",
                RequiresConfirmation: true,
                TimeoutSeconds: 120,
                IsActive: true,
                CreatedDate: DateTimeOffset.UtcNow,
                UpdatedDate: null
            ),
            new ToolDefinitionDto(
                Id: ListWorkspaceFilesId,
                Name: ListWorkspaceFilesToolName,
                DisplayName: "Liệt kê tệp tin Workspace",
                Description: "List files available in your workspace (scripts, artifacts, logs).",
                Type: ToolType.RestApi,
                EndpointUrl: "system://workspace/files",
                HttpMethod: "GET",
                HeadersConfig: "{}",
                ParametersSchema: @"{
  ""type"": ""object"",
  ""properties"": {
    ""category"": {
      ""type"": ""string"",
      ""enum"": [""scripts"", ""artifacts"", ""logs"", ""all""],
      ""description"": ""Optional folder category filter""
    }
  }
}",
                RequiresConfirmation: false,
                TimeoutSeconds: 15,
                IsActive: true,
                CreatedDate: DateTimeOffset.UtcNow,
                UpdatedDate: null
            )
        ];

        /// <summary>
        /// Kiểm tra tên công cụ có phải là System Built-In Tool hay không.
        /// </summary>
        public static bool IsBuiltInTool(string toolName)
        {
            return BuiltInTools.Any(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Lấy toàn bộ danh sách System Built-In Tools.
        /// </summary>
        public static IReadOnlyList<ToolDefinitionDto> GetAll()
        {
            return BuiltInTools;
        }

        /// <summary>
        /// Tìm kiếm System Built-In Tool theo tên định danh.
        /// </summary>
        public static ToolDefinitionDto? GetByName(string toolName)
        {
            return BuiltInTools.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Lệnh thực thi công cụ hệ thống tích hợp sẵn (execute_python_code, install_python_packages, list_workspace_files).
    /// </summary>
    public record ExecuteSystemToolCommand(
        Guid AgentProfileId,
        string ToolName,
        string? ArgumentsJson,
        string CallerType = "Chat",
        Guid? ExecutionSourceId = null) : ICommand<string>;

    /// <summary>
    /// Handler xử lý thực thi System Built-In Tool.
    /// </summary>
    public class ExecuteSystemToolCommandHandler(ISender sender) : ICommandHandler<ExecuteSystemToolCommand, string>
    {
        /// <inheritdoc />
        public async Task<Result<string>> Handle(
            ExecuteSystemToolCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ToolName.Equals(SystemBuiltInTools.ExecutePythonCodeToolName, StringComparison.OrdinalIgnoreCase))
            {
                string code = string.Empty;
                string? scriptName = null;
                int? timeoutSeconds = null;

                if (!string.IsNullOrWhiteSpace(request.ArgumentsJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(request.ArgumentsJson);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("code", out var codeElem))
                        {
                            code = codeElem.GetString() ?? string.Empty;
                        }
                        if (root.TryGetProperty("script_name", out var scriptElem))
                        {
                            scriptName = scriptElem.GetString();
                        }
                        if (root.TryGetProperty("timeout_seconds", out var timeoutElem) && timeoutElem.TryGetInt32(out var tVal))
                        {
                            timeoutSeconds = tVal;
                        }
                    }
                    catch
                    {
                        // Fallback parsing failure
                    }
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    return Result.Failure<string>(Error.Validation("Python.EmptyCode", "Mã nguồn Python không được để trống."));
                }

                var execResult = await sender.Send(new ExecuteScriptCommand(
                    AgentProfileId: request.AgentProfileId,
                    ScriptContent: code,
                    ScriptName: scriptName,
                    TimeoutSeconds: timeoutSeconds,
                    CallerType: request.CallerType,
                    ExecutionSourceId: request.ExecutionSourceId
                ), cancellationToken);

                if (!execResult.IsSuccess)
                {
                    return Result.Failure<string>(execResult.Error);
                }

                var res = execResult.Value;
                var outputBuilder = new System.Text.StringBuilder();
                outputBuilder.AppendLine($"Exit Code: {res.ExitCode}");
                outputBuilder.AppendLine($"Duration: {res.DurationMs}ms");

                if (!string.IsNullOrWhiteSpace(res.Stdout))
                {
                    outputBuilder.AppendLine("--- STDOUT ---");
                    outputBuilder.AppendLine(res.Stdout.TrimEnd());
                }

                if (!string.IsNullOrWhiteSpace(res.Stderr))
                {
                    outputBuilder.AppendLine("--- STDERR ---");
                    outputBuilder.AppendLine(res.Stderr.TrimEnd());
                }

                if (res.GeneratedArtifacts.Count > 0)
                {
                    outputBuilder.AppendLine("--- ARTIFACTS GENERATED ---");
                    foreach (var a in res.GeneratedArtifacts)
                    {
                        outputBuilder.AppendLine($"- {a.FileName} ({a.SizeBytes} bytes, path: {a.RelativePath})");
                    }
                }

                return Result.Success(outputBuilder.ToString().TrimEnd());
            }

            if (request.ToolName.Equals(SystemBuiltInTools.InstallPythonPackagesToolName, StringComparison.OrdinalIgnoreCase))
            {
                var packages = new List<string>();
                if (!string.IsNullOrWhiteSpace(request.ArgumentsJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(request.ArgumentsJson);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("packages", out var pkgElem) && pkgElem.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in pkgElem.EnumerateArray())
                            {
                                if (item.GetString() is { } s && !string.IsNullOrWhiteSpace(s))
                                {
                                    packages.Add(s);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore parse error
                    }
                }

                var installResult = await sender.Send(new InstallPackagesCommand(request.AgentProfileId, packages), cancellationToken);
                return installResult;
            }

            if (request.ToolName.Equals(SystemBuiltInTools.ListWorkspaceFilesToolName, StringComparison.OrdinalIgnoreCase))
            {
                string? category = null;
                if (!string.IsNullOrWhiteSpace(request.ArgumentsJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(request.ArgumentsJson);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("category", out var catElem))
                        {
                            category = catElem.GetString();
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                }

                var filesResult = await sender.Send(new ListWorkspaceFilesQuery(request.AgentProfileId, category), cancellationToken);
                if (!filesResult.IsSuccess)
                {
                    return Result.Failure<string>(filesResult.Error);
                }

                var json = JsonSerializer.Serialize(filesResult.Value, new JsonSerializerOptions { WriteIndented = true });
                return Result.Success(json);
            }

            return Result.Failure<string>(Error.NotFound("Tool.NotSupported", $"Công cụ hệ thống '{request.ToolName}' không được hỗ trợ."));
        }
    }
}
