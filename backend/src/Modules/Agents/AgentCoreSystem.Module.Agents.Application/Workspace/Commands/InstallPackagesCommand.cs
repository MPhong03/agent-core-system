// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Application.Services.Python;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Workspace.Commands
{
    /// <summary>
    /// Lệnh cài đặt các thư viện Python qua pip vào .venv của Agent.
    /// </summary>
    public record InstallPackagesCommand(
        Guid AgentProfileId,
        IReadOnlyList<string> Packages) : ICommand<string>;

    /// <summary>
    /// Handler xử lý cài đặt packages qua pip.
    /// </summary>
    public class InstallPackagesCommandHandler(
        IAgentsDbContext dbContext,
        IPythonSandboxRunner pythonRunner,
        IWorkspaceManager workspaceManager) : ICommandHandler<InstallPackagesCommand, string>
    {
        /// <inheritdoc />
        public async Task<Result<string>> Handle(
            InstallPackagesCommand request,
            CancellationToken cancellationToken)
        {
            var installResult = await pythonRunner.InstallPackagesAsync(request.AgentProfileId, request.Packages, cancellationToken);
            if (!installResult.IsSuccess)
            {
                return installResult;
            }

            var config = await dbContext.AgentWorkspaceConfigs
                .FirstOrDefaultAsync(c => c.AgentProfileId == request.AgentProfileId, cancellationToken);

            if (config != null)
            {
                config.RecordActivity();
                var stats = await workspaceManager.GetWorkspaceStatsAsync(request.AgentProfileId, cancellationToken);
                if (stats.IsSuccess)
                {
                    config.UpdateStorageUsed(stats.Value.TotalSizeBytes);
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return installResult;
        }
    }
}
