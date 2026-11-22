// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Agents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentCoreSystem.Module.Agents.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="AgentWorkspaceConfig"/>.
    /// </summary>
    public class AgentWorkspaceConfigConfiguration : IEntityTypeConfiguration<AgentWorkspaceConfig>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<AgentWorkspaceConfig> builder)
        {
            builder.ToTable("AgentWorkspaceConfigs");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.AgentProfileId)
                .IsRequired();

            builder.HasIndex(c => c.AgentProfileId)
                .IsUnique();

            builder.Property(c => c.PythonVersion)
                .HasMaxLength(64)
                .IsRequired(false);

            builder.Property(c => c.MaxTimeoutSeconds)
                .HasDefaultValue(60)
                .IsRequired();

            builder.Property(c => c.IsVenvReady)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(c => c.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(c => c.StorageUsedBytes)
                .HasDefaultValue(0L)
                .IsRequired();

            builder.Property(c => c.LastActiveAt)
                .IsRequired(false);

            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
}
