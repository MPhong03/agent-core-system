// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Organization.Application.Contracts;
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
    /// <para>🔴 <strong>Lưu ý:</strong> Bảng lịch sử di chuyển dữ liệu được tách riêng thành <c>__EFMigrationsHistory_Organization</c>.</para>
    /// <para>⛔ <strong>Cấm:</strong> Tuyệt đối không tạo Foreign Key vật lý hoặc truy vấn trực tiếp bảng của các module khác.</para>
    /// </remarks>
    public class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : BaseDbContext(options), IOrganizationDbContext
    {
        // ── Schema & Constants ──

        /// <summary>
        /// Tên schema cơ sở dữ liệu độc quyền của phân hệ Organization.
        /// </summary>
        public const string SchemaName = "org";

        // ── DbSets ──

        /// <inheritdoc />
        public DbSet<Department> Departments => Set<Department>();

        /// <inheritdoc />
        public DbSet<TeamChannel> Channels => Set<TeamChannel>();

        /// <inheritdoc />
        public DbSet<Member> Members => Set<Member>();

        /// <inheritdoc />
        public DbSet<AgentProfile> AgentProfiles => Set<AgentProfile>();

        // ── Model Configuration ──

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema(SchemaName);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly);
        }
    }
}
