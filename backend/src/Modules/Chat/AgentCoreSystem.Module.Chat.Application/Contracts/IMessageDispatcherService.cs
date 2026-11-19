// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Chat.Application.Messages.DTOs;

namespace AgentCoreSystem.Module.Chat.Application.Contracts
{
    /// <summary>
    /// Dịch vụ điều phối và định tuyến tin nhắn gắn thẻ (@mention) tới các nhân sự AI (Agents).
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Tiếp nhận tin nhắn người dùng hoặc tin nhắn giữa các agent, kiểm tra độ sâu cuộc gọi để chống lặp đệ quy, xếp hàng xử lý tuần tự (Sequential Queue), tạo context sliding window 20 tin nhắn gần nhất, và stream phản hồi qua SignalR.</para>
    /// <para><strong>When:</strong> Được kích hoạt khi có tin nhắn mới chứa danh sách nhắc tên @mention.</para>
    /// <para><strong>Why:</strong> Đảm bảo trải nghiệm trò chuyện mượt mà theo thời gian thực và ngăn chặn các vòng lặp phản hồi vô tận (CallDepth >= 3).</para>
    /// </remarks>
    public interface IMessageDispatcherService
    {
        /// <summary>
        /// Điều phối tin nhắn tới các Agent được nhắc tên trong kênh làm việc.
        /// </summary>
        /// <param name="userMessage">Tin nhắn kích hoạt việc điều phối.</param>
        /// <param name="mentionedAgentIds">Danh sách mã định danh thành viên hoặc hồ sơ Agent được gắn thẻ nhắc tên.</param>
        /// <param name="ct">Mã hủy bỏ tác vụ.</param>
        Task DispatchMessageAsync(
            ChatMessageDto userMessage,
            List<Guid> mentionedAgentIds,
            CancellationToken ct = default);
    }
}
