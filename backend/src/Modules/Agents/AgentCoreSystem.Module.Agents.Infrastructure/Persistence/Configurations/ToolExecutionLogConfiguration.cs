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
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="ToolExecutionLog"/>.
    /// </summary>
    public class ToolExecutionLogConfiguration : IEntityTypeConfiguration<ToolExecutionLog>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<ToolExecutionLog> builder)
        {
            builder.ToTable("ToolExecutionLogs");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.ToolDefinitionId)
                .IsRequired();

            builder.Property(l => l.AgentProfileId)
                .IsRequired();

            builder.Property(l => l.CallerType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(l => l.ExecutionSourceId)
                .IsRequired();

            builder.Property(l => l.DurationMs)
                .IsRequired();

            builder.Property(l => l.IsSuccess)
                .IsRequired();

            builder.Property(l => l.ErrorMessage)
                .HasMaxLength(4000);

            builder.HasOne(l => l.ToolDefinition)
                .WithMany(t => t.ExecutionLogs)
                .HasForeignKey(l => l.ToolDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(l => l.ToolDefinitionId);
            builder.HasIndex(l => l.AgentProfileId);
            builder.HasIndex(l => l.ExecutionSourceId);

            builder.HasQueryFilter(l => !l.IsDeleted);
        }
    }
}
