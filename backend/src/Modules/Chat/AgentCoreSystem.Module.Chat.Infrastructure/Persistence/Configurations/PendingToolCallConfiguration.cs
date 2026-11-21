// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentCoreSystem.Module.Chat.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="PendingToolCall"/>.
    /// </summary>
    public class PendingToolCallConfiguration : IEntityTypeConfiguration<PendingToolCall>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<PendingToolCall> builder)
        {
            builder.ToTable("PendingToolCalls");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.ChannelId)
                .IsRequired();

            builder.Property(p => p.ChatMessageId)
                .IsRequired();

            builder.Property(p => p.ToolCallId)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(p => p.ToolName)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(p => p.ArgumentsJson)
                .IsRequired();

            builder.Property(p => p.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(p => p.ExpiresAt)
                .IsRequired();

            builder.HasIndex(p => new { p.ChannelId, p.Status });
            builder.HasIndex(p => p.ToolCallId);

            builder.HasQueryFilter(p => !p.IsDeleted);
        }
    }
}
