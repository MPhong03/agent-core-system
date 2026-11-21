// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Agents.Domain.Entities
{
    /// <summary>
    /// Thực thể phân quyền công cụ cho Agent (Agent Tool Access).
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Quản lý danh sách các công cụ mà một Agent Profile cụ thể được phép sử dụng.</para>
    /// <para><strong>Why:</strong> Đảm bảo nguyên tắc đặc quyền tối thiểu (Least Privilege), mỗi Agent chỉ có quyền gọi các công cụ liên quan đến vai trò của nó.</para>
    /// </remarks>
    public class AgentToolAccess : BaseEntity
    {
        // ── Constructors ──

        /// <summary>
        /// Khởi tạo phục vụ Entity Framework Core deserialization.
        /// </summary>
        protected AgentToolAccess() : base()
        {
        }

        /// <summary>
        /// Khởi tạo quyền truy cập công cụ cho Agent với khóa chính UUIDv7.
        /// </summary>
        /// <param name="agentProfileId">Mã định danh hồ sơ Agent (tham chiếu logic sang org.AgentProfiles).</param>
        /// <param name="toolDefinitionId">Mã định danh công cụ.</param>
        /// <param name="isActive">Trạng thái kích hoạt quyền (mặc định true).</param>
        /// <param name="id">Mã định danh (UUIDv7, tùy chọn).</param>
        public AgentToolAccess(
            Guid agentProfileId,
            Guid toolDefinitionId,
            bool isActive = true,
            Guid? id = null) : base(id ?? Guid.Empty)
        {
            AgentProfileId = agentProfileId;
            ToolDefinitionId = toolDefinitionId;
            IsActive = isActive;
        }

        // ── Properties ──

        /// <summary>
        /// Mã định danh hồ sơ Agent được cấp quyền (tham chiếu logic sang org.AgentProfiles).
        /// </summary>
        /// <remarks>⛔ <strong>Cấm:</strong> Không tạo Foreign Key vật lý sang bảng org.AgentProfiles.</remarks>
        public Guid AgentProfileId { get; private set; }

        /// <summary>
        /// Mã định danh công cụ được cấp quyền.
        /// </summary>
        public Guid ToolDefinitionId { get; private set; }

        /// <summary>
        /// Trạng thái hiệu lực của quyền truy cập.
        /// </summary>
        public bool IsActive { get; private set; }

        // ── Navigation ──

        /// <summary>
        /// Định nghĩa công cụ tương ứng.
        /// </summary>
        public virtual ToolDefinition? ToolDefinition { get; private set; }

        // ── Domain Methods ──

        /// <summary>
        /// Kích hoạt quyền truy cập công cụ.
        /// </summary>
        public void Activate()
        {
            IsActive = true;
        }

        /// <summary>
        /// Vô hiệu hóa quyền truy cập công cụ.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
