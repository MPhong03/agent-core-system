// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;
using AgentCoreSystem.Module.Agents.Application.Workspace.DTOs;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Workspace.Queries
{
    /// <summary>
    /// Truy vấn thông tin cấu hình và thống kê không gian làm việc của Agent.
    /// </summary>
    public record GetAgentWorkspaceQuery(Guid AgentProfileId) : IQuery<AgentWorkspaceDetailDto>;

    /// <summary>
    /// Handler xử lý truy vấn không gian làm việc của Agent.
    /// </summary>
    public class GetAgentWorkspaceQueryHandler(
        IAgentsDbContext dbContext,
        IWorkspaceManager workspaceManager) : IQueryHandler<GetAgentWorkspaceQuery, AgentWorkspaceDetailDto>
    {
        /// <inheritdoc />
        public async Task<Result<AgentWorkspaceDetailDto>> Handle(
            GetAgentWorkspaceQuery request,
            CancellationToken cancellationToken)
        {
            var config = await dbContext.AgentWorkspaceConfigs
                .FirstOrDefaultAsync(c => c.AgentProfileId == request.AgentProfileId, cancellationToken);

            if (config == null)
            {
                var createResult = AgentWorkspaceConfig.Create(request.AgentProfileId);
                if (!createResult.IsSuccess)
                {
                    return Result.Failure<AgentWorkspaceDetailDto>(createResult.Error);
                }

                config = createResult.Value;
                dbContext.AgentWorkspaceConfigs.Add(config);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var statsResult = await workspaceManager.GetWorkspaceStatsAsync(request.AgentProfileId, cancellationToken);
            var stats = statsResult.IsSuccess
                ? statsResult.Value
                : new WorkspaceStatsDto(request.AgentProfileId, workspaceManager.GetAgentWorkspacePath(request.AgentProfileId), false, 0, 0, 0, 0);

            if (stats.VenvExists && !config.IsVenvReady)
            {
                config.MarkVenvReady();
                config.UpdateStorageUsed(stats.TotalSizeBytes);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var dto = new AgentWorkspaceDetailDto(
                config.Id,
                config.AgentProfileId,
                stats.RootPath,
                config.PythonVersion,
                config.MaxTimeoutSeconds,
                config.IsVenvReady,
                config.IsActive,
                stats.TotalSizeBytes,
                stats.TotalFilesCount,
                stats.ScriptsCount,
                stats.ArtifactsCount,
                config.LastActiveAt
            );

            return Result.Success(dto);
        }
    }
}
