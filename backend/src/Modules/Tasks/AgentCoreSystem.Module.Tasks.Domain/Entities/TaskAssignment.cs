// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Tasks.Domain.Entities
{
    /// <summary>
    /// Phân công thành viên (Human hoặc Agent) chịu trách nhiệm thực thi tác vụ.
    /// </summary>
    public class TaskAssignment : BaseEntity
    {
        private TaskAssignment()
        {
        }

        public TaskAssignment(
            Guid taskId,
            Guid assigneeMemberId,
            bool isAgent,
            string roleInTask)
        {
            TaskId = taskId;
            AssigneeMemberId = assigneeMemberId;
            IsAgent = isAgent;
            RoleInTask = string.IsNullOrWhiteSpace(roleInTask) ? "Assignee" : roleInTask;
        }

        /// <summary>
        /// Mã tác vụ liên kết.
        /// </summary>
        public Guid TaskId { get; private set; }

        /// <summary>
        /// Mã thành viên (MemberId) từ phân hệ Organization.
        /// </summary>
        public Guid AssigneeMemberId { get; private set; }

        /// <summary>
        /// Cờ nhận biết người nhận là Nhân sự AI (Agent) hay Con người (Human).
        /// </summary>
        public bool IsAgent { get; private set; }

        /// <summary>
        /// Vai trò hoặc định danh trong tác vụ (ví dụ: Assignee, Reviewer, Assistant).
        /// </summary>
        public string RoleInTask { get; private set; } = "Assignee";
    }
}
