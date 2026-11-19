// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Chat.Application.Contracts;
using AgentCoreSystem.Module.Chat.Domain.Entities;
using AgentCoreSystem.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Chat.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext độc quyền cho phân hệ Chat, quản lý dữ liệu trên schema 'chat'.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Quản lý các thực thể tin nhắn trò chuyện (ChatMessage) và lượt nhắc tên (@mention) của phân hệ Chat.</para>
    /// <para><strong>Why:</strong> Thiết lập HasDefaultSchema("chat") và MigrationsHistoryTable riêng để đảm bảo tính độc lập và tự chủ (Autonomy) theo kiến trúc Modular Monolith.</para>
    /// </remarks>
    public class ChatDbContext(DbContextOptions<ChatDbContext> options) : BaseDbContext(options), IChatDbContext
    {
        public const string SchemaName = "chat";

        public DbSet<ChatMessage> Messages => Set<ChatMessage>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<MessageMention> Mentions => Set<MessageMention>();
        public DbSet<MessageMention> MessageMentions => Set<MessageMention>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema(SchemaName);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
        }
    }
}
