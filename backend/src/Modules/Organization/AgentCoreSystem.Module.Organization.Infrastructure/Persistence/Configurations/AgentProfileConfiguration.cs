// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Organization.Domain.Entities;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentCoreSystem.Module.Organization.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="AgentProfile"/>.
    /// </summary>
    /// <remarks>
    /// <para>Ánh xạ bảng <c>AgentProfiles</c> trên schema 'org' với các ràng buộc kiểu dữ liệu, độ dài và soft delete.</para>
    /// <para>🔴 <strong>Ràng buộc:</strong> Áp dụng Global Query Filter loại bỏ bản ghi bị xóa mềm (<c>!IsDeleted</c>).</para>
    /// </remarks>
    public class AgentProfileConfiguration : IEntityTypeConfiguration<AgentProfile>
    {
        /// <inheritdoc />
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

            builder.Property(a => a.ToolExecutionMode)
                .HasConversion<int>()
                .HasDefaultValue(ToolExecutionMode.SemiAuto)
                .HasSentinel(ToolExecutionMode.SemiAuto)
                .IsRequired();

            builder.HasQueryFilter(a => !a.IsDeleted);
        }
    }
}
