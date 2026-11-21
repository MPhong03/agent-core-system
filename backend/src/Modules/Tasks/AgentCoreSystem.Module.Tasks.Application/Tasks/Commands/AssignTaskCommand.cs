// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Tasks.Application.Contracts;
using AgentCoreSystem.Module.Tasks.Application.Tasks.DTOs;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Tasks.Application.Tasks.Commands
{
    /// <summary>
    /// Lệnh phân công trách nhiệm thực thi tác vụ cho thành viên (Human hoặc Agent).
    /// </summary>
    public record AssignTaskCommand(
        Guid TaskId,
        Guid AssigneeMemberId,
        bool IsAgent,
        string RoleInTask,
        Guid ActorMemberId) : ICommand<TaskDto>;

    /// <summary>
    /// Handler xử lý việc phân công tác vụ.
    /// </summary>
    public class AssignTaskCommandHandler(ITasksDbContext dbContext)
        : ICommandHandler<AssignTaskCommand, TaskDto>
    {
        /// <inheritdoc />
        public async Task<Result<TaskDto>> Handle(
            AssignTaskCommand request,
            CancellationToken cancellationToken)
        {
            var task = await dbContext.Tasks
                .Include(t => t.Assignments)
                .Include(t => t.ActivityLogs)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

            if (task == null)
            {
                return Result.Failure<TaskDto>(Error.NotFound("Task.NotFound", "Không tìm thấy tác vụ được chỉ định."));
            }

            var assignResult = task.AssignTo(
                request.AssigneeMemberId,
                request.IsAgent,
                request.RoleInTask,
                request.ActorMemberId);

            if (assignResult.IsFailure)
            {
                return Result.Failure<TaskDto>(assignResult.Error);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(TaskDto.FromEntity(task));
        }
    }
}
