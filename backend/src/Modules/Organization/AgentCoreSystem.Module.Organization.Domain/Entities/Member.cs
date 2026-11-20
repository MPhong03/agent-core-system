// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Organization.Domain.Entities
{
    /// <summary>
    /// Thực thể đại diện cho Thành viên tham gia vào một Kênh làm việc.
    /// </summary>
    /// <remarks>
    /// <strong>What:</strong> Kết nối một Người dùng thật (qua UserId) hoặc một Hồ sơ Agent (qua AgentProfileId) vào Kênh.
    /// <strong>Why:</strong> Đảm bảo tính trừu tượng hóa cho cả Human và Agent để phục vụ tính năng @mention trong Chat và giao việc trên Kanban.
    /// </remarks>
    public class Member : BaseEntity
    {
        // ── Factory Methods ──

        /// <summary>
        /// Tạo một thành viên thuộc loại Người dùng (Human).
        /// </summary>
        /// <param name="channelId">Mã kênh tham gia.</param>
        /// <param name="userId">Mã người dùng từ Module System (liên kết logic).</param>
        /// <param name="displayName">Tên hiển thị trong kênh.</param>
        /// <returns>Thực thể <see cref="Member"/> đại diện cho người dùng thật.</returns>
        /// <remarks>
        /// <para>🔴 <strong>Ràng buộc:</strong> <paramref name="userId"/> bắt buộc có giá trị và <see cref="AgentProfileId"/> nhận giá trị null.</para>
        /// </remarks>
        public static Member CreateHuman(Guid channelId, Guid userId, string displayName)
        {
            return new Member
            {
                ChannelId = channelId,
                Type = MemberType.Human,
                UserId = userId,
                AgentProfileId = null,
                DisplayName = displayName
            };
        }

        /// <summary>
        /// Tạo một thành viên thuộc loại Nhân sự AI (Agent).
        /// </summary>
        /// <param name="channelId">Mã kênh tham gia.</param>
        /// <param name="agentProfileId">Mã hồ sơ Agent định danh.</param>
        /// <param name="displayName">Tên đại diện Agent trong kênh.</param>
        /// <returns>Thực thể <see cref="Member"/> đại diện cho nhân sự AI.</returns>
        /// <remarks>
        /// <para>🔴 <strong>Ràng buộc:</strong> <paramref name="agentProfileId"/> bắt buộc có giá trị và <see cref="UserId"/> nhận giá trị null.</para>
        /// </remarks>
        public static Member CreateAgent(Guid channelId, Guid agentProfileId, string displayName)
        {
            return new Member
            {
                ChannelId = channelId,
                Type = MemberType.Agent,
                UserId = null,
                AgentProfileId = agentProfileId,
                DisplayName = displayName
            };
        }

        // ── Constructors ──

        /// <summary>
        /// Constructor phục vụ Entity Framework Core deserialization.
        /// </summary>
        public Member() : base()
        {
            DisplayName = string.Empty;
        }

        // ── Channel & Membership ──

        /// <summary>
        /// Mã định danh kênh nhóm trực thuộc.
        /// </summary>
        /// <remarks>Khóa ngoại vật lý tham chiếu tới bảng <see cref="TeamChannel"/>.</remarks>
        public Guid ChannelId { get; set; }

        /// <summary>
        /// Loại thành viên (Human hoặc Agent).
        /// </summary>
        /// <remarks>
        /// <para>⇒ Xác định nguồn định danh tương ứng: Human ⇒ <see cref="UserId"/>; Agent ⇒ <see cref="AgentProfileId"/>.</para>
        /// </remarks>
        public MemberType Type { get; set; }

        /// <summary>
        /// Mã định danh người dùng từ Module System (chỉ có giá trị nếu Type = Human).
        /// </summary>
        /// <remarks>
        /// <para>⛔ <strong>Cấm:</strong> Không thiết lập Foreign Key vật lý sang bảng Users ở schema 'system'.</para>
        /// <para>🔴 <strong>Ràng buộc:</strong> Khóa tham chiếu logic liên module. Kiểm tra tồn tại thông qua MediatR Queries.</para>
        /// </remarks>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Mã hồ sơ nhân sự AI (chỉ có giá trị nếu Type = Agent).
        /// </summary>
        /// <remarks>
        /// <para>Khóa ngoại vật lý tham chiếu trong nội bộ module tới bảng <see cref="AgentProfile"/>.</para>
        /// </remarks>
        public Guid? AgentProfileId { get; set; }

        /// <summary>
        /// Tên hiển thị của thành viên trong kênh.
        /// </summary>
        /// <remarks>
        /// <para>🔴 <strong>Ràng buộc:</strong> Tên hiển thị là duy nhất trong phạm vi mỗi kênh (Unique Index kết hợp <see cref="ChannelId"/> và <see cref="DisplayName"/>).</para>
        /// </remarks>
        public string DisplayName { get; set; }

        // ── Navigation ──

        /// <summary>
        /// Tham chiếu điều hướng tới Kênh làm việc.
        /// </summary>
        public virtual TeamChannel? Channel { get; set; }

        /// <summary>
        /// Tham chiếu điều hướng tới Hồ sơ Agent (nếu là Agent).
        /// </summary>
        public virtual AgentProfile? AgentProfile { get; set; }
    }
}
