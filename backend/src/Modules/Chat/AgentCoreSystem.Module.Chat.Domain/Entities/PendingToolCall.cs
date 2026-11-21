// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Chat.Domain.Enums;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Chat.Domain.Entities
{
    /// <summary>
    /// Thực thể đại diện cho yêu cầu gọi công cụ đang chờ xác nhận (Pending Tool Call) trong kênh trò chuyện.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Lưu trữ trạng thái và tham số của lệnh gọi công cụ khi Agent chạy ở chế độ bán tự động (SemiAuto) hoặc cần phê duyệt (NeedApprove).</para>
    /// <para><strong>Why:</strong> Cho phép tương tác Human-in-the-loop, yêu cầu người dùng xác nhận hoặc từ chối trước khi hệ thống thực thi tác vụ.</para>
    /// </remarks>
    public class PendingToolCall : BaseEntity
    {
        // ── Constructors ──

        /// <summary>
        /// Khởi tạo phục vụ Entity Framework Core deserialization.
        /// </summary>
        protected PendingToolCall() : base()
        {
            ToolCallId = string.Empty;
            ToolName = string.Empty;
            ArgumentsJson = string.Empty;
        }

        /// <summary>
        /// Khởi tạo một yêu cầu gọi công cụ đang chờ phê duyệt.
        /// </summary>
        /// <param name="channelId">Mã định danh kênh trò chuyện.</param>
        /// <param name="chatMessageId">Mã định danh tin nhắn phát sinh yêu cầu gọi công cụ.</param>
        /// <param name="toolCallId">Mã ID định danh cuộc gọi của LLM (vd: call_abc123).</param>
        /// <param name="toolName">Tên định danh công cụ.</param>
        /// <param name="argumentsJson">Chuỗi JSON chứa danh sách tham số truyền vào công cụ.</param>
        /// <param name="expiresAt">Thời điểm hết hạn chờ phê duyệt.</param>
        /// <param name="id">Mã định danh (UUIDv7, tùy chọn).</param>
        public PendingToolCall(
            Guid channelId,
            Guid chatMessageId,
            string toolCallId,
            string toolName,
            string argumentsJson,
            DateTimeOffset expiresAt,
            Guid? id = null) : base(id ?? Guid.Empty)
        {
            ChannelId = channelId;
            ChatMessageId = chatMessageId;
            ToolCallId = toolCallId ?? string.Empty;
            ToolName = toolName ?? string.Empty;
            ArgumentsJson = argumentsJson ?? "{}";
            Status = PendingToolCallStatus.Pending;
            ExpiresAt = expiresAt;
        }

        // ── Properties ──

        /// <summary>
        /// Mã định danh kênh trò chuyện.
        /// </summary>
        public Guid ChannelId { get; private set; }

        /// <summary>
        /// Mã định danh tin nhắn trò chuyện chứa yêu cầu gọi công cụ.
        /// </summary>
        public Guid ChatMessageId { get; private set; }

        /// <summary>
        /// Mã định danh lời gọi công cụ do mô hình AI cung cấp (Tool Call ID).
        /// </summary>
        public string ToolCallId { get; private set; }

        /// <summary>
        /// Tên định danh của công cụ được gọi.
        /// </summary>
        public string ToolName { get; private set; }

        /// <summary>
        /// Tham số đầu vào dưới dạng chuỗi JSON.
        /// </summary>
        public string ArgumentsJson { get; private set; }

        /// <summary>
        /// Trạng thái phê duyệt của yêu cầu.
        /// </summary>
        public PendingToolCallStatus Status { get; private set; }

        /// <summary>
        /// Thời điểm hết hạn xác nhận.
        /// </summary>
        public DateTimeOffset ExpiresAt { get; private set; }

        // ── Domain Methods ──

        /// <summary>
        /// Xác nhận đồng ý thực thi yêu cầu gọi công cụ.
        /// </summary>
        public void Confirm()
        {
            Status = PendingToolCallStatus.Confirmed;
        }

        /// <summary>
        /// Từ chối thực thi yêu cầu gọi công cụ.
        /// </summary>
        public void Reject()
        {
            Status = PendingToolCallStatus.Rejected;
        }

        /// <summary>
        /// Đánh dấu yêu cầu đã quá hạn chờ phê duyệt.
        /// </summary>
        public void Expire()
        {
            Status = PendingToolCallStatus.Expired;
        }
    }
}
