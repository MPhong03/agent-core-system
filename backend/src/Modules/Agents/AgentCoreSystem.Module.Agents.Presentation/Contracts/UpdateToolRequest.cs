// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Module.Agents.Presentation.Contracts
{
    /// <summary>
    /// Dữ liệu yêu cầu cập nhật thông tin định nghĩa công cụ (Tool Definition).
    /// </summary>
    /// <param name="DisplayName">Tên hiển thị thân thiện trên UI người dùng.</param>
    /// <param name="Description">Mô tả chức năng chi tiết cho mô hình ngôn ngữ lớn (LLM).</param>
    /// <param name="EndpointUrl">Địa chỉ URL API endpoint đích cần gọi.</param>
    /// <param name="HttpMethod">Phương thức HTTP (GET, POST, PUT, DELETE, PATCH).</param>
    /// <param name="HeadersConfig">Chuỗi JSON cấu hình Headers (Authorization, API Keys...). Giá trị mang '••••••••' sẽ được bảo toàn Secret gốc.</param>
    /// <param name="ParametersSchema">JSON Schema định nghĩa cấu trúc tham số đầu vào.</param>
    /// <param name="RequiresConfirmation">Cờ yêu cầu phê duyệt của người dùng trước khi gọi công cụ.</param>
    /// <param name="TimeoutSeconds">Thời gian chờ tối đa khi gọi công cụ (1 đến 300 giây, mặc định 30).</param>
    public sealed record UpdateToolRequest(
        string DisplayName,
        string Description,
        string EndpointUrl,
        string HttpMethod,
        string? HeadersConfig = null,
        string? ParametersSchema = null,
        bool RequiresConfirmation = false,
        int TimeoutSeconds = 30);
}
