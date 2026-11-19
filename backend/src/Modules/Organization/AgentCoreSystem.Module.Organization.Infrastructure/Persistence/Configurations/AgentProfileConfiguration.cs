// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Organization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentCoreSystem.Module.Organization.Infrastructure.Persistence.Configurations
{
    public class AgentProfileConfiguration : IEntityTypeConfiguration<AgentProfile>
    {
        public void Configure(EntityTypeBuilder<AgentProfile> builder)
        {
            builder.ToTable("AgentProfiles");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(a => a.AvatarUrl)
                .HasMaxLength(500);

            builder.Property(a => a.Provider)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(a => a.ModelName)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(a => a.SystemPrompt)
                .IsRequired();

            builder.Property(a => a.Temperature)
                .HasPrecision(3, 2);

            builder.HasQueryFilter(a => !a.IsDeleted);
        }
    }
}
