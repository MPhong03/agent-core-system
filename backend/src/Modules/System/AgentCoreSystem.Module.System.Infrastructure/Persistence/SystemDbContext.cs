// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.System.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.System.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext độc quyền cho phân hệ System, quản lý dữ liệu trên schema 'system'.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Chứa các bảng Microsoft Identity và AuditLog của phân hệ System.</para>
    /// <para><strong>Why:</strong> Thiết lập HasDefaultSchema("system") và MigrationsHistoryTable riêng để đảm bảo tính độc lập và tự chủ (Autonomy) theo kiến trúc Modular Monolith.</para>
    /// </remarks>
    public class SystemDbContext(DbContextOptions<SystemDbContext> options) 
        : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
    {
        public const string SchemaName = "system";

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Đặt toàn bộ các bảng vào schema độc quyền 'system'
            builder.HasDefaultSchema(SchemaName);

            builder.Entity<ApplicationUser>(b =>
            {
                b.ToTable("Users");
                b.HasQueryFilter(u => !u.IsDeleted);
                b.Property(u => u.FullName).HasMaxLength(150);
            });

            builder.Entity<ApplicationRole>(b =>
            {
                b.ToTable("Roles");
                b.Property(r => r.Description).HasMaxLength(250);
            });

            builder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("UserRoles"));
            builder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("UserClaims"));
            builder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("UserLogins"));
            builder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("RoleClaims"));
            builder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("UserTokens"));

            builder.Entity<AuditLog>(b =>
            {
                b.ToTable("AuditLogs");
                b.HasKey(a => a.Id);
                b.Property(a => a.Action).HasMaxLength(100).IsRequired();
                b.Property(a => a.EntityName).HasMaxLength(150).IsRequired();
                b.Property(a => a.EntityId).HasMaxLength(100);
            });
        }
    }
}

