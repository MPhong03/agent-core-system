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
    /// <para>🔴 <strong>Lưu ý:</strong> Bảng lịch sử di chuyển dữ liệu được tách riêng thành <c>__EFMigrationsHistory_Chat</c>.</para>
    /// <para>⛔ <strong>Cấm:</strong> Không tạo Foreign Key vật lý sang các schema khác (org, system).</para>
    /// </remarks>
    public class ChatDbContext(DbContextOptions<ChatDbContext> options) : BaseDbContext(options), IChatDbContext
    {
        // ── Schema & Constants ──

        /// <summary>
        /// Tên schema cơ sở dữ liệu độc quyền của phân hệ Chat.
        /// </summary>
        public const string SchemaName = "chat";

        // ── DbSets ──

        /// <inheritdoc />
        public DbSet<ChatMessage> Messages => Set<ChatMessage>();

        /// <inheritdoc />
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

        /// <inheritdoc />
        public DbSet<MessageMention> Mentions => Set<MessageMention>();

        /// <inheritdoc />
        public DbSet<MessageMention> MessageMentions => Set<MessageMention>();

        /// <inheritdoc />
        public DbSet<PendingToolCall> PendingToolCalls => Set<PendingToolCall>();

        // ── Model Configuration ──

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema(SchemaName);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
        }
    }
}
