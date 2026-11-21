// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Truy vấn danh sách các tác vụ trong kênh để render bảng Kanban.
    /// </summary>
    public record GetChannelTasksQuery(Guid ChannelId) : IQuery<List<TaskDto>>;

    /// <summary>
    /// Handler xử lý lấy danh sách tác vụ của kênh.
    /// </summary>
    public class GetChannelTasksQueryHandler(ITasksDbContext dbContext)
        : IQueryHandler<GetChannelTasksQuery, List<TaskDto>>
    {
        /// <inheritdoc />
        public async Task<Result<List<TaskDto>>> Handle(
            GetChannelTasksQuery request,
            CancellationToken cancellationToken)
        {
            var tasks = await dbContext.Tasks
                .AsNoTracking()
                .Include(t => t.Assignments)
                .Where(t => t.ChannelId == request.ChannelId)
                .OrderBy(t => t.CreatedDate)
                .ToListAsync(cancellationToken);

            var dtos = tasks.Select(TaskDto.FromEntity).ToList();
            return Result.Success(dtos);
        }
    }
}
