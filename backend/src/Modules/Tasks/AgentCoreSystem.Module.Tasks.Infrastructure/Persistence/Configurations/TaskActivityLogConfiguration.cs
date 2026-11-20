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
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="TaskActivityLog"/>.
    /// </summary>
    public class TaskActivityLogConfiguration : IEntityTypeConfiguration<TaskActivityLog>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<TaskActivityLog> builder)
        {
            builder.ToTable("TaskActivityLogs");

            builder.HasKey(log => log.Id);

            builder.Property(log => log.TaskId)
                .IsRequired();

            builder.Property(log => log.ActorMemberId)
                .IsRequired();

            builder.Property(log => log.ActionType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(log => log.FromStatus)
                .HasConversion<int>()
                .IsRequired(false);

            builder.Property(log => log.ToStatus)
                .HasConversion<int>()
                .IsRequired(false);

            builder.Property(log => log.Comment)
                .HasMaxLength(2000)
                .IsRequired(false);

            builder.HasIndex(log => new { log.TaskId, log.CreatedDate });

            builder.HasQueryFilter(log => !log.IsDeleted);
        }
    }
}
