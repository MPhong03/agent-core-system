// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Tasks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentCoreSystem.Module.Tasks.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="TaskItem"/>.
    /// </summary>
    public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.ToTable("Tasks");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.ChannelId)
                .IsRequired();

            builder.Property(t => t.TaskCode)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(t => t.Title)
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(t => t.Description)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(t => t.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(t => t.Priority)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(t => t.DueDate)
                .IsRequired(false);

            builder.Property(t => t.StartedDate)
                .IsRequired(false);

            builder.Property(t => t.CompletedDate)
                .IsRequired(false);

            // Index trên (ChannelId, Status) hỗ trợ truy vấn bảng Kanban theo cột
            builder.HasIndex(t => new { t.ChannelId, t.Status });

            // Index duy nhất trên (ChannelId, TaskCode)
            builder.HasIndex(t => new { t.ChannelId, t.TaskCode })
                .IsUnique();

            builder.HasMany(t => t.Assignments)
                .WithOne()
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Artifacts)
                .WithOne()
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.ActivityLogs)
                .WithOne()
                .HasForeignKey(l => l.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(t => t.Assignments).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(t => t.Artifacts).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(t => t.ActivityLogs).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasQueryFilter(t => !t.IsDeleted);
        }
    }
}
