// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Application.Services.Python;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Workspace.Commands
{
    /// <summary>
    /// Lệnh khởi tạo hoặc sửa chữa môi trường ảo .venv cho Agent.
    /// </summary>
    public record InitializeVenvCommand(Guid AgentProfileId) : ICommand<string>;

    /// <summary>
    /// Handler xử lý khởi tạo .venv cho Agent.
    /// </summary>
    public class InitializeVenvCommandHandler(
        IAgentsDbContext dbContext,
        IPythonSandboxRunner pythonRunner) : ICommandHandler<InitializeVenvCommand, string>
    {
        /// <inheritdoc />
        public async Task<Result<string>> Handle(
            InitializeVenvCommand request,
            CancellationToken cancellationToken)
        {
            var initResult = await pythonRunner.EnsureVenvInitializedAsync(request.AgentProfileId, cancellationToken);
            if (!initResult.IsSuccess)
            {
                return initResult;
            }

            var config = await dbContext.AgentWorkspaceConfigs
                .FirstOrDefaultAsync(c => c.AgentProfileId == request.AgentProfileId, cancellationToken);

            if (config == null)
            {
                var createResult = AgentWorkspaceConfig.Create(request.AgentProfileId);
                if (createResult.IsSuccess)
                {
                    config = createResult.Value;
                    config.MarkVenvReady();
                    dbContext.AgentWorkspaceConfigs.Add(config);
                }
            }
            else
            {
                config.MarkVenvReady();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return initResult;
        }
    }
}
