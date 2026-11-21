// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace AgentCoreSystem.Module.Agents.Presentation.Contracts
{
    /// <summary>
    /// Dữ liệu yêu cầu chạy thử nghiệm công cụ đã lưu trong hệ thống.
    /// </summary>
    /// <param name="HeadersConfig">Chuỗi JSON cấu hình Headers bổ sung hoặc ghi đè (tùy chọn).</param>
    /// <param name="Arguments">Dictionary các giá trị tham số đầu vào giả lập.</param>
    /// <param name="ArgumentsJson">Chuỗi JSON các giá trị tham số đầu vào giả lập (tùy chọn thay cho Arguments).</param>
    /// <param name="TimeoutSeconds">Thời gian chờ tối đa khi chạy test (mặc định lấy theo công cụ hoặc 30 giây).</param>
    public sealed record TestToolRequest(
        string? HeadersConfig = null,
        IDictionary<string, object?>? Arguments = null,
        string? ArgumentsJson = null,
        int? TimeoutSeconds = null);
}
