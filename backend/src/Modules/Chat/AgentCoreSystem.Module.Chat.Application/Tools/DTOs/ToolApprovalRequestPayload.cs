// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace AgentCoreSystem.Module.Chat.Application.Tools.DTOs
{
    /// <summary>
    /// Payload phát sóng qua SignalR thông báo có yêu cầu gọi công cụ cần người dùng phê duyệt (Action Card).
    /// </summary>
    /// <param name="PendingToolCallId">Mã định danh yêu cầu chờ phê duyệt (UUIDv7).</param>
    /// <param name="ChannelId">Mã định danh kênh trò chuyện.</param>
    /// <param name="ChatMessageId">Mã định danh tin nhắn cha kích hoạt yêu cầu.</param>
    /// <param name="ToolCallId">Mã định danh cuộc gọi của LLM (call_xxx).</param>
    /// <param name="ToolName">Tên công cụ cần thực thi.</param>
    /// <param name="ArgumentsJson">Chuỗi JSON các tham số đầu vào do LLM sinh ra.</param>
    /// <param name="ExpiresAt">Thời điểm hết hạn phê duyệt (mặc định UtcNow + 5 phút).</param>
    public sealed record ToolApprovalRequestPayload(
        Guid PendingToolCallId,
        Guid ChannelId,
        Guid ChatMessageId,
        string ToolCallId,
        string ToolName,
        string ArgumentsJson,
        DateTimeOffset ExpiresAt);
}
