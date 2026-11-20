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
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="TeamChannel"/>.
    /// </summary>
    /// <remarks>
    /// <para>Ánh xạ bảng <c>Channels</c> trên schema 'org' kèm ràng buộc chỉ mục duy nhất kết hợp <c>(DepartmentId, Name)</c>.</para>
    /// <para>⇒ Xóa kênh sẽ tự động xóa toàn bộ liên kết thành viên trong kênh đó (DeleteBehavior.Cascade).</para>
    /// </remarks>
    public class TeamChannelConfiguration : IEntityTypeConfiguration<TeamChannel>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<TeamChannel> builder)
        {
            builder.ToTable("Channels");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(c => c.Description)
                .HasMaxLength(500);

            // Ràng buộc tên kênh là duy nhất trong cùng một phòng ban
            builder.HasIndex(c => new { c.DepartmentId, c.Name })
                .IsUnique();

            builder.HasMany(c => c.Members)
                .WithOne(m => m.Channel)
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
}
