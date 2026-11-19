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
        /// <summary>
        /// Tạo một thành viên thuộc loại Người dùng (Human).
        /// </summary>
        /// <param name="channelId">Mã kênh tham gia.</param>
        /// <param name="userId">Mã người dùng từ Module System (liên kết logic).</param>
        /// <param name="displayName">Tên hiển thị trong kênh.</param>
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

        /// <summary>
        /// Constructor phục vụ Entity Framework Core deserialization.
        /// </summary>
        public Member() : base()
        {
            DisplayName = string.Empty;
        }

        /// <summary>
        /// Mã định danh kênh nhóm trực thuộc.
        /// </summary>
        public Guid ChannelId { get; set; }

        /// <summary>
        /// Loại thành viên (Human hoặc Agent).
        /// </summary>
        public MemberType Type { get; set; }

        /// <summary>
        /// Mã định danh người dùng từ Module System (chỉ có giá trị nếu Type = Human).
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Mã hồ sơ nhân sự AI (chỉ có giá trị nếu Type = Agent).
        /// </summary>
        public Guid? AgentProfileId { get; set; }

        /// <summary>
        /// Tên hiển thị của thành viên trong kênh.
        /// </summary>
        public string DisplayName { get; set; }

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
