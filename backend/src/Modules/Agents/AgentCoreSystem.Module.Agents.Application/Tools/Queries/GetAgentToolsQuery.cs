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
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Tools.Queries
{
    /// <summary>
    /// Truy vấn danh sách các công cụ đang hoạt động được phân quyền cho một Agent Profile cụ thể.
    /// </summary>
    /// <param name="AgentProfileId">Mã định danh hồ sơ Agent.</param>
    /// <param name="MaskSecrets">Cờ yêu cầu che giấu trường Secret trong Headers (mặc định True).</param>
    public record GetAgentToolsQuery(Guid AgentProfileId, bool MaskSecrets = true) : IQuery<List<ToolDefinitionDto>>;

    /// <summary>
    /// Handler xử lý truy vấn danh sách công cụ được phân quyền cho Agent.
    /// </summary>
    public class GetAgentToolsQueryHandler(IAgentsDbContext dbContext)
        : IQueryHandler<GetAgentToolsQuery, List<ToolDefinitionDto>>
    {
        /// <inheritdoc />
        public async Task<Result<List<ToolDefinitionDto>>> Handle(
            GetAgentToolsQuery request,
            CancellationToken cancellationToken)
        {
            var accesses = await dbContext.AgentToolAccesses
                .AsNoTracking()
                .Include(a => a.ToolDefinition)
                .Where(a => a.AgentProfileId == request.AgentProfileId && a.IsActive)
                .ToListAsync(cancellationToken);

            var activeTools = accesses
                .Where(a => a.ToolDefinition != null && a.ToolDefinition.IsActive && !a.ToolDefinition.IsDeleted)
                .Select(a => a.ToolDefinition!)
                .OrderBy(t => t.Name)
                .Select(t => ToolDefinitionDto.FromEntity(t, maskSecrets: request.MaskSecrets))
                .ToList();

            return Result.Success(activeTools);
        }
    }
}
