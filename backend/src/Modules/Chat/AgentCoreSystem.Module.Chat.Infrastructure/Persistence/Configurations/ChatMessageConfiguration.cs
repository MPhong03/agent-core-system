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
    /// Cấu hình ánh xạ Entity Framework Core cho thực thể <see cref="ChatMessage"/>.
    /// </summary>
    /// <remarks>
    /// <para>Ánh xạ bảng <c>ChatMessages</c> trên schema 'chat' với các chỉ mục tối ưu cho phân trang và sliding window context.</para>
    /// <para>🔴 <strong>Chỉ mục hiệu năng:</strong> Index kết hợp <c>(ChannelId, CreatedDate)</c> để tăng tốc truy vấn 20 tin nhắn gần nhất.</para>
    /// <para>⇒ Áp dụng Global Query Filter lọc các tin nhắn bị xóa mềm (<c>!IsDeleted</c>).</para>
    /// </remarks>
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("ChatMessages");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.ChannelId)
                .IsRequired();

            builder.Property(m => m.SenderMemberId)
                .IsRequired();

            builder.Property(m => m.SenderDisplayName)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(m => m.IsAgent)
                .IsRequired();

            builder.Property(m => m.Content)
                .IsRequired();

            builder.Property(m => m.CallDepth)
                .IsRequired();

            builder.Property(m => m.ParentMessageId)
                .IsRequired(false);

            // Index trên (ChannelId, CreatedDate) hỗ trợ truy vấn sliding window 20 tin nhắn
            builder.HasIndex(m => new { m.ChannelId, m.CreatedDate });

            builder.HasMany(m => m.Mentions)
                .WithOne(men => men.Message)
                .HasForeignKey(men => men.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(m => !m.IsDeleted);
        }
    }
}
