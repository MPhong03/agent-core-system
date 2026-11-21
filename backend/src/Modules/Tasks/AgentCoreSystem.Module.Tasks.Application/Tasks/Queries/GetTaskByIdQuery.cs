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

namespace AgentCoreSystem.Module.Tasks.Application.Tasks.Queries
{
    /// <summary>
    /// Truy vấn chi tiết đầy đủ của một tác vụ bao gồm artifacts và lịch sử hoạt động.
    /// </summary>
    public record GetTaskByIdQuery(Guid TaskId) : IQuery<TaskDetailDto>;

    /// <summary>
    /// Handler xử lý lấy chi tiết tác vụ theo mã Id.
    /// </summary>
    public class GetTaskByIdQueryHandler(ITasksDbContext dbContext)
        : IQueryHandler<GetTaskByIdQuery, TaskDetailDto>
    {
        /// <inheritdoc />
        public async Task<Result<TaskDetailDto>> Handle(
            GetTaskByIdQuery request,
            CancellationToken cancellationToken)
        {
            var task = await dbContext.Tasks
                .AsNoTracking()
                .Include(t => t.Assignments)
                .Include(t => t.Artifacts)
                .Include(t => t.ActivityLogs)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

            if (task == null)
            {
                return Result.Failure<TaskDetailDto>(Error.NotFound("Task.NotFound", "Không tìm thấy tác vụ được chỉ định."));
            }

            return Result.Success(TaskDetailDto.FromEntity(task));
        }
    }
}
