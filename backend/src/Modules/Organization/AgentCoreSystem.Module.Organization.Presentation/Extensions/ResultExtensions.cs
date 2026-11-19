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
    public static class ResultExtensions
    {
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
