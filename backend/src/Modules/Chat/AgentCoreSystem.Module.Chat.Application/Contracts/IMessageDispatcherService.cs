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
    /// <para><strong>Why:</strong> Đảm bảo trải nghiệm trò chuyện mượt mà theo thời gian thực và ngăn chặn các vòng lặp phản hồi vô tận (CallDepth &gt;= 3).</para>
    /// <para>🔴 <strong>Lưu ý:</strong> Chạy nền (Background Processing) để giải phóng thread SignalR Client ngay lập tức.</para>
    /// </remarks>
    public interface IMessageDispatcherService
    {
        /// <summary>
        /// Điều phối tin nhắn tới các Agent được nhắc tên trong kênh làm việc.
        /// </summary>
        /// <param name="userMessage">Tin nhắn kích hoạt việc điều phối.</param>
        /// <param name="mentionedAgentIds">Danh sách mã định danh thành viên hoặc hồ sơ Agent được gắn thẻ nhắc tên.</param>
        /// <param name="ct">Mã hủy bỏ tác vụ.</param>
        /// <returns>Tác vụ bất đồng bộ hoàn tất khi toàn bộ Agent đã được xử lý xong.</returns>
        Task DispatchMessageAsync(
            ChatMessageDto userMessage,
            List<Guid> mentionedAgentIds,
            CancellationToken ct = default);

        /// <summary>
        /// Tiếp tục chu trình hội thoại với LLM sau khi yêu cầu gọi công cụ được người dùng phê duyệt hoặc từ chối.
        /// </summary>
        /// <remarks>
        /// <para><strong>What:</strong> Tải lại ngữ cảnh hội thoại, nạp kết quả thực thi công cụ hoặc thông điệp từ chối vào lịch sử chat và gửi tới mô hình AI để hoàn tất câu trả lời.</para>
        /// <para><strong>Why:</strong> Hiện thực hóa cơ chế Human-in-the-loop đối với các công cụ cần phê duyệt (NeedApprove/SemiAuto).</para>
        /// </remarks>
        /// <param name="pendingToolCallId">Mã định danh yêu cầu gọi công cụ đang chờ duyệt.</param>
        /// <param name="ct">Mã hủy bỏ tác vụ.</param>
        /// <returns>Tác vụ bất đồng bộ hoàn tất việc tiếp tục hội thoại.</returns>
        Task ResumeAfterToolCallAsync(
            Guid pendingToolCallId,
            CancellationToken ct = default);
    }
}
