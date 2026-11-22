// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Application.Services.Tools;
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Tools.Queries
{
    /// <summary>
    /// Truy vấn danh sách toàn bộ các định nghĩa công cụ trong hệ thống.
    /// </summary>
    /// <param name="MaskSecrets">Cờ yêu cầu che giấu trường Secret trong Headers (mặc định True).</param>
    /// <param name="IncludeBuiltInTools">Cờ yêu cầu đính kèm các công cụ hệ thống tích hợp sẵn (mặc định False).</param>
    public record GetToolDefinitionsQuery(bool MaskSecrets = true, bool IncludeBuiltInTools = false) : IQuery<List<ToolDefinitionDto>>;

    /// <summary>
    /// Handler xử lý truy vấn danh sách định nghĩa công cụ.
    /// </summary>
    public class GetToolDefinitionsQueryHandler(IAgentsDbContext dbContext)
        : IQueryHandler<GetToolDefinitionsQuery, List<ToolDefinitionDto>>
    {
        /// <inheritdoc />
        public async Task<Result<List<ToolDefinitionDto>>> Handle(
            GetToolDefinitionsQuery request,
            CancellationToken cancellationToken)
        {
            var tools = await dbContext.Tools
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

            var dtos = tools
                .Select(t => ToolDefinitionDto.FromEntity(t, maskSecrets: request.MaskSecrets))
                .ToList();

            if (request.IncludeBuiltInTools)
            {
                foreach (var systemTool in SystemBuiltInTools.GetAll())
                {
                    if (!dtos.Any(t => t.Name.Equals(systemTool.Name, System.StringComparison.OrdinalIgnoreCase)))
                    {
                        dtos.Add(systemTool);
                    }
                }
            }

            return Result.Success(dtos);
        }
    }
}
