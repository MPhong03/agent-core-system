// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Tasks.Application.Contracts;
using AgentCoreSystem.Module.Tasks.Domain.Entities;
using AgentCoreSystem.Shared.Kernel.Primitives;
using AgentCoreSystem.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Tasks.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext độc quyền cho phân hệ Tasks, quản lý dữ liệu trên schema 'task'.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Quản lý các thực thể Tác vụ (TaskItem), Phân công (TaskAssignment), Sản phẩm đầu ra (TaskArtifact) và Nhật ký hoạt động (TaskActivityLog).</para>
    /// <para><strong>Why:</strong> Thiết lập HasDefaultSchema("task") và MigrationsHistoryTable riêng để đảm bảo tính độc lập và tự chủ (Autonomy) theo kiến trúc Modular Monolith.</para>
    /// <para>🔴 <strong>Lưu ý:</strong> Bảng lịch sử di chuyển dữ liệu được tách riêng thành <c>__EFMigrationsHistory_Tasks</c>.</para>
    /// <para>⛔ <strong>Cấm:</strong> Không tạo Foreign Key vật lý sang các schema khác (org, chat, system).</para>
    /// </remarks>
    public class TasksDbContext(DbContextOptions<TasksDbContext> options) : BaseDbContext(options), ITasksDbContext
    {
        // ── Schema & Constants ──

        /// <summary>
        /// Tên schema cơ sở dữ liệu độc quyền của phân hệ Tasks.
        /// </summary>
        public const string SchemaName = "task";

        // ── DbSets ──

        /// <inheritdoc />
        public DbSet<TaskItem> Tasks => Set<TaskItem>();

        /// <inheritdoc />
        public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();

        /// <inheritdoc />
        public DbSet<TaskArtifact> TaskArtifacts => Set<TaskArtifact>();

        /// <inheritdoc />
        public DbSet<TaskActivityLog> TaskActivityLogs => Set<TaskActivityLog>();

        // ── Model Configuration ──

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema(SchemaName);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TasksDbContext).Assembly);
        }

        /// <inheritdoc />
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Modified &&
                    entry.Entity is TaskAssignment or TaskArtifact or TaskActivityLog)
                {
                    // Các thực thể phụ thuộc (assignments, artifacts, logs) được sinh mã UUIDv7 từ constructor.
                    // Khi gắn vào thực thể TaskItem đã được track, ChangeTracker mặc định gán Modified do khóa đã có giá trị.
                    // Chuyển sang Added nếu thực thể chưa từng có UpdatedDate (mới tạo).
                    if (entry.Entity is BaseEntity baseEntity && baseEntity.UpdatedDate == null)
                    {
                        entry.State = EntityState.Added;
                    }
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
