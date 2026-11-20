// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AgentCoreSystem.Module.Chat.Application.Messages.DTOs;
using AgentCoreSystem.Shared.Application.Messaging;

namespace AgentCoreSystem.Module.Chat.Application.Messages.Queries
{
    /// <summary>
    /// Truy vấn danh sách tin nhắn trong một kênh làm việc với phân trang theo mốc thời gian.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Lấy danh sách tin nhắn thuộc về <see cref="ChannelId"/>, sắp xếp theo thời gian tăng dần để hiển thị hội thoại.</para>
    /// <para><strong>Why:</strong> Phục vụ màn hình Chat thời gian thực và cung cấp Context Sliding Window cho Agent.</para>
    /// </remarks>
    /// <param name="ChannelId">Mã định danh kênh trò chuyện cần lấy tin nhắn.</param>
    /// <param name="Limit">Số lượng tin nhắn tối đa cần lấy (mặc định 50).</param>
    /// <param name="BeforeMessageId">Mã tin nhắn mốc để phân trang lùi (nếu có, chỉ lấy các tin nhắn tạo trước tin nhắn này).</param>
    public record GetChannelMessagesQuery(
        Guid ChannelId,
        int Limit = 50,
        Guid? BeforeMessageId = null) : IQuery<IReadOnlyList<ChatMessageDto>>;
}
