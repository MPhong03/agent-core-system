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
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="ScriptExecutionHistory"/>.
    /// </summary>
    public class ScriptExecutionHistoryConfiguration : IEntityTypeConfiguration<ScriptExecutionHistory>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<ScriptExecutionHistory> builder)
        {
            builder.ToTable("ScriptExecutionHistories");

            builder.HasKey(h => h.Id);

            builder.Property(h => h.AgentProfileId)
                .IsRequired();

            builder.HasIndex(h => h.AgentProfileId);

            builder.Property(h => h.CallerType)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(h => h.ExecutionSourceId)
                .IsRequired();

            builder.HasIndex(h => h.ExecutionSourceId);

            builder.Property(h => h.ScriptName)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(h => h.ScriptContent)
                .IsRequired();

            builder.Property(h => h.ExitCode)
                .IsRequired();

            builder.Property(h => h.Stdout)
                .IsRequired(false);

            builder.Property(h => h.Stderr)
                .IsRequired(false);

            builder.Property(h => h.DurationMs)
                .IsRequired();

            builder.Property(h => h.IsTimeout)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(h => h.ArtifactsJson)
                .IsRequired(false);

            builder.HasQueryFilter(h => !h.IsDeleted);
        }
    }
}
