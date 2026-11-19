// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgentCoreSystem.Shared.Application.Behaviors
{
    /// <summary>
    /// Pipeline Behavior tự động kích hoạt FluentValidation trước khi Request đến Handler.
    /// </summary>
    /// <remarks>
    /// <strong>Why:</strong> Tập trung hóa logic xác thực dữ liệu đầu vào, không để dữ liệu sai lọt vào tầng nghiệp vụ.
    /// </remarks>
    public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(
            TRequest request, 
            RequestHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            if (!validators.Any())
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                var errorMessages = string.Join("; ", failures.Select(f => f.ErrorMessage));
                var error = Error.Validation("Request.ValidationError", errorMessages);

                // Nếu TResponse là Result hoặc Result<T>
                if (typeof(TResponse) == typeof(Result))
                {
                    return (TResponse)(object)Result.Failure(error);
                }

                if (typeof(TResponse).IsGenericType &&
                    typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var resultType = typeof(TResponse);
                    var failureMethod = typeof(Result)
                        .GetMethods()
                        .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod)
                        .MakeGenericMethod(resultType.GenericTypeArguments[0]);

                    return (TResponse)failureMethod.Invoke(null, [error])!;
                }

                throw new ValidationException(failures);
            }

            return await next();
        }
    }

    /// <summary>
    /// Pipeline Behavior ghi log theo dõi luồng thực thi của Request trong MediatR.
    /// </summary>
    public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(
            TRequest request, 
            RequestHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            logger.LogInformation("Đang bắt đầu xử lý Request: {RequestName}", requestName);

            var response = await next();

            logger.LogInformation("Đã hoàn tất xử lý Request: {RequestName}", requestName);
            return response;
        }
    }

    /// <summary>
    /// Pipeline Behavior đo lường và cảnh báo nếu Request xử lý quá ngưỡng thời gian quy định (500ms).
    /// </summary>
    public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly Stopwatch _timer = new();

        public async Task<TResponse> Handle(
            TRequest request, 
            RequestHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            _timer.Start();
            var response = await next();
            _timer.Stop();

            var elapsedMilliseconds = _timer.ElapsedMilliseconds;
            if (elapsedMilliseconds > 500)
            {
                var requestName = typeof(TRequest).Name;
                logger.LogWarning("Cảnh báo hiệu năng: Request {RequestName} thực thi mất {ElapsedMilliseconds} ms (vượt ngưỡng 500ms)",
                    requestName, elapsedMilliseconds);
            }

            return response;
        }
    }
}

