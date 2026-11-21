// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace AgentCoreSystem.Module.Agents.Presentation.Contracts
{
    /// <summary>
    /// Dữ liệu yêu cầu chạy thử nghiệm công cụ ad-hoc (trước khi lưu vào cơ sở dữ liệu).
    /// </summary>
    /// <param name="EndpointUrl">Địa chỉ URL API endpoint cần gọi kiểm tra.</param>
    /// <param name="HttpMethod">Phương thức HTTP (GET, POST, PUT, DELETE, PATCH).</param>
    /// <param name="HeadersConfig">Chuỗi JSON cấu hình Headers (Authorization, API Keys...).</param>
    /// <param name="Arguments">Dictionary các giá trị tham số đầu vào giả lập.</param>
    /// <param name="ArgumentsJson">Chuỗi JSON các giá trị tham số đầu vào giả lập (tùy chọn thay cho Arguments).</param>
    /// <param name="TimeoutSeconds">Thời gian chờ tối đa khi chạy test (mặc định 30 giây).</param>
    public sealed record TestAdHocToolRequest(
        string EndpointUrl,
        string HttpMethod = "GET",
        string? HeadersConfig = null,
        IDictionary<string, object?>? Arguments = null,
        string? ArgumentsJson = null,
        int? TimeoutSeconds = null);
}
