// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Application.Workspace.DTOs;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Workspace.Queries
{
    /// <summary>
    /// Truy vấn danh sách lịch sử các lần chạy script của Agent.
    /// </summary>
    public record GetScriptHistoryQuery(
        Guid AgentProfileId,
        int Limit = 50) : IQuery<IReadOnlyList<ScriptExecutionHistoryDto>>;

    /// <summary>
    /// Handler xử lý tra cứu lịch sử chạy script.
    /// </summary>
    public class GetScriptHistoryQueryHandler(
        IAgentsDbContext dbContext) : IQueryHandler<GetScriptHistoryQuery, IReadOnlyList<ScriptExecutionHistoryDto>>
    {
        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<ScriptExecutionHistoryDto>>> Handle(
            GetScriptHistoryQuery request,
            CancellationToken cancellationToken)
        {
            var limit = Math.Clamp(request.Limit, 1, 100);

            var histories = await dbContext.ScriptExecutionHistories
                .AsNoTracking()
                .Where(h => h.AgentProfileId == request.AgentProfileId)
                .OrderByDescending(h => h.CreatedDate)
                .Take(limit)
                .ToListAsync(cancellationToken);

            var dtos = histories.Select(ScriptExecutionHistoryDto.FromEntity).ToList();
            return Result.Success<IReadOnlyList<ScriptExecutionHistoryDto>>(dtos);
        }
    }
}
