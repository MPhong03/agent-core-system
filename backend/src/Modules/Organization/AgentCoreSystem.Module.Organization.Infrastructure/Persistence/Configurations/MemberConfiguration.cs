// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Organization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentCoreSystem.Module.Organization.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="Member"/>.
    /// </summary>
    /// <remarks>
    /// <para>Ánh xạ bảng <c>Members</c> trên schema 'org' kèm ràng buộc chỉ mục duy nhất kết hợp <c>(ChannelId, DisplayName)</c>.</para>
    /// <para>🔴 <strong>Lưu ý:</strong> Liên kết sang hồ sơ Agent sử dụng DeleteBehavior.Restrict để bảo toàn danh tính thành viên.</para>
    /// </remarks>
    public class MemberConfiguration : IEntityTypeConfiguration<Member>
    {
        /// <inheritdoc />
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
