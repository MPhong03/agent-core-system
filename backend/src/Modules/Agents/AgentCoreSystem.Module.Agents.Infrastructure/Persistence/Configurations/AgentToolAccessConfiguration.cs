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
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="AgentToolAccess"/>.
    /// </summary>
    public class AgentToolAccessConfiguration : IEntityTypeConfiguration<AgentToolAccess>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<AgentToolAccess> builder)
        {
            builder.ToTable("AgentToolAccesses");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.AgentProfileId)
                .IsRequired();

            builder.Property(a => a.ToolDefinitionId)
                .IsRequired();

            builder.Property(a => a.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            builder.HasOne(a => a.ToolDefinition)
                .WithMany(t => t.ToolAccesses)
                .HasForeignKey(a => a.ToolDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(a => new { a.AgentProfileId, a.ToolDefinitionId });

            builder.HasQueryFilter(a => !a.IsDeleted);
        }
    }
}
