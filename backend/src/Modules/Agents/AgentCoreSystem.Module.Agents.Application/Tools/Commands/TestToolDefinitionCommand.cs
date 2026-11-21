// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Common;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Infrastructure.Http;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Tools.Commands
{
    /// <summary>
    /// Lệnh chạy thử nghiệm kiểm tra tính hoạt động của công cụ REST API.
    /// </summary>
    /// <param name="ToolDefinitionId">Mã định danh công cụ cần test (nếu kiểm tra công cụ đã lưu).</param>
    /// <param name="EndpointUrl">Địa chỉ URL API (nếu kiểm tra ad-hoc trước khi lưu).</param>
    /// <param name="HttpMethod">Phương thức HTTP (GET, POST, PUT, DELETE, PATCH).</param>
    /// <param name="HeadersConfig">Chuỗi JSON cấu hình Headers bổ sung/ghi đè.</param>
    /// <param name="Arguments">Dictionary các tham số đầu vào giả lập.</param>
    /// <param name="ArgumentsJson">Chuỗi JSON các tham số đầu vào giả lập (tùy chọn thay cho Arguments).</param>
    /// <param name="TimeoutSeconds">Thời gian chờ tối đa khi test (mặc định theo tool hoặc 30s).</param>
    public record TestToolDefinitionCommand(
        Guid? ToolDefinitionId = null,
        string? EndpointUrl = null,
        string? HttpMethod = null,
        string? HeadersConfig = null,
        IDictionary<string, object?>? Arguments = null,
        string? ArgumentsJson = null,
        int? TimeoutSeconds = null) : ICommand<ToolTestResultDto>;

    /// <summary>
    /// Bộ xác thực dữ liệu đầu vào cho lệnh <see cref="TestToolDefinitionCommand"/>.
    /// </summary>
    public class TestToolDefinitionCommandValidator : AbstractValidator<TestToolDefinitionCommand>
    {
        public TestToolDefinitionCommandValidator()
        {
            When(x => !x.ToolDefinitionId.HasValue || x.ToolDefinitionId.Value == Guid.Empty, () =>
            {
                RuleFor(x => x.EndpointUrl)
                    .NotEmpty().WithMessage("Endpoint URL không được để trống khi không chỉ định ToolDefinitionId.")
                    .Must(BeAValidUrl).WithMessage("Endpoint URL không đúng định dạng URL hợp lệ.");

                RuleFor(x => x.HttpMethod)
                    .NotEmpty().WithMessage("Phương thức HTTP không được để trống khi không chỉ định ToolDefinitionId.");
            });

            RuleFor(x => x.ArgumentsJson)
                .Must(BeValidJson).When(x => !string.IsNullOrWhiteSpace(x.ArgumentsJson))
                .WithMessage("ArgumentsJson phải là chuỗi JSON hợp lệ.");
        }

        private static bool BeAValidUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private static bool BeValidJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return true;
            try
            {
                using var doc = JsonDocument.Parse(json);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Handler xử lý chạy thử nghiệm công cụ qua <see cref="IRestApiExecutor"/>.
    /// </summary>
    public class TestToolDefinitionCommandHandler(
        IAgentsDbContext dbContext,
        IRestApiExecutor restApiExecutor)
        : ICommandHandler<TestToolDefinitionCommand, ToolTestResultDto>
    {
        /// <inheritdoc />
        public async Task<Result<ToolTestResultDto>> Handle(
            TestToolDefinitionCommand request,
            CancellationToken cancellationToken)
        {
            string endpointUrl;
            string httpMethod;
            string headersJson;
            int timeoutSeconds;

            if (request.ToolDefinitionId.HasValue && request.ToolDefinitionId.Value != Guid.Empty)
            {
                var tool = await dbContext.Tools
                    .FirstOrDefaultAsync(t => t.Id == request.ToolDefinitionId.Value, cancellationToken);

                if (tool == null)
                {
                    return Result.Failure<ToolTestResultDto>(Error.NotFound(
                        "ToolDefinition.NotFound",
                        $"Không tìm thấy định nghĩa công cụ với mã '{request.ToolDefinitionId.Value}'."));
                }

                endpointUrl = request.EndpointUrl ?? tool.EndpointUrl;
                httpMethod = request.HttpMethod ?? tool.HttpMethod;
                timeoutSeconds = request.TimeoutSeconds ?? tool.TimeoutSeconds;

                // Nếu có headersConfig mới gửi kèm, merge với secret gốc trong DB
                headersJson = !string.IsNullOrWhiteSpace(request.HeadersConfig)
                    ? SecretMaskingHelper.MergeHeadersJson(tool.HeadersConfig, request.HeadersConfig)
                    : tool.HeadersConfig;
            }
            else
            {
                endpointUrl = request.EndpointUrl ?? string.Empty;
                httpMethod = request.HttpMethod ?? "GET";
                timeoutSeconds = request.TimeoutSeconds ?? 30;
                headersJson = request.HeadersConfig ?? "{}";
            }

            // Phân giải headers sang dictionary thực tế mang secrets plaintext
            var headersDict = SecretMaskingHelper.ParseHeaders(headersJson);

            // Phân giải arguments
            IDictionary<string, object?>? argumentsDict = request.Arguments;
            if (argumentsDict == null && !string.IsNullOrWhiteSpace(request.ArgumentsJson))
            {
                try
                {
                    argumentsDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(request.ArgumentsJson);
                }
                catch (JsonException)
                {
                    return Result.Failure<ToolTestResultDto>(Error.Validation(
                        "ToolDefinition.InvalidArgumentsJson",
                        "ArgumentsJson không phải là chuỗi JSON hợp lệ."));
                }
            }

            var apiRequest = new RestApiExecutionRequest
            {
                Url = endpointUrl,
                Method = httpMethod,
                Headers = headersDict,
                Arguments = argumentsDict,
                TimeoutSeconds = timeoutSeconds
            };

            var execResult = await restApiExecutor.ExecuteAsync(apiRequest, cancellationToken);

            return Result.Success(ToolTestResultDto.FromExecutionResult(execResult));
        }
    }
}
