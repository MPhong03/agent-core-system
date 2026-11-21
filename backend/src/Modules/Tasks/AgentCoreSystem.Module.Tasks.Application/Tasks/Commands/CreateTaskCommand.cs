// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Tasks.Application.Contracts;
using AgentCoreSystem.Module.Tasks.Application.Tasks.DTOs;
using AgentCoreSystem.Module.Tasks.Domain.Entities;
using AgentCoreSystem.Module.Tasks.Domain.Enums;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Tasks.Application.Tasks.Commands
{
    /// <summary>
    /// Lệnh khởi tạo một tác vụ mới trên bảng Kanban của kênh.
    /// </summary>
    public record CreateTaskCommand(
        Guid ChannelId,
        string Title,
        string Description,
        TaskPriority Priority,
        DateTimeOffset? DueDate,
        Guid? AssigneeMemberId,
        bool? IsAgent,
        Guid CreatorMemberId) : ICommand<TaskDto>;

    /// <summary>
    /// Handler xử lý tạo mới tác vụ.
    /// </summary>
    public class CreateTaskCommandHandler(ITasksDbContext dbContext)
        : ICommandHandler<CreateTaskCommand, TaskDto>
    {
        /// <inheritdoc />
        public async Task<Result<TaskDto>> Handle(
            CreateTaskCommand request,
            CancellationToken cancellationToken)
        {
            var taskCount = await dbContext.Tasks
                .CountAsync(t => t.ChannelId == request.ChannelId, cancellationToken);

            var taskCode = $"TSK-{(taskCount + 1):D3}";

            var createResult = TaskItem.Create(
                request.ChannelId,
                taskCode,
                request.Title,
                request.Description,
                request.Priority,
                request.DueDate,
                request.CreatorMemberId);

            if (createResult.IsFailure)
            {
                return Result.Failure<TaskDto>(createResult.Error);
            }

            var task = createResult.Value;

            if (request.AssigneeMemberId.HasValue && request.AssigneeMemberId.Value != Guid.Empty)
            {
                var assignResult = task.AssignTo(
                    request.AssigneeMemberId.Value,
                    request.IsAgent ?? false,
                    "Assignee",
                    request.CreatorMemberId);

                if (assignResult.IsFailure)
                {
                    return Result.Failure<TaskDto>(assignResult.Error);
                }
            }

            dbContext.Tasks.Add(task);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(TaskDto.FromEntity(task));
        }
    }
}
