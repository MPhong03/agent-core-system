// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace AgentCoreSystem.Module.Agents.Application.Services.Workspace
{
    /// <summary>
    /// Thống kê dung lượng và số lượng file trong không gian làm việc của Agent.
    /// </summary>
    public record WorkspaceStatsDto(
        Guid AgentProfileId,
        string RootPath,
        bool VenvExists,
        long TotalSizeBytes,
        int TotalFilesCount,
        int ScriptsCount,
        int ArtifactsCount
    );

    /// <summary>
    /// Thông tin chi tiết của một file trong workspace của Agent.
    /// </summary>
    public record WorkspaceFileDto(
        string Name,
        string RelativePath,
        string Category,
        long SizeBytes,
        DateTime LastModifiedUtc,
        string Extension
    );

    /// <summary>
    /// Đại diện sản phẩm đầu ra (artifact) do Agent tạo ra sau khi thực thi script.
    /// </summary>
    public record ArtifactFileDto(
        string FileName,
        long SizeBytes,
        string RelativePath
    );
}
