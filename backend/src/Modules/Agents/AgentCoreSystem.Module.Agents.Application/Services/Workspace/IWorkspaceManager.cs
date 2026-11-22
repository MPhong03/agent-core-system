// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Agents.Application.Services.Workspace
{
    /// <summary>
    /// Giao diện quản lý cấu trúc thư mục, đường dẫn và tệp tin trong Workspace cách ly của từng Agent.
    /// </summary>
    public interface IWorkspaceManager
    {
        /// <summary>
        /// Lấy đường dẫn thư mục gốc không gian làm việc của Agent: Storage/Workspaces/{AgentId}/.
        /// </summary>
        string GetAgentWorkspacePath(Guid agentProfileId);

        /// <summary>
        /// Lấy đường dẫn thư mục môi trường ảo Python: Storage/Workspaces/{AgentId}/.venv/.
        /// </summary>
        string GetVenvPath(Guid agentProfileId);

        /// <summary>
        /// Lấy đường dẫn file nhị phân Python trong môi trường ảo (.venv/Scripts/python.exe trên Windows hoặc .venv/bin/python trên Linux).
        /// </summary>
        string GetPythonExecutablePath(Guid agentProfileId);

        /// <summary>
        /// Lấy đường dẫn thư mục chứa script Python: Storage/Workspaces/{AgentId}/scripts/.
        /// </summary>
        string GetScriptsPath(Guid agentProfileId);

        /// <summary>
        /// Lấy đường dẫn thư mục chứa artifacts kết quả: Storage/Workspaces/{AgentId}/artifacts/.
        /// </summary>
        string GetArtifactsPath(Guid agentProfileId);

        /// <summary>
        /// Lấy đường dẫn thư mục chứa nhật ký thực thi: Storage/Workspaces/{AgentId}/logs/.
        /// </summary>
        string GetLogsPath(Guid agentProfileId);

        /// <summary>
        /// Khởi tạo đầy đủ các thư mục chuẩn mực cho Agent nếu chưa tồn tại.
        /// </summary>
        Task<Result> EnsureWorkspaceDirectoriesAsync(Guid agentProfileId, CancellationToken ct = default);

        /// <summary>
        /// Thống kê dung lượng ổ đĩa và số lượng file trong workspace của Agent.
        /// </summary>
        Task<Result<WorkspaceStatsDto>> GetWorkspaceStatsAsync(Guid agentProfileId, CancellationToken ct = default);

        /// <summary>
        /// Duyệt danh sách file trong workspace theo phân mục.
        /// </summary>
        Task<Result<IReadOnlyList<WorkspaceFileDto>>> ListFilesAsync(Guid agentProfileId, string? subFolder = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy stream đọc file trong workspace có cơ chế bảo vệ Path Traversal Guard.
        /// </summary>
        Task<Result<Stream>> GetFileStreamAsync(Guid agentProfileId, string relativeFilePath, CancellationToken ct = default);

        /// <summary>
        /// Xóa một file trong workspace có cơ chế bảo vệ Path Traversal Guard.
        /// </summary>
        Task<Result> DeleteFileAsync(Guid agentProfileId, string relativeFilePath, CancellationToken ct = default);

        /// <summary>
        /// Ghi nội dung script Python vào thư mục scripts của Agent.
        /// </summary>
        Task<Result<string>> WriteScriptFileAsync(Guid agentProfileId, string scriptName, string scriptContent, CancellationToken ct = default);
    }
}
