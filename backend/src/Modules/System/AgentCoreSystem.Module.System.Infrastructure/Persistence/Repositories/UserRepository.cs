// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Domain.Entities;
using AgentCoreSystem.Module.System.Domain.Repositories;
using AgentCoreSystem.Module.System.Infrastructure.Persistence;
using AgentCoreSystem.Shared.Kernel.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.System.Infrastructure.Persistence.Repositories
{
    public class UserRepository(
        SystemDbContext dbContext,
        UserManager<ApplicationUser> userManager) : IUserRepository
    {
        public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<PagedResult<ApplicationUser>> GetPagedAsync(
            int pageIndex, 
            int pageSize, 
            string? keyword = null, 
            CancellationToken cancellationToken = default)
        {
            var query = dbContext.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmed = keyword.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(trimmed) || 
                                         (u.Email != null && u.Email.ToLower().Contains(trimmed)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(u => u.CreatedDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<ApplicationUser>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            var roles = await userManager.GetRolesAsync(user);
            return [.. roles];
        }
    }
}

