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
using AgentCoreSystem.Module.Agents.Domain.Entities;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Tools.Commands
{
    /// <summary>
    /// Lệnh phân quyền danh sách công cụ được phép sử dụng cho một Agent Profile.
    /// </summary>
    /// <param name="AgentProfileId">Mã định danh hồ sơ Agent.</param>
    /// <param name="ToolDefinitionIds">Danh sách mã định danh công cụ được cấp quyền.</param>
    public record AssignAgentToolsCommand(
        Guid AgentProfileId,
        List<Guid> ToolDefinitionIds) : ICommand;

    /// <summary>
    /// Bộ xác thực dữ liệu đầu vào cho lệnh <see cref="AssignAgentToolsCommand"/>.
    /// </summary>
    public class AssignAgentToolsCommandValidator : AbstractValidator<AssignAgentToolsCommand>
    {
        public AssignAgentToolsCommandValidator()
        {
            RuleFor(x => x.AgentProfileId)
                .NotEmpty().WithMessage("Mã hồ sơ Agent (AgentProfileId) không được để trống.");

            RuleFor(x => x.ToolDefinitionIds)
                .NotNull().WithMessage("Danh sách mã công cụ không được là null.");
        }
    }

    /// <summary>
    /// Handler xử lý đồng bộ phân quyền công cụ cho Agent Profile.
    /// </summary>
    public class AssignAgentToolsCommandHandler(IAgentsDbContext dbContext)
        : ICommandHandler<AssignAgentToolsCommand>
    {
        /// <inheritdoc />
        public async Task<Result> Handle(
            AssignAgentToolsCommand request,
            CancellationToken cancellationToken)
        {
            var requestedToolIds = new HashSet<Guid>(request.ToolDefinitionIds);

            var existingAccesses = await dbContext.AgentToolAccesses
                .Where(a => a.AgentProfileId == request.AgentProfileId)
                .ToListAsync(cancellationToken);

            var existingToolIdMap = existingAccesses.ToDictionary(a => a.ToolDefinitionId);

            // Cập nhật trạng thái các quyền hiện có
            foreach (var access in existingAccesses)
            {
                if (requestedToolIds.Contains(access.ToolDefinitionId))
                {
                    access.Activate();
                }
                else
                {
                    access.Deactivate();
                }
            }

            // Bổ sung các quyền mới chưa từng có
            foreach (var toolId in requestedToolIds)
            {
                if (!existingToolIdMap.ContainsKey(toolId))
                {
                    var newAccess = new AgentToolAccess(request.AgentProfileId, toolId, isActive: true);
                    dbContext.AgentToolAccesses.Add(newAccess);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
