// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Module.Chat.Domain.Enums
{
    /// <summary>
    /// Trạng thái yêu cầu phê duyệt gọi công cụ (Pending Tool Call) trong kênh trò chuyện.
    /// </summary>
    public enum PendingToolCallStatus
    {
        /// <summary>
        /// Đang chờ người dùng phê duyệt hoặc từ chối.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Người dùng đã xác nhận đồng ý thực thi công cụ.
        /// </summary>
        Confirmed = 1,

        /// <summary>
        /// Người dùng đã từ chối thực thi công cụ.
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Yêu cầu đã quá hạn thời gian chờ phê duyệt.
        /// </summary>
        Expired = 3
    }
}
