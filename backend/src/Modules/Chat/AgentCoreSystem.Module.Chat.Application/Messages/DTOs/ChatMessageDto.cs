// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace AgentCoreSystem.Module.Chat.Application.Messages.DTOs
{
    /// <summary>
    /// Đối tượng truyền tải dữ liệu tin nhắn trò chuyện trong kênh làm việc.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Chứa thông tin đầy đủ của một tin nhắn kênh, bao gồm định danh người gửi, nội dung, danh sách tag @mention và độ sâu cuộc gọi.</para>
    /// <para><strong>When:</strong> Được trả về sau khi lưu tin nhắn hoặc khi truy vấn lịch sử hội thoại kênh.</para>
    /// </remarks>
    /// <param name="Id">Mã định danh tin nhắn (UUIDv7).</param>
    /// <param name="ChannelId">Mã định danh kênh trò chuyện.</param>
    /// <param name="SenderMemberId">Mã định danh thành viên gửi tin nhắn.</param>
    /// <param name="SenderDisplayName">Tên hiển thị của người gửi tại thời điểm gửi tin.</param>
    /// <param name="IsAgent">Xác định tin nhắn được gửi bởi Agent hay Người dùng.</param>
    /// <param name="Content">Nội dung văn bản của tin nhắn.</param>
    /// <param name="CallDepth">Độ sâu cuộc gọi phản hồi tự động giữa các agent.</param>
    /// <param name="ParentMessageId">Mã tin nhắn cha được phản hồi (nếu có).</param>
    /// <param name="Mentions">Danh sách mã định danh các thành viên được gắn thẻ nhắc tên (@mention).</param>
    /// <param name="CreatedDate">Thời điểm tin nhắn được gửi (UTC).</param>
    public record ChatMessageDto(
        Guid Id,
        Guid ChannelId,
        Guid SenderMemberId,
        string SenderDisplayName,
        bool IsAgent,
        string Content,
        int CallDepth,
        Guid? ParentMessageId,
        IReadOnlyList<Guid> Mentions,
        DateTimeOffset CreatedDate);
}
