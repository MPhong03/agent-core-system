// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Shared.Infrastructure.Http
{
    /// <summary>
    /// Yêu cầu thực thi REST API cho Agent Tool Calling.
    /// </summary>
    public sealed class RestApiExecutionRequest
    {
        /// <summary>
        /// Đường dẫn Endpoint URL đích (có thể chứa URL template dạng {param_name}).
        /// </summary>
        public required string Url { get; init; }

        /// <summary>
        /// Phương thức HTTP (GET, POST, PUT, DELETE, PATCH...). Mặc định là GET.
        /// </summary>
        public string Method { get; init; } = "GET";

        /// <summary>
        /// Danh sách HTTP Headers tùy chỉnh (Authorization, X-Api-Key...).
        /// </summary>
        public IDictionary<string, string>? Headers { get; init; }

        /// <summary>
        /// Danh sách tham số đầu vào được sinh từ LLM Tool Call.
        /// </summary>
        public IDictionary<string, object?>? Arguments { get; init; }

        /// <summary>
        /// Nội dung JSON body tùy chọn nếu được chỉ định trực tiếp thay vì tự động serialize từ Arguments.
        /// </summary>
        public string? Body { get; init; }

        /// <summary>
        /// Thời gian chờ tối đa (giây) trước khi ngắt kết nối. Mặc định 30 giây.
        /// </summary>
        public int TimeoutSeconds { get; init; } = 30;
    }

    /// <summary>
    /// Kết quả thực thi REST API trả về cho Agent và hệ thống ghi log.
    /// </summary>
    public sealed class RestApiExecutionResult
    {
        /// <summary>
        /// Cờ xác định cuộc gọi thành công (HTTP 2xx và không gặp lỗi SSRF/mạng).
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Mã trạng thái HTTP trả về từ endpoint đích (0 nếu bị chặn trước khi gửi).
        /// </summary>
        public int StatusCode { get; init; }

        /// <summary>
        /// Chuỗi dữ liệu phản hồi (JSON hoặc Text) nhận từ endpoint đích.
        /// </summary>
        public string ResponsePayload { get; init; } = string.Empty;

        /// <summary>
        /// Thời gian thực thi tính theo mili-giây (ms).
        /// </summary>
        public long DurationMs { get; init; }

        /// <summary>
        /// Thông điệp lỗi chi tiết khi cuộc gọi thất bại.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Tạo kết quả thực thi thành công.
        /// </summary>
        public static RestApiExecutionResult Success(int statusCode, string payload, long durationMs) =>
            new()
            {
                IsSuccess = true,
                StatusCode = statusCode,
                ResponsePayload = payload,
                DurationMs = durationMs,
                ErrorMessage = null
            };

        /// <summary>
        /// Tạo kết quả thực thi thất bại.
        /// </summary>
        public static RestApiExecutionResult Failure(string errorMessage, int statusCode = 0, long durationMs = 0, string payload = "") =>
            new()
            {
                IsSuccess = false,
                StatusCode = statusCode,
                ResponsePayload = payload,
                DurationMs = durationMs,
                ErrorMessage = errorMessage
            };
    }
}
