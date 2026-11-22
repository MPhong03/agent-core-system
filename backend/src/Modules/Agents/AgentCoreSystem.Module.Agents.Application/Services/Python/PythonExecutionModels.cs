// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;

namespace AgentCoreSystem.Module.Agents.Application.Services.Python
{
    /// <summary>
    /// Yêu cầu thực thi script Python trong môi trường ảo sandbox của Agent.
    /// </summary>
    public record PythonExecutionRequest(
        Guid AgentProfileId,
        string ScriptContent,
        string? ScriptName = null,
        int? TimeoutSeconds = null,
        IReadOnlyDictionary<string, string>? ScriptArguments = null
    );

    /// <summary>
    /// Kết quả thực thi script Python sau khi hoàn tất hoặc timeout.
    /// </summary>
    public record PythonExecutionResult(
        bool IsSuccess,
        int ExitCode,
        string Stdout,
        string Stderr,
        long DurationMs,
        bool IsTimeout,
        IReadOnlyList<ArtifactFileDto> GeneratedArtifacts,
        string? ErrorMessage = null
    );
}
