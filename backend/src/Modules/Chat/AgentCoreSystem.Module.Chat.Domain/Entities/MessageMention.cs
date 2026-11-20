// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Chat.Domain.Entities
{
    /// <summary>
    /// Thực thể đại diện cho một lượt nhắc tên (@mention) thành viên trong tin nhắn trò chuyện.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Lưu vết thành viên (Human hoặc Agent) được nhắc đến cụ thể trong một tin nhắn của kênh.</para>
    /// <para><strong>Why:</strong> Phục vụ cơ chế Mention Router phân tích tin nhắn và kích hoạt luồng trả lời tự động của Agent trong kênh tương ứng.</para>
    /// </remarks>
    public class MessageMention : BaseEntity
    {
        // ── Constructors ──

        /// <summary>
        /// Khởi tạo phục vụ Entity Framework Core deserialization.
        /// </summary>
        protected MessageMention() : base()
        {
        }

        /// <summary>
        /// Khởi tạo một bản ghi nhắc tên thành viên mới với UUIDv7.
        /// </summary>
        /// <param name="messageId">Mã định danh tin nhắn chứa lượt nhắc.</param>
        /// <param name="mentionedMemberId">Mã định danh thành viên được nhắc tên.</param>
        public MessageMention(Guid messageId, Guid mentionedMemberId) : base()
        {
            MessageId = messageId;
            MentionedMemberId = mentionedMemberId;
        }

        // ── Identifiers & Relationships ──

        /// <summary>
        /// Mã định danh tin nhắn cha chứa lượt nhắc tên này.
        /// </summary>
        /// <remarks>Khóa ngoại vật lý tham chiếu tới bảng <see cref="ChatMessage"/>.</remarks>
        public Guid MessageId { get; private set; }

        /// <summary>
        /// Mã định danh thành viên được nhắc tới (tham chiếu logic tới org.Members).
        /// </summary>
        /// <remarks>
        /// <para>⛔ <strong>Cấm:</strong> Không thiết lập Foreign Key vật lý sang bảng org.Members.</para>
        /// <para>⇒ Được Mention Dispatcher sử dụng để phân loại thành viên Human hay Agent nhằm kích hoạt phản hồi tự động.</para>
        /// </remarks>
        public Guid MentionedMemberId { get; private set; }

        // ── Navigation ──

        /// <summary>
        /// Tham chiếu điều hướng tới tin nhắn chứa lượt nhắc.
        /// </summary>
        public virtual ChatMessage? Message { get; private set; }
    }
}
