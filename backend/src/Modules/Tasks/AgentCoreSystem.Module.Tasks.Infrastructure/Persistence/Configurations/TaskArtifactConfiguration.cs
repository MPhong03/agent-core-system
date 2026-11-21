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
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="TaskArtifact"/>.
    /// </summary>
    public class TaskArtifactConfiguration : IEntityTypeConfiguration<TaskArtifact>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<TaskArtifact> builder)
        {
            builder.ToTable("TaskArtifacts");

            builder.HasKey(art => art.Id);

            builder.Property(art => art.TaskId)
                .IsRequired();

            builder.Property(art => art.FileName)
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(art => art.FilePath)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(art => art.FileType)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(art => art.FileSize)
                .IsRequired();

            builder.Property(art => art.Summary)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(art => art.CreatedByMemberId)
                .IsRequired();

            builder.HasIndex(art => art.TaskId);

            builder.HasQueryFilter(art => !art.IsDeleted);
        }
    }
}
