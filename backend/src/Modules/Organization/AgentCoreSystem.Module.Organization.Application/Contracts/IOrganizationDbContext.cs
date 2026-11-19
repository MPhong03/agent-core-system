// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Organization.Application.Contracts
{
    /// <summary>
    /// Giao diện DbContext cho phân hệ Organization.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Cung cấp các DbSet thực thể phòng ban, kênh nhóm, thành viên và hồ sơ Agent.</para>
    /// <para><strong>Why:</strong> Tuân thủ Clean Architecture, cho phép Application truy cập dữ liệu mà không bị phụ thuộc vào chi tiết tầng Infrastructure.</para>
    /// </remarks>
    public interface IOrganizationDbContext
    {
        DbSet<Department> Departments { get; }
        DbSet<TeamChannel> Channels { get; }
        DbSet<Member> Members { get; }
        DbSet<AgentProfile> AgentProfiles { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
