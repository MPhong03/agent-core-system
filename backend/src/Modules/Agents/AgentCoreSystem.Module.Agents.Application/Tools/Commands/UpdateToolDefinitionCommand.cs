// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Common;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Tools.Commands
{
    /// <summary>
    /// Lệnh cập nhật cấu hình thông tin định nghĩa công cụ.
    /// </summary>
    public record UpdateToolDefinitionCommand(
        Guid Id,
        string DisplayName,
        string Description,
        string EndpointUrl,
        string HttpMethod,
        string? HeadersConfig = null,
        string? ParametersSchema = null,
        bool RequiresConfirmation = false,
        int TimeoutSeconds = 30) : ICommand<ToolDefinitionDto>;

    /// <summary>
    /// Bộ xác thực dữ liệu đầu vào cho lệnh <see cref="UpdateToolDefinitionCommand"/>.
    /// </summary>
    public class UpdateToolDefinitionCommandValidator : AbstractValidator<UpdateToolDefinitionCommand>
    {
        public UpdateToolDefinitionCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Mã định danh công cụ không được để trống.");

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Tên hiển thị không được để trống.")
                .MaximumLength(200).WithMessage("Tên hiển thị tối đa 200 ký tự.");

            RuleFor(x => x.EndpointUrl)
                .NotEmpty().WithMessage("Endpoint URL không được để trống.")
                .Must(BeAValidUrl).WithMessage("Endpoint URL không đúng định dạng URL hợp lệ.");

            RuleFor(x => x.HttpMethod)
                .NotEmpty().WithMessage("Phương thức HTTP không được để trống.")
                .Must(m => m != null && (m.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
                                         m.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                                         m.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                                         m.Equals("DELETE", StringComparison.OrdinalIgnoreCase) ||
                                         m.Equals("PATCH", StringComparison.OrdinalIgnoreCase)))
                .WithMessage("Phương thức HTTP phải là một trong các giá trị: GET, POST, PUT, DELETE, PATCH.");

            RuleFor(x => x.TimeoutSeconds)
                .InclusiveBetween(1, 300).WithMessage("Thời gian chờ (TimeoutSeconds) phải nằm trong khoảng từ 1 đến 300 giây.");

            RuleFor(x => x.ParametersSchema)
                .Must(BeValidJson).When(x => !string.IsNullOrWhiteSpace(x.ParametersSchema))
                .WithMessage("ParametersSchema phải là chuỗi JSON hợp lệ.");

            RuleFor(x => x.HeadersConfig)
                .Must(BeValidJson).When(x => !string.IsNullOrWhiteSpace(x.HeadersConfig))
                .WithMessage("HeadersConfig phải là chuỗi JSON hợp lệ.");
        }

        private static bool BeAValidUrl(string url)
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
    /// Handler xử lý cập nhật định nghĩa công cụ và bảo toàn Secrets trong Headers.
    /// </summary>
    public class UpdateToolDefinitionCommandHandler(IAgentsDbContext dbContext)
        : ICommandHandler<UpdateToolDefinitionCommand, ToolDefinitionDto>
    {
        /// <inheritdoc />
        public async Task<Result<ToolDefinitionDto>> Handle(
            UpdateToolDefinitionCommand request,
            CancellationToken cancellationToken)
        {
            var tool = await dbContext.Tools
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (tool == null)
            {
                return Result.Failure<ToolDefinitionDto>(Error.NotFound(
                    "ToolDefinition.NotFound",
                    $"Không tìm thấy định nghĩa công cụ với mã '{request.Id}'."));
            }

            // Hợp nhất Headers nhằm bảo toàn giá trị Secret ban đầu nếu client gửi lên chuỗi đã bị che giấu '••••••••'
            var mergedHeaders = SecretMaskingHelper.MergeHeadersJson(tool.HeadersConfig, request.HeadersConfig);

            var updateResult = tool.Update(
                displayName: request.DisplayName,
                description: request.Description,
                endpointUrl: request.EndpointUrl,
                httpMethod: request.HttpMethod,
                headersConfig: mergedHeaders,
                parametersSchema: request.ParametersSchema,
                requiresConfirmation: request.RequiresConfirmation,
                timeoutSeconds: request.TimeoutSeconds);

            if (updateResult.IsFailure)
            {
                return Result.Failure<ToolDefinitionDto>(updateResult.Error);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(ToolDefinitionDto.FromEntity(tool, maskSecrets: true));
        }
    }
}
