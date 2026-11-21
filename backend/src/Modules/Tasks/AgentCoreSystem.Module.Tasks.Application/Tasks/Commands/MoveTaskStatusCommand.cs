// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Tasks.Application.Contracts;
using AgentCoreSystem.Module.Tasks.Application.Tasks.DTOs;
using AgentCoreSystem.Module.Tasks.Domain.Enums;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Tasks.Application.Tasks.Commands
{
    /// <summary>
    /// Lệnh điều chỉnh trạng thái tác vụ trên bảng Kanban (kéo thả giữa các cột).
    /// </summary>
    public record MoveTaskStatusCommand(
        Guid TaskId,
        TaskItemStatus NewStatus,
        Guid ActorMemberId,
        bool IsActorAgent,
        string? Comment = null) : ICommand<TaskDto>;

    /// <summary>
    /// Handler xử lý việc di chuyển trạng thái tác vụ.
    /// </summary>
    public class MoveTaskStatusCommandHandler(ITasksDbContext dbContext)
        : ICommandHandler<MoveTaskStatusCommand, TaskDto>
    {
        /// <inheritdoc />
        public async Task<Result<TaskDto>> Handle(
            MoveTaskStatusCommand request,
            CancellationToken cancellationToken)
        {
            var task = await dbContext.Tasks
                .Include(t => t.Assignments)
                .Include(t => t.Artifacts)
                .Include(t => t.ActivityLogs)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

            if (task == null)
            {
                return Result.Failure<TaskDto>(Error.NotFound("Task.NotFound", "Không tìm thấy tác vụ được chỉ định."));
            }

            var moveResult = task.MoveTo(
                request.NewStatus,
                request.ActorMemberId,
                request.IsActorAgent,
                request.Comment);

            if (moveResult.IsFailure)
            {
                return Result.Failure<TaskDto>(moveResult.Error);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(TaskDto.FromEntity(task));
        }
    }
}
