// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AgentCoreSystem.Module.Agents.Domain.Entities;

namespace AgentCoreSystem.Module.Agents.Application.Workspace.DTOs
{
    /// <summary>
    /// Thông tin chi tiết cấu hình và trạng thái không gian làm việc của Agent.
    /// </summary>
    public record AgentWorkspaceDetailDto(
        Guid Id,
        Guid AgentProfileId,
        string RootPath,
        string? PythonVersion,
        int MaxTimeoutSeconds,
        bool IsVenvReady,
        bool IsActive,
        long StorageUsedBytes,
        int TotalFilesCount,
        int ScriptsCount,
        int ArtifactsCount,
        DateTime? LastActiveAt
    );

    /// <summary>
    /// Bản ghi lịch sử thực thi mã nguồn Python.
    /// </summary>
    public record ScriptExecutionHistoryDto(
        Guid Id,
        Guid AgentProfileId,
        string CallerType,
        Guid ExecutionSourceId,
        string ScriptName,
        string ScriptContent,
        int ExitCode,
        string? Stdout,
        string? Stderr,
        long DurationMs,
        bool IsTimeout,
        string? ArtifactsJson,
        DateTimeOffset CreatedDate
    )
    {
        public static ScriptExecutionHistoryDto FromEntity(ScriptExecutionHistory entity)
        {
            return new ScriptExecutionHistoryDto(
                entity.Id,
                entity.AgentProfileId,
                entity.CallerType,
                entity.ExecutionSourceId,
                entity.ScriptName,
                entity.ScriptContent,
                entity.ExitCode,
                entity.Stdout,
                entity.Stderr,
                entity.DurationMs,
                entity.IsTimeout,
                entity.ArtifactsJson,
                entity.CreatedDate
            );
        }
    }
}
