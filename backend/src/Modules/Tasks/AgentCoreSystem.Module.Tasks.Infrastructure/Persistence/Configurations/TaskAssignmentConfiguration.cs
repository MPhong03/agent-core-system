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
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="TaskAssignment"/>.
    /// </summary>
    public class TaskAssignmentConfiguration : IEntityTypeConfiguration<TaskAssignment>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<TaskAssignment> builder)
        {
            builder.ToTable("TaskAssignments");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.TaskId)
                .IsRequired();

            builder.Property(a => a.AssigneeMemberId)
                .IsRequired();

            builder.Property(a => a.IsAgent)
                .IsRequired();

            builder.Property(a => a.RoleInTask)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(a => new { a.TaskId, a.AssigneeMemberId });

            builder.HasQueryFilter(a => !a.IsDeleted);
        }
    }
}
