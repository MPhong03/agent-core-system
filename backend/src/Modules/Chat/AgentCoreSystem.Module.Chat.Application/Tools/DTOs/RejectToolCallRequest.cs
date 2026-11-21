// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Module.Chat.Application.Tools.DTOs
{
    /// <summary>
    /// Dữ liệu đầu vào của API từ chối thực thi công cụ.
    /// </summary>
    /// <param name="Reason">Lý do từ chối (tùy chọn) để gửi cho AI Agent điều chỉnh phản hồi.</param>
    public sealed record RejectToolCallRequest(string? Reason = null);
}
