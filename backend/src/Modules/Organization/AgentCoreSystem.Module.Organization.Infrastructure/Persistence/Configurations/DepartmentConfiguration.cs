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
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="Department"/>.
    /// </summary>
    /// <remarks>
    /// <para>Ánh xạ bảng <c>Departments</c> trên schema 'org' kèm ràng buộc chỉ mục duy nhất trên trường <c>Name</c>.</para>
    /// <para>🔴 <strong>Ràng buộc xóa:</strong> DeleteBehavior.Restrict để không tự xóa các kênh làm việc trực thuộc khi xóa phòng ban.</para>
    /// </remarks>
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.ToTable("Departments");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(d => d.Description)
                .HasMaxLength(500);

            builder.HasIndex(d => d.Name)
                .IsUnique();

            builder.HasMany(d => d.Channels)
                .WithOne(c => c.Department)
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(d => !d.IsDeleted);
        }
    }
}
