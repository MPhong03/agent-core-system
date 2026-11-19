// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Organization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentCoreSystem.Module.Organization.Infrastructure.Persistence.Configurations
{
    public class MemberConfiguration : IEntityTypeConfiguration<Member>
    {
        public void Configure(EntityTypeBuilder<Member> builder)
        {
            builder.ToTable("Members");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.DisplayName)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(m => m.Type)
                .HasConversion<int>()
                .IsRequired();

            // Tên hiển thị là duy nhất trong một kênh
            builder.HasIndex(m => new { m.ChannelId, m.DisplayName })
                .IsUnique();

            builder.HasOne(m => m.AgentProfile)
                .WithMany(a => a.Members)
                .HasForeignKey(m => m.AgentProfileId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(m => !m.IsDeleted);
        }
    }
}
