// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Shared.Infrastructure.Http;

namespace AgentCoreSystem.Module.Agents.Application.Tools.DTOs
{
    /// <summary>
    /// DTO kết quả chạy thử nghiệm một công cụ API (Tool Test Result).
    /// </summary>
    /// <param name="IsSuccess">Cờ xác định yêu cầu HTTP thành công (mã 2xx và không gặp lỗi SSRF/mạng).</param>
    /// <param name="StatusCode">Mã trạng thái HTTP trả về từ endpoint đích.</param>
    /// <param name="ResponsePayload">Nội dung phản hồi (JSON hoặc Text) nhận được.</param>
    /// <param name="DurationMs">Thời gian thực thi tính theo mili-giây.</param>
    /// <param name="ErrorMessage">Thông điệp lỗi chi tiết nếu yêu cầu thất bại.</param>
    public sealed record ToolTestResultDto(
        bool IsSuccess,
        int StatusCode,
        string ResponsePayload,
        long DurationMs,
        string? ErrorMessage)
    {
        /// <summary>
        /// Chuyển đổi từ <see cref="RestApiExecutionResult"/> sang DTO <see cref="ToolTestResultDto"/>.
        /// </summary>
        public static ToolTestResultDto FromExecutionResult(RestApiExecutionResult result)
        {
            return new ToolTestResultDto(
                result.IsSuccess,
                result.StatusCode,
                result.ResponsePayload,
                result.DurationMs,
                result.ErrorMessage);
        }
    }
}
