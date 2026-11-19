// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Organization.Domain.Entities
{
    /// <summary>
    /// Thực thể đại diện cho Kênh làm việc chuyên môn hóa theo nhóm (Team Channel).
    /// </summary>
    /// <remarks>
    /// <strong>What:</strong> Là không gian cộng tác nơi Con người và các Agent cùng trao đổi và thực thi tác vụ (vd: #tech-research, #email-processing).
    /// <strong>Why:</strong> Thiết lập phạm vi bộ nhớ chia sẻ (Shared Group Memory) và xác định các thành viên có thẩm quyền tiếp nhận chỉ đạo.
    /// </remarks>
    public class TeamChannel : BaseEntity
    {
        /// <summary>
        /// Khởi tạo một kênh làm việc mới.
        /// </summary>
        /// <param name="departmentId">Mã định danh phòng ban trực thuộc.</param>
        /// <param name="name">Tên kênh định danh (ví dụ: tech-research).</param>
        /// <param name="description">Mô tả mục tiêu của kênh.</param>
        public TeamChannel(Guid departmentId, string name, string? description = null) : base()
        {
            DepartmentId = departmentId;
            Name = name;
            Description = description;
            Members = new List<Member>();
        }

        /// <summary>
        /// Constructor phục vụ Entity Framework Core deserialization.
        /// </summary>
        protected TeamChannel() : base()
        {
            Name = string.Empty;
            Members = new List<Member>();
        }

        /// <summary>
        /// Mã định danh phòng ban trực thuộc.
        /// </summary>
        public Guid DepartmentId { get; set; }

        /// <summary>
        /// Tên kênh làm việc.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Mô tả mục tiêu của kênh.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Tham chiếu điều hướng tới phòng ban trực thuộc.
        /// </summary>
        public virtual Department? Department { get; set; }

        /// <summary>
        /// Danh sách thành viên (Human & Agent) tham gia kênh.
        /// </summary>
        public virtual ICollection<Member> Members { get; set; }
    }
}
