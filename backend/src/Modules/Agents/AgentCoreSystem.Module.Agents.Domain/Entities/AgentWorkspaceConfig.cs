// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Agents.Domain.Entities
{
    /// <summary>
    /// Thực thể quản lý cấu hình và trạng thái không gian làm việc (Workspace & .venv Sandbox) của Agent.
    /// </summary>
    public class AgentWorkspaceConfig : BaseEntity
    {
        // ── Constructors ──

        /// <summary>
        /// Khởi tạo phục vụ Entity Framework Core deserialization.
        /// </summary>
        protected AgentWorkspaceConfig() : base()
        {
        }

        private AgentWorkspaceConfig(
            Guid agentProfileId,
            int maxTimeoutSeconds,
            string? pythonVersion) : base()
        {
            AgentProfileId = agentProfileId;
            MaxTimeoutSeconds = maxTimeoutSeconds;
            PythonVersion = pythonVersion;
            IsVenvReady = false;
            IsActive = true;
            StorageUsedBytes = 0;
            LastActiveAt = null;
        }

        // ── Properties ──

        /// <summary>
        /// Định danh hồ sơ Agent sở hữu không gian làm việc này.
        /// </summary>
        public Guid AgentProfileId { get; private set; }

        /// <summary>
        /// Phiên bản Python được phát hiện hoặc chỉ định (ví dụ: '3.12.0').
        /// </summary>
        public string? PythonVersion { get; private set; }

        /// <summary>
        /// Thời gian thực thi tối đa cho phép (tính theo giây).
        /// </summary>
        public int MaxTimeoutSeconds { get; private set; }

        /// <summary>
        /// Trạng thái sẵn sàng của môi trường ảo Python (.venv).
        /// </summary>
        public bool IsVenvReady { get; private set; }

        /// <summary>
        /// Trạng thái hoạt động của sandbox.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Dung lượng lưu trữ đã sử dụng trên đĩa (tính theo bytes).
        /// </summary>
        public long StorageUsedBytes { get; private set; }

        /// <summary>
        /// Thời điểm kích hoạt hoặc thực thi script gần nhất.
        /// </summary>
        public DateTime? LastActiveAt { get; private set; }

        // ── Factory Methods & Domain Actions ──

        /// <summary>
        /// Tạo mới bản ghi cấu hình workspace cho Agent.
        /// </summary>
        public static Result<AgentWorkspaceConfig> Create(
            Guid agentProfileId,
            int maxTimeoutSeconds = 60,
            string? pythonVersion = null)
        {
            if (agentProfileId == Guid.Empty)
            {
                return Result.Failure<AgentWorkspaceConfig>(Error.Validation("AgentWorkspaceConfig.InvalidAgentId", "AgentProfileId không được để trống."));
            }

            var timeout = Math.Clamp(maxTimeoutSeconds, 5, 300);
            var config = new AgentWorkspaceConfig(agentProfileId, timeout, pythonVersion);
            return Result.Success(config);
        }

        /// <summary>
        /// Đánh dấu môi trường ảo .venv đã được khởi tạo sẵn sàng.
        /// </summary>
        public void MarkVenvReady(string? pythonVersion = null)
        {
            IsVenvReady = true;
            if (!string.IsNullOrWhiteSpace(pythonVersion))
            {
                PythonVersion = pythonVersion;
            }
            LastActiveAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Cập nhật dung lượng ổ đĩa đã sử dụng.
        /// </summary>
        public void UpdateStorageUsed(long bytes)
        {
            StorageUsedBytes = Math.Max(0, bytes);
        }

        /// <summary>
        /// Ghi nhận thời điểm hoạt động mới nhất.
        /// </summary>
        public void RecordActivity()
        {
            LastActiveAt = DateTime.UtcNow;
        }
    }
}
