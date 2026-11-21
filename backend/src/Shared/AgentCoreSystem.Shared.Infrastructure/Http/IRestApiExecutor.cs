// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Shared.Infrastructure.Http
{
    /// <summary>
    /// Giao diện dịch vụ thực thi REST API không trạng thái dành cho No-Code Tool Calling.
    /// </summary>
    public interface IRestApiExecutor
    {
        /// <summary>
        /// Thực thi yêu cầu gọi REST API với kiểm tra SSRF Guard, thay thế tham số template và timeout.
        /// </summary>
        /// <param name="request">Yêu cầu thực thi API bao gồm Endpoint, Method, Headers, Arguments.</param>
        /// <param name="ct">CancellationToken hủy tác vụ.</param>
        /// <returns>Đối tượng kết quả thực thi <see cref="RestApiExecutionResult"/>.</returns>
        Task<RestApiExecutionResult> ExecuteAsync(RestApiExecutionRequest request, CancellationToken ct = default);

        /// <summary>
        /// Xác thực URL mục tiêu nhằm đảm bảo an toàn chống tấn công SSRF.
        /// Ném <see cref="InvalidOperationException"/> nếu URL trỏ đến dải địa chỉ nội bộ, loopback hoặc private network.
        /// </summary>
        /// <param name="url">URL cần kiểm tra.</param>
        /// <param name="ct">CancellationToken hủy tác vụ.</param>
        Task EnsureSafeUrlAsync(string url, CancellationToken ct = default);
    }
}
