// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Domain.Entities;
using AgentCoreSystem.Shared.Kernel.Responses;

namespace AgentCoreSystem.Module.System.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<PagedResult<ApplicationUser>> GetPagedAsync(int pageIndex, int pageSize, string? keyword = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    }
}

