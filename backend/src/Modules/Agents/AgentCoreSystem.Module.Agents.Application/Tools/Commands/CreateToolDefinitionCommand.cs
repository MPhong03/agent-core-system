// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using AgentCoreSystem.Module.Agents.Domain.Enums;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Tools.Commands
{
    /// <summary>
    /// Lệnh tạo mới một định nghĩa công cụ (Tool Definition).
    /// </summary>
    public record CreateToolDefinitionCommand(
        string Name,
        string DisplayName,
        string Description,
        ToolType Type,
        string EndpointUrl,
        string HttpMethod,
        string? HeadersConfig = null,
        string? ParametersSchema = null,
        bool RequiresConfirmation = false,
        int TimeoutSeconds = 30) : ICommand<ToolDefinitionDto>;

    /// <summary>
    /// Bộ xác thực dữ liệu đầu vào cho lệnh <see cref="CreateToolDefinitionCommand"/>.
    /// </summary>
    public class CreateToolDefinitionCommandValidator : AbstractValidator<CreateToolDefinitionCommand>
    {
        public CreateToolDefinitionCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên định danh công cụ (Name) không được để trống.")
                .MaximumLength(100).WithMessage("Tên công cụ tối đa 100 ký tự.")
                .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Tên công cụ chỉ được chứa chữ cái, số, gạch dưới và gạch nối.");

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
    /// Handler xử lý tạo mới định nghĩa công cụ trong phân hệ Agents.
    /// </summary>
    public class CreateToolDefinitionCommandHandler(IAgentsDbContext dbContext)
        : ICommandHandler<CreateToolDefinitionCommand, ToolDefinitionDto>
    {
        /// <inheritdoc />
        public async Task<Result<ToolDefinitionDto>> Handle(
            CreateToolDefinitionCommand request,
            CancellationToken cancellationToken)
        {
            // Kiểm tra tính hợp lệ của ParametersSchema JSON nếu có
            if (!string.IsNullOrWhiteSpace(request.ParametersSchema))
            {
                try
                {
                    using var doc = JsonDocument.Parse(request.ParametersSchema);
                }
                catch (JsonException)
                {
                    return Result.Failure<ToolDefinitionDto>(Error.Validation(
                        "ToolDefinition.InvalidSchema",
                        "ParametersSchema không phải là định dạng JSON hợp lệ."));
                }
            }

            // Kiểm tra tính hợp lệ của HeadersConfig JSON nếu có
            if (!string.IsNullOrWhiteSpace(request.HeadersConfig))
            {
                try
                {
                    using var doc = JsonDocument.Parse(request.HeadersConfig);
                }
                catch (JsonException)
                {
                    return Result.Failure<ToolDefinitionDto>(Error.Validation(
                        "ToolDefinition.InvalidHeadersJson",
                        "HeadersConfig không phải là định dạng JSON hợp lệ."));
                }
            }

            // Kiểm tra trùng lặp tên công cụ
            var exists = await dbContext.Tools
                .AnyAsync(t => t.Name == request.Name.Trim(), cancellationToken);

            if (exists)
            {
                return Result.Failure<ToolDefinitionDto>(Error.Conflict(
                    "ToolDefinition.DuplicateName",
                    $"Công cụ mang tên '{request.Name}' đã tồn tại trong hệ thống."));
            }

            var toolResult = ToolDefinition.Create(
                name: request.Name,
                displayName: request.DisplayName,
                description: request.Description,
                type: request.Type,
                endpointUrl: request.EndpointUrl,
                httpMethod: request.HttpMethod,
                headersConfig: request.HeadersConfig,
                parametersSchema: request.ParametersSchema,
                requiresConfirmation: request.RequiresConfirmation,
                timeoutSeconds: request.TimeoutSeconds);

            if (toolResult.IsFailure)
            {
                return Result.Failure<ToolDefinitionDto>(toolResult.Error);
            }

            var tool = toolResult.Value;
            dbContext.Tools.Add(tool);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(ToolDefinitionDto.FromEntity(tool, maskSecrets: true));
        }
    }
}
