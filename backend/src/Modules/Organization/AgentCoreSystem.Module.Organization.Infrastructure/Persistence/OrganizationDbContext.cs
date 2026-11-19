// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Organization.Domain.Entities;
using AgentCoreSystem.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Organization.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext độc quyền cho phân hệ Organization, quản lý dữ liệu trên schema 'org'.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Quản lý các thực thể Phòng ban, Kênh nhóm, Thành viên và Hồ sơ Agent.</para>
    /// <para><strong>Why:</strong> Thiết lập HasDefaultSchema("org") và MigrationsHistoryTable riêng để đảm bảo tính độc lập và tự chủ (Autonomy) theo kiến trúc Modular Monolith.</para>
    /// </remarks>
    public class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : BaseDbContext(options)
    {
        public const string SchemaName = "org";

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<TeamChannel> Channels => Set<TeamChannel>();
        public DbSet<Member> Members => Set<Member>();
        public DbSet<AgentProfile> AgentProfiles => Set<AgentProfile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema(SchemaName);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly);
        }
    }
}
