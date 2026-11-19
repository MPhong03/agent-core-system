// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AgentCoreSystem.Api.Middleware
{
    /// <summary>
    /// Xử lý ngoại lệ toàn cục cho ứng dụng theo chuẩn .NET 9 IExceptionHandler và RFC 7807.
    /// </summary>
    /// <remarks>
    /// <strong>Why:</strong> Đảm bảo mọi lỗi hệ thống bất thường đều được ghi log chi tiết kèm TraceId và trả về client định dạng ProblemDetails an toàn.
    /// </remarks>
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, 
            Exception exception, 
            CancellationToken cancellationToken)
        {
            var traceId = httpContext.TraceIdentifier;
            logger.LogError(exception, "Unhandled Exception xảy ra trên request {Path} [TraceId: {TraceId}]", 
                httpContext.Request.Path, traceId);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Lỗi Máy Chủ Nội Bộ",
                Detail = "Đã xảy ra sự cố ngoài ý muốn trong quá trình xử lý yêu cầu. Vui lòng liên hệ quản trị viên.",
                Instance = httpContext.Request.Path
            };

            problemDetails.Extensions["traceId"] = traceId;
            problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}

