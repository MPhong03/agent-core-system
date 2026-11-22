// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Application.Services.Python;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Workspace.Commands
{
    /// <summary>
    /// Lệnh thực thi script Python trong môi trường sandbox của Agent và lưu vết lịch sử.
    /// </summary>
    public record ExecuteScriptCommand(
        Guid AgentProfileId,
        string ScriptContent,
        string? ScriptName = null,
        int? TimeoutSeconds = null,
        string CallerType = "Manual",
        Guid? ExecutionSourceId = null) : ICommand<PythonExecutionResult>;

    /// <summary>
    /// Handler xử lý chạy script Python.
    /// </summary>
    public class ExecuteScriptCommandHandler(
        IAgentsDbContext dbContext,
        IPythonSandboxRunner pythonRunner,
        IWorkspaceManager workspaceManager) : ICommandHandler<ExecuteScriptCommand, PythonExecutionResult>
    {
        /// <inheritdoc />
        public async Task<Result<PythonExecutionResult>> Handle(
            ExecuteScriptCommand request,
            CancellationToken cancellationToken)
        {
            var executionRequest = new PythonExecutionRequest(
                AgentProfileId: request.AgentProfileId,
                ScriptContent: request.ScriptContent,
                ScriptName: request.ScriptName,
                TimeoutSeconds: request.TimeoutSeconds
            );

            var executionResult = await pythonRunner.ExecuteScriptAsync(executionRequest, cancellationToken);
            if (!executionResult.IsSuccess)
            {
                return executionResult;
            }

            var result = executionResult.Value;
            var artifactsJson = result.GeneratedArtifacts.Count > 0
                ? JsonSerializer.Serialize(result.GeneratedArtifacts)
                : null;

            var historyResult = ScriptExecutionHistory.Create(
                agentProfileId: request.AgentProfileId,
                callerType: request.CallerType,
                executionSourceId: request.ExecutionSourceId ?? request.AgentProfileId,
                scriptName: request.ScriptName ?? "unnamed_script.py",
                scriptContent: request.ScriptContent,
                exitCode: result.ExitCode,
                stdout: result.Stdout,
                stderr: result.Stderr,
                durationMs: result.DurationMs,
                isTimeout: result.IsTimeout,
                artifactsJson: artifactsJson
            );

            if (historyResult.IsSuccess)
            {
                dbContext.ScriptExecutionHistories.Add(historyResult.Value);
            }

            var config = await dbContext.AgentWorkspaceConfigs
                .FirstOrDefaultAsync(c => c.AgentProfileId == request.AgentProfileId, cancellationToken);

            if (config != null)
            {
                config.RecordActivity();
                var stats = await workspaceManager.GetWorkspaceStatsAsync(request.AgentProfileId, cancellationToken);
                if (stats.IsSuccess)
                {
                    config.UpdateStorageUsed(stats.Value.TotalSizeBytes);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return executionResult;
        }
    }
}
