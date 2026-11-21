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
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="ToolDefinition"/>.
    /// </summary>
    public class ToolDefinitionConfiguration : IEntityTypeConfiguration<ToolDefinition>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<ToolDefinition> builder)
        {
            builder.ToTable("Tools");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.HasIndex(t => t.Name)
                .IsUnique();

            builder.Property(t => t.DisplayName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(t => t.Description)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(t => t.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(t => t.EndpointUrl)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(t => t.HttpMethod)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(t => t.HeadersConfig)
                .IsRequired();

            builder.Property(t => t.ParametersSchema)
                .IsRequired();

            builder.Property(t => t.RequiresConfirmation)
                .IsRequired();

            builder.Property(t => t.TimeoutSeconds)
                .HasDefaultValue(30)
                .IsRequired();

            builder.Property(t => t.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            builder.HasQueryFilter(t => !t.IsDeleted);
        }
    }
}
