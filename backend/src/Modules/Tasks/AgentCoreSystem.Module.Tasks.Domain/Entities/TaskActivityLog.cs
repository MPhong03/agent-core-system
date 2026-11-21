// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Tasks.Domain.Enums;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Tasks.Domain.Entities
{
    /// <summary>
    /// Bản ghi nhật ký hoạt động và biến động trạng thái của tác vụ.
    /// </summary>
    public class TaskActivityLog : BaseEntity
    {
        private TaskActivityLog()
        {
        }

        public TaskActivityLog(
            Guid taskId,
            Guid actorMemberId,
            string actionType,
            TaskItemStatus? fromStatus,
            TaskItemStatus? toStatus,
            string? comment)
        {
            TaskId = taskId;
            ActorMemberId = actorMemberId;
            ActionType = actionType;
            FromStatus = fromStatus;
            ToStatus = toStatus;
            Comment = comment;
        }

        /// <summary>
        /// Mã tác vụ liên kết.
        /// </summary>
        public Guid TaskId { get; private set; }

        /// <summary>
        /// Mã thành viên (Human hoặc Agent) thực hiện thao tác.
        /// </summary>
        public Guid ActorMemberId { get; private set; }

        /// <summary>
        /// Loại hành động (Created, StatusChanged, Assigned, ArtifactAdded, ReviewApproved, RevisionRequested).
        /// </summary>
        public string ActionType { get; private set; } = string.Empty;

        /// <summary>
        /// Trạng thái trước khi thay đổi (nếu có).
        /// </summary>
        public TaskItemStatus? FromStatus { get; private set; }

        /// <summary>
        /// Trạng thái sau khi thay đổi (nếu có).
        /// </summary>
        public TaskItemStatus? ToStatus { get; private set; }

        /// <summary>
        /// Bình luận hoặc lý do điều chỉnh.
        /// </summary>
        public string? Comment { get; private set; }
    }
}
