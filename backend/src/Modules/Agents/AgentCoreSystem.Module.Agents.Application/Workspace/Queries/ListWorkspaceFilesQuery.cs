// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Agents.Application.Workspace.Queries
{
    /// <summary>
    /// Truy vấn danh sách các file trong workspace của Agent theo phân mục (scripts, artifacts, logs).
    /// </summary>
    public record ListWorkspaceFilesQuery(
        Guid AgentProfileId,
        string? Category = null) : IQuery<IReadOnlyList<WorkspaceFileDto>>;

    /// <summary>
    /// Handler xử lý liệt kê file trong workspace.
    /// </summary>
    public class ListWorkspaceFilesQueryHandler(
        IWorkspaceManager workspaceManager) : IQueryHandler<ListWorkspaceFilesQuery, IReadOnlyList<WorkspaceFileDto>>
    {
        /// <inheritdoc />
        public Task<Result<IReadOnlyList<WorkspaceFileDto>>> Handle(
            ListWorkspaceFilesQuery request,
            CancellationToken cancellationToken)
        {
            return workspaceManager.ListFilesAsync(request.AgentProfileId, request.Category, cancellationToken);
        }
    }
}
