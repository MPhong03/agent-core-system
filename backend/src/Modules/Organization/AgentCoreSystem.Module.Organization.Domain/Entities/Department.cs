// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Organization.Domain.Entities
{
    /// <summary>
    /// Thực thể đại diện cho Phòng ban / Khối chức năng trong doanh nghiệp.
    /// </summary>
    /// <remarks>
    /// <strong>What:</strong> Quản lý danh mục phòng ban (Kỹ thuật, Vận hành, Kế toán...) và phân bổ các kênh làm việc trực thuộc.
    /// <strong>Why:</strong> Cung cấp ranh giới tổ chức cấp cao nhất để gom nhóm các luồng nghiệp vụ và biệt đội AI.
    /// </remarks>
    public class Department : BaseEntity
    {
        /// <summary>
        /// Khởi tạo một phòng ban mới.
        /// </summary>
        /// <param name="name">Tên phòng ban duy nhất.</param>
        /// <param name="description">Mô tả chức năng nhiệm vụ của phòng ban.</param>
        public Department(string name, string? description = null) : base()
        {
            Name = name;
            Description = description;
            Channels = new List<TeamChannel>();
        }

        /// <summary>
        /// Constructor phục vụ Entity Framework Core deserialization.
        /// </summary>
        protected Department() : base()
        {
            Name = string.Empty;
            Channels = new List<TeamChannel>();
        }

        /// <summary>
        /// Tên phòng ban.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Mô tả chi tiết chức năng nhiệm vụ của phòng ban.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Danh sách các kênh làm việc trực thuộc phòng ban.
        /// </summary>
        public virtual ICollection<TeamChannel> Channels { get; set; }
    }
}
