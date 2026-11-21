// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Tools.Commands
{
    /// <summary>
    /// Lệnh xóa mềm (Soft Delete) một định nghĩa công cụ khỏi hệ thống.
    /// </summary>
    /// <param name="Id">Mã định danh công cụ cần xóa.</param>
    public record DeleteToolDefinitionCommand(Guid Id) : ICommand;

    /// <summary>
    /// Handler xử lý xóa mềm định nghĩa công cụ (vô hiệu hóa và đánh dấu IsDeleted).
    /// </summary>
    public class DeleteToolDefinitionCommandHandler(IAgentsDbContext dbContext)
        : ICommandHandler<DeleteToolDefinitionCommand>
    {
        /// <inheritdoc />
        public async Task<Result> Handle(
            DeleteToolDefinitionCommand request,
            CancellationToken cancellationToken)
        {
            var tool = await dbContext.Tools
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (tool == null)
            {
                return Result.Failure(Error.NotFound(
                    "ToolDefinition.NotFound",
                    $"Không tìm thấy định nghĩa công cụ với mã '{request.Id}'."));
            }

            tool.Deactivate();
            tool.IsDeleted = true;
            tool.DeletedDate = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
