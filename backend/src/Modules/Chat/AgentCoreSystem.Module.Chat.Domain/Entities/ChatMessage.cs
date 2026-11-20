// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Chat.Domain.Entities
{
    /// <summary>
    /// Thực thể đại diện cho tin nhắn trò chuyện trong kênh làm việc.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Lưu trữ nội dung trao đổi thời gian thực giữa Người dùng và các Nhân sự AI (Agents) trong Kênh.</para>
    /// <para><strong>Why:</strong> Phục vụ lịch sử trò chuyện kênh nhóm, cửa sổ trượt Sliding Window 20 tin nhắn cho LLM Context, và điều phối chuỗi hội thoại Agent-to-Agent với kiểm soát độ sâu <see cref="CallDepth"/>.</para>
    /// </remarks>
    public class ChatMessage : BaseEntity
    {
        // ── Constructors ──

        /// <summary>
        /// Khởi tạo phục vụ Entity Framework Core deserialization.
        /// </summary>
        protected ChatMessage() : base()
        {
            SenderDisplayName = string.Empty;
            Content = string.Empty;
            Mentions = new List<MessageMention>();
        }

        /// <summary>
        /// Khởi tạo một tin nhắn trò chuyện mới trong kênh với khóa chính UUIDv7.
        /// </summary>
        /// <param name="channelId">Mã định danh kênh trò chuyện.</param>
        /// <param name="senderMemberId">Mã định danh thành viên gửi tin nhắn.</param>
        /// <param name="senderDisplayName">Tên hiển thị của người gửi tại thời điểm gửi tin.</param>
        /// <param name="isAgent">Cờ xác định tin nhắn được gửi từ nhân sự AI hay người dùng thật.</param>
        /// <param name="content">Nội dung văn bản của tin nhắn.</param>
        /// <param name="callDepth">Độ sâu cuộc gọi phản hồi tự động giữa các agent (mặc định 0).</param>
        /// <param name="parentMessageId">Mã định danh tin nhắn cha được phản hồi (nếu có).</param>
        /// <param name="id">Mã định danh tin nhắn cấp trước (UUIDv7, tùy chọn).</param>
        public ChatMessage(
            Guid channelId,
            Guid senderMemberId,
            string senderDisplayName,
            bool isAgent,
            string content,
            int callDepth = 0,
            Guid? parentMessageId = null,
            Guid? id = null) : base(id ?? Guid.Empty)
        {
            ChannelId = channelId;
            SenderMemberId = senderMemberId;
            SenderDisplayName = senderDisplayName ?? string.Empty;
            IsAgent = isAgent;
            Content = content ?? string.Empty;
            CallDepth = callDepth;
            ParentMessageId = parentMessageId;
            Mentions = new List<MessageMention>();
        }

        // ── Metadata & Routing ──

        /// <summary>
        /// Mã định danh kênh trò chuyện chứa tin nhắn (tham chiếu logic tới org.TeamChannels).
        /// </summary>
        /// <remarks>
        /// <para>⛔ <strong>Cấm:</strong> Không tạo Foreign Key vật lý sang bảng org.TeamChannels.</para>
        /// <para>🔴 <strong>Lưu ý:</strong> Kết hợp cùng CreatedDate trong Index Clustered để tối ưu truy vấn sliding window.</para>
        /// </remarks>
        public Guid ChannelId { get; private set; }

        /// <summary>
        /// Mã định danh thành viên gửi tin nhắn (tham chiếu logic tới org.Members).
        /// </summary>
        /// <remarks>
        /// <para>⛔ <strong>Cấm:</strong> Không tạo Foreign Key vật lý sang bảng org.Members.</para>
        /// </remarks>
        public Guid SenderMemberId { get; private set; }

        /// <summary>
        /// Tên hiển thị người gửi tại thời điểm tin nhắn được tạo.
        /// </summary>
        /// <remarks>Chụp nhanh (Snapshot) tên hiển thị để không bị phụ thuộc nếu thành viên đổi tên sau này.</remarks>
        public string SenderDisplayName { get; private set; }

        /// <summary>
        /// Cờ xác định tin nhắn do AI Agent gửi (<c>true</c>) hay người dùng thật (<c>false</c>).
        /// </summary>
        /// <remarks>⇒ Giúp Client hiển thị đúng huy hiệu Agent/Human và định dạng giao diện tương ứng.</remarks>
        public bool IsAgent { get; private set; }

        // ── Content & Structure ──

        /// <summary>
        /// Nội dung văn bản của tin nhắn trò chuyện.
        /// </summary>
        /// <remarks>Định dạng văn bản thuần hoặc Markdown từ câu trả lời của AI Agent.</remarks>
        public string Content { get; private set; }

        /// <summary>
        /// Độ sâu chuỗi phản hồi tự động giữa các Agent, dùng để chặn đệ quy vô tận khi agent mention lẫn nhau (MaxDepth = 3).
        /// </summary>
        /// <remarks>
        /// <para>🔴 <strong>Cảnh báo vòng lặp:</strong> Khi CallDepth &gt;= 3, hệ thống bắt buộc dừng kích hoạt chuỗi mention tự động.</para>
        /// </remarks>
        public int CallDepth { get; private set; }

        /// <summary>
        /// Mã định danh tin nhắn cha mà tin nhắn này trực tiếp phản hồi (nếu có).
        /// </summary>
        /// <remarks>Hỗ trợ phân cấp cây phản hồi (Reply Thread) trong kênh.</remarks>
        public Guid? ParentMessageId { get; private set; }

        // ── Mentions & Navigation ──

        /// <summary>
        /// Danh sách các lượt nhắc tên thành viên (@mention) được gắn kèm trong tin nhắn này.
        /// </summary>
        /// <remarks>⇒ Quan hệ 1-N với <see cref="MessageMention"/>, tự động xóa liên đới (Cascade) khi tin nhắn bị xóa.</remarks>
        public virtual ICollection<MessageMention> Mentions { get; private set; }

        // ── Domain Methods ──

        /// <summary>
        /// Thêm một lượt nhắc tên thành viên vào tin nhắn.
        /// </summary>
        /// <param name="mentionedMemberId">Mã định danh thành viên được nhắc tới.</param>
        /// <returns>Thực thể <see cref="MessageMention"/> mới được tạo.</returns>
        public MessageMention AddMention(Guid mentionedMemberId)
        {
            var mention = new MessageMention(Id, mentionedMemberId);
            Mentions.Add(mention);
            return mention;
        }

        /// <summary>
        /// Thêm đối tượng lượt nhắc tên thành viên đã tạo vào tập hợp lượt nhắc.
        /// </summary>
        /// <param name="mention">Thực thể lượt nhắc tên cần thêm.</param>
        public void AddMention(MessageMention mention)
        {
            ArgumentNullException.ThrowIfNull(mention);
            Mentions.Add(mention);
        }
    }
}
