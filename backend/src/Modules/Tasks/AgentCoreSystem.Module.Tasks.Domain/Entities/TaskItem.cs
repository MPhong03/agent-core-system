// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AgentCoreSystem.Module.Tasks.Domain.Enums;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Tasks.Domain.Entities
{
    /// <summary>
    /// Thực thể quản lý Tác vụ cốt lõi trên bảng Kanban thuộc phân hệ Tasks.
    /// </summary>
    public class TaskItem : BaseEntity
    {
        private readonly List<TaskAssignment> _assignments = [];
        private readonly List<TaskArtifact> _artifacts = [];
        private readonly List<TaskActivityLog> _activityLogs = [];

        private TaskItem()
        {
        }

        private TaskItem(
            Guid channelId,
            string taskCode,
            string title,
            string description,
            TaskPriority priority,
            DateTimeOffset? dueDate,
            Guid creatorMemberId)
        {
            ChannelId = channelId;
            TaskCode = taskCode;
            Title = title;
            Description = description;
            Priority = priority;
            DueDate = dueDate;
            Status = TaskItemStatus.ToDo;

            _activityLogs.Add(new TaskActivityLog(
                Id,
                creatorMemberId,
                "Created",
                null,
                TaskItemStatus.ToDo,
                "Tác vụ được khởi tạo."));
        }

        /// <summary>
        /// Mã kênh làm việc (TeamChannel) sở hữu tác vụ này.
        /// </summary>
        public Guid ChannelId { get; private set; }

        /// <summary>
        /// Mã định danh hiển thị của tác vụ (ví dụ: TSK-001).
        /// </summary>
        public string TaskCode { get; private set; } = string.Empty;

        /// <summary>
        /// Tiêu đề tác vụ.
        /// </summary>
        public string Title { get; private set; } = string.Empty;

        /// <summary>
        /// Mô tả chi tiết yêu cầu công việc.
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// Trạng thái hiện tại trên bảng Kanban.
        /// </summary>
        public TaskItemStatus Status { get; private set; } = TaskItemStatus.ToDo;

        /// <summary>
        /// Mức độ ưu tiên của tác vụ.
        /// </summary>
        public TaskPriority Priority { get; private set; } = TaskPriority.Medium;

        /// <summary>
        /// Thời hạn hoàn thành tác vụ (nếu có).
        /// </summary>
        public DateTimeOffset? DueDate { get; private set; }

        /// <summary>
        /// Thời điểm bắt đầu thực thi (chuyển sang InProgress).
        /// </summary>
        public DateTimeOffset? StartedDate { get; private set; }

        /// <summary>
        /// Thời điểm hoàn thành và được nghiệm thu (chuyển sang Done).
        /// </summary>
        public DateTimeOffset? CompletedDate { get; private set; }

        /// <summary>
        /// Danh sách phân công chịu trách nhiệm.
        /// </summary>
        public IReadOnlyCollection<TaskAssignment> Assignments => _assignments.AsReadOnly();

        /// <summary>
        /// Danh sách sản phẩm đầu ra (Artifacts) bàn giao.
        /// </summary>
        public IReadOnlyCollection<TaskArtifact> Artifacts => _artifacts.AsReadOnly();

        /// <summary>
        /// Lịch sử biến động trạng thái và nhật ký hoạt động.
        /// </summary>
        public IReadOnlyCollection<TaskActivityLog> ActivityLogs => _activityLogs.AsReadOnly();

        /// <summary>
        /// Phương thức tạo mới một tác vụ với các thuộc tính hợp lệ.
        /// </summary>
        public static Result<TaskItem> Create(
            Guid channelId,
            string taskCode,
            string title,
            string description,
            TaskPriority priority,
            DateTimeOffset? dueDate,
            Guid creatorMemberId)
        {
            if (channelId == Guid.Empty)
            {
                return Result.Failure<TaskItem>(Error.Validation("TaskItem.ChannelRequired", "Mã kênh làm việc không được để trống."));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                return Result.Failure<TaskItem>(Error.Validation("TaskItem.TitleRequired", "Tiêu đề tác vụ không được để trống."));
            }

            if (string.IsNullOrWhiteSpace(taskCode))
            {
                return Result.Failure<TaskItem>(Error.Validation("TaskItem.CodeRequired", "Mã hiển thị tác vụ (TaskCode) không được để trống."));
            }

            var task = new TaskItem(
                channelId,
                taskCode.Trim(),
                title.Trim(),
                description?.Trim() ?? string.Empty,
                priority,
                dueDate,
                creatorMemberId);

            return Result.Success(task);
        }

        /// <summary>
        /// Chuyển đổi trạng thái tác vụ tuân thủ State Machine và Cổng Nghiệm Thu (Review Gate).
        /// </summary>
        public Result MoveTo(
            TaskItemStatus newStatus,
            Guid actorMemberId,
            bool isActorAgent,
            string? comment = null)
        {
            if (Status == newStatus)
            {
                return Result.Success();
            }

            // Luôn cho phép chuyển sang Failed khi gặp sự cố
            if (newStatus == TaskItemStatus.Failed)
            {
                RecordStatusChange(newStatus, actorMemberId, comment ?? "Tác vụ thất bại.");
                return Result.Success();
            }

            switch (Status)
            {
                case TaskItemStatus.ToDo:
                    if (newStatus != TaskItemStatus.InProgress)
                    {
                        return Result.Failure(Error.Validation(
                            "TaskItem.InvalidTransition",
                            $"Không thể chuyển trạng thái từ ToDo sang {newStatus}. Tác vụ phải bắt đầu ở InProgress trước."));
                    }
                    StartedDate ??= DateTimeOffset.UtcNow;
                    break;

                case TaskItemStatus.InProgress:
                    if (newStatus != TaskItemStatus.InReview && newStatus != TaskItemStatus.ToDo)
                    {
                        return Result.Failure(Error.Validation(
                            "TaskItem.InvalidTransition",
                            $"Không thể chuyển trực tiếp từ InProgress sang {newStatus}. Phải qua Cổng Nghiệm Thu (InReview)."));
                    }
                    break;

                case TaskItemStatus.InReview:
                    if (newStatus == TaskItemStatus.Done)
                    {
                        // ⛔ CẤM AGENT TỰ DUYỆT (Review Gate)
                        if (isActorAgent)
                        {
                            return Result.Failure(Error.Forbidden(
                                "TaskItem.AgentCannotApprove",
                                "Nhân sự AI không được phép tự nghiệm thu tác vụ. Thao tác duyệt chuyển sang Done chỉ dành cho Con người."));
                        }
                        CompletedDate = DateTimeOffset.UtcNow;
                    }
                    else if (newStatus != TaskItemStatus.InProgress && newStatus != TaskItemStatus.ToDo)
                    {
                        return Result.Failure(Error.Validation(
                            "TaskItem.InvalidTransition",
                            $"Không thể chuyển từ InReview sang {newStatus}."));
                    }
                    break;

                case TaskItemStatus.Done:
                    // Mở lại task đã xong
                    CompletedDate = null;
                    break;
            }

            RecordStatusChange(newStatus, actorMemberId, comment);
            return Result.Success();
        }

        /// <summary>
        /// Phân công người chịu trách nhiệm cho tác vụ.
        /// </summary>
        public Result AssignTo(
            Guid memberId,
            bool isAgent,
            string roleInTask,
            Guid actorMemberId)
        {
            if (memberId == Guid.Empty)
            {
                return Result.Failure(Error.Validation("TaskItem.MemberRequired", "Mã thành viên phân công không được để trống."));
            }

            _assignments.RemoveAll(a => a.AssigneeMemberId == memberId);
            var assignment = new TaskAssignment(Id, memberId, isAgent, roleInTask);
            _assignments.Add(assignment);

            _activityLogs.Add(new TaskActivityLog(
                Id,
                actorMemberId,
                "Assigned",
                Status,
                Status,
                $"Phân công cho {(isAgent ? "Agent" : "Human")} (MemberId: {memberId}) với vai trò '{roleInTask}'."));

            return Result.Success();
        }

        /// <summary>
        /// Đính kèm sản phẩm đầu ra (Artifact) vào tác vụ.
        /// </summary>
        public Result AddArtifact(
            string fileName,
            string filePath,
            string fileType,
            long fileSize,
            string summary,
            Guid creatorMemberId)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Result.Failure(Error.Validation("TaskItem.ArtifactNameRequired", "Tên file artifact không được để trống."));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Result.Failure(Error.Validation("TaskItem.ArtifactPathRequired", "Đường dẫn file artifact không được để trống."));
            }

            var artifact = new TaskArtifact(
                Id,
                fileName.Trim(),
                filePath.Trim(),
                fileType?.Trim() ?? string.Empty,
                fileSize,
                summary?.Trim() ?? string.Empty,
                creatorMemberId);

            _artifacts.Add(artifact);

            _activityLogs.Add(new TaskActivityLog(
                Id,
                creatorMemberId,
                "ArtifactAdded",
                Status,
                Status,
                $"Đã nộp sản phẩm đầu ra: {fileName} ({fileSize} bytes)."));

            return Result.Success();
        }

        /// <summary>
        /// Ghi nhận thêm một bản ghi nhật ký hoạt động tùy chỉnh cho tác vụ (ví dụ cảnh báo yêu cầu phê duyệt công cụ).
        /// </summary>
        /// <param name="actorMemberId">Mã định danh thành viên hoặc Agent thực hiện thao tác.</param>
        /// <param name="actionType">Loại hành động hoặc phân loại cảnh báo.</param>
        /// <param name="comment">Nội dung ghi chú chi tiết về sự kiện.</param>
        /// <returns>Kết quả thực hiện <see cref="Result.Success"/>.</returns>
        public Result AddActivityLog(
            Guid actorMemberId,
            string actionType,
            string? comment)
        {
            _activityLogs.Add(new TaskActivityLog(
                Id,
                actorMemberId,
                actionType,
                Status,
                Status,
                comment));

            return Result.Success();
        }

        private void RecordStatusChange(TaskItemStatus newStatus, Guid actorMemberId, string? comment)
        {
            var oldStatus = Status;
            Status = newStatus;

            _activityLogs.Add(new TaskActivityLog(
                Id,
                actorMemberId,
                "StatusChanged",
                oldStatus,
                newStatus,
                comment ?? $"Chuyển trạng thái từ {oldStatus} sang {newStatus}."));
        }
    }
}
