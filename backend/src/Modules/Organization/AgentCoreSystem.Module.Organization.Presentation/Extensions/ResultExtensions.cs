// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.Organization.Presentation.Extensions
{
    /// <summary>
    /// Các phương thức mở rộng hỗ trợ ánh xạ tập trung từ Result/Error sang RFC 7807 ProblemDetails.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Tiện ích chuyển đổi đối tượng <see cref="Result"/> thất bại thành phản hồi <see cref="ActionResult"/> chuẩn ProblemDetails.</para>
    /// <para><strong>When:</strong> Được gọi tại các API Controller khi kết quả CQRS trả về lỗi nghiệp vụ.</para>
    /// <para><strong>Why:</strong> Chuẩn hóa định dạng lỗi toàn hệ thống tuân thủ RFC 7807 và nhất quán với Angular Client.</para>
    /// </remarks>
    public static class ResultExtensions
    {
        // ── ProblemDetails Mapping ──

        /// <summary>
        /// Ánh xạ một đối tượng <see cref="Result"/> thất bại thành phản hồi <see cref="ActionResult"/> theo chuẩn RFC 7807 ProblemDetails.
        /// </summary>
        /// <param name="result">Kết quả nghiệp vụ mang thông tin lỗi.</param>
        /// <param name="controller">ControllerBase phục vụ tạo Problem response.</param>
        /// <returns>Đối tượng <see cref="ActionResult"/> chứa ProblemDetails.</returns>
        /// <exception cref="InvalidOperationException">Ném ra nếu Result là trạng thái thành công.</exception>
        public static ActionResult ToProblemDetails(this Result result, ControllerBase controller)
        {
            if (result.IsSuccess)
            {
                throw new InvalidOperationException("Không thể tạo ProblemDetails từ một Result thành công.");
            }

            var statusCode = GetStatusCode(result.Error.Type);
            var title = GetTitle(result.Error.Type);
            var typeUri = GetTypeUri(result.Error.Type);

            return controller.Problem(
                statusCode: statusCode,
                title: title,
                type: typeUri,
                detail: result.Error.Description,
                extensions: new Dictionary<string, object?>
                {
                    { "errorCode", result.Error.Code },
                    { "timestamp", DateTimeOffset.UtcNow }
                });
        }

        // ── Helper Mappings ──

        /// <summary>
        /// Lấy mã trạng thái HTTP tương ứng với loại lỗi nghiệp vụ.
        /// </summary>
        /// <param name="errorType">Loại lỗi phân loại trong Domain/Kernel.</param>
        /// <returns>Mã trạng thái HTTP tương ứng.</returns>
        public static int GetStatusCode(ErrorType errorType) => errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };

        /// <summary>
        /// Lấy tiêu đề mô tả ngắn cho mã lỗi RFC 7807.
        /// </summary>
        /// <param name="errorType">Loại lỗi phân loại.</param>
        /// <returns>Tiêu đề lỗi chuẩn hóa tiếng Anh.</returns>
        private static string GetTitle(ErrorType errorType) => errorType switch
        {
            ErrorType.Validation => "Bad Request - Validation Error",
            ErrorType.NotFound => "Not Found",
            ErrorType.Conflict => "Conflict",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden => "Forbidden",
            ErrorType.Unexpected => "Internal Server Error",
            _ => "Bad Request"
        };

        /// <summary>
        /// Lấy URI đặc tả kỹ thuật của chuẩn RFC tương ứng với mã lỗi.
        /// </summary>
        /// <param name="errorType">Loại lỗi phân loại.</param>
        /// <returns>URI đặc tả kỹ thuật RFC.</returns>
        private static string GetTypeUri(ErrorType errorType) => errorType switch
        {
            ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
            ErrorType.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }
}
