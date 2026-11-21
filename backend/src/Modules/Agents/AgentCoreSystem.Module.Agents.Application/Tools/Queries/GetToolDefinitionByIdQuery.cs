// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
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
    /// Truy vấn thông tin chi tiết một định nghĩa công cụ theo mã định danh.
    /// </summary>
    /// <param name="Id">Mã định danh công cụ.</param>
    /// <param name="MaskSecrets">Cờ yêu cầu che giấu trường Secret trong Headers (mặc định True).</param>
    public record GetToolDefinitionByIdQuery(Guid Id, bool MaskSecrets = true) : IQuery<ToolDefinitionDto>;

    /// <summary>
    /// Handler xử lý truy vấn chi tiết một định nghĩa công cụ.
    /// </summary>
    public class GetToolDefinitionByIdQueryHandler(IAgentsDbContext dbContext)
        : IQueryHandler<GetToolDefinitionByIdQuery, ToolDefinitionDto>
    {
        /// <inheritdoc />
        public async Task<Result<ToolDefinitionDto>> Handle(
            GetToolDefinitionByIdQuery request,
            CancellationToken cancellationToken)
        {
            var tool = await dbContext.Tools
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (tool == null)
            {
                return Result.Failure<ToolDefinitionDto>(Error.NotFound(
                    "ToolDefinition.NotFound",
                    $"Không tìm thấy định nghĩa công cụ với mã '{request.Id}'."));
            }

            return Result.Success(ToolDefinitionDto.FromEntity(tool, maskSecrets: request.MaskSecrets));
        }
    }
}
