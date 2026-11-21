// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AgentCoreSystem.Module.Agents.Domain.Enums;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Agents.Domain.Entities
{
    /// <summary>
    /// Thực thể định nghĩa công cụ (Tool Definition) trong hệ thống Tool Builder.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Quản lý siêu dữ liệu, schema tham số, cấu hình HTTP request và quyền thực thi công cụ cho Agent.</para>
    /// <para><strong>Why:</strong> Cho phép tạo công cụ No-Code qua REST API và liên kết với Function Calling của LLM.</para>
    /// </remarks>
    public class ToolDefinition : BaseEntity
    {
        private static readonly Regex NameRegex = new("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
        private static readonly HashSet<string> AllowedHttpMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "GET", "POST", "PUT", "DELETE", "PATCH"
        };

        private readonly List<AgentToolAccess> _toolAccesses = [];
        private readonly List<ToolExecutionLog> _executionLogs = [];

        // ── Constructors ──

        /// <summary>
        /// Khởi tạo phục vụ Entity Framework Core deserialization.
        /// </summary>
        protected ToolDefinition() : base()
        {
            Name = string.Empty;
            DisplayName = string.Empty;
            Description = string.Empty;
            EndpointUrl = string.Empty;
            HttpMethod = string.Empty;
            HeadersConfig = "{}";
            ParametersSchema = "{}";
        }

        private ToolDefinition(
            string name,
            string displayName,
            string description,
            ToolType type,
            string endpointUrl,
            string httpMethod,
            string headersConfig,
            string parametersSchema,
            bool requiresConfirmation,
            int timeoutSeconds) : base()
        {
            Name = name;
            DisplayName = displayName;
            Description = description;
            Type = type;
            EndpointUrl = endpointUrl;
            HttpMethod = httpMethod.ToUpperInvariant();
            HeadersConfig = headersConfig;
            ParametersSchema = parametersSchema;
            RequiresConfirmation = requiresConfirmation;
            TimeoutSeconds = timeoutSeconds;
            IsActive = true;
        }

        // ── Properties ──

        /// <summary>
        /// Tên định danh kỹ thuật duy nhất của công cụ (vd: search_orders, weather_forecast).
        /// </summary>
        /// <remarks>🔴 <strong>Ràng buộc:</strong> Chỉ chứa chữ cái, chữ số, gạch dưới và gạch nối.</remarks>
        public string Name { get; private set; }

        /// <summary>
        /// Tên hiển thị thân thiện trên giao diện người dùng.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Mô tả chức năng và mục đích của công cụ (được đưa vào LLM tool description).
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Loại hình công cụ (hiện tại hỗ trợ RestApi).
        /// </summary>
        public ToolType Type { get; private set; }

        /// <summary>
        /// Địa chỉ URL endpoint của API cần gọi.
        /// </summary>
        public string EndpointUrl { get; private set; }

        /// <summary>
        /// Phương thức HTTP (GET, POST, PUT, DELETE, PATCH).
        /// </summary>
        public string HttpMethod { get; private set; }

        /// <summary>
        /// Cấu hình Headers dưới dạng JSON key-value map.
        /// </summary>
        public string HeadersConfig { get; private set; }

        /// <summary>
        /// Định nghĩa schema JSON của các tham số đầu vào cho LLM Function Calling.
        /// </summary>
        public string ParametersSchema { get; private set; }

        /// <summary>
        /// Cờ yêu cầu người dùng xác nhận trước khi thực thi công cụ.
        /// </summary>
        public bool RequiresConfirmation { get; private set; }

        /// <summary>
        /// Thời gian chờ tối đa khi gọi API (tính theo giây, mặc định 30).
        /// </summary>
        public int TimeoutSeconds { get; private set; }

        /// <summary>
        /// Trạng thái hoạt động của công cụ.
        /// </summary>
        public bool IsActive { get; private set; }

        // ── Navigation ──

        /// <summary>
        /// Danh sách quyền truy cập công cụ được cấp cho các Agent.
        /// </summary>
        public virtual IReadOnlyCollection<AgentToolAccess> ToolAccesses => _toolAccesses.AsReadOnly();

        /// <summary>
        /// Lịch sử nhật ký các lần thực thi công cụ.
        /// </summary>
        public virtual IReadOnlyCollection<ToolExecutionLog> ExecutionLogs => _executionLogs.AsReadOnly();

        // ── Factory & Domain Methods ──

        /// <summary>
        /// Tạo mới một định nghĩa công cụ với các ràng buộc kiểm tra hợp lệ.
        /// </summary>
        /// <param name="name">Tên định danh duy nhất của công cụ (^[a-zA-Z0-9_-]+$).</param>
        /// <param name="displayName">Tên hiển thị thân thiện.</param>
        /// <param name="description">Mô tả chức năng cho LLM.</param>
        /// <param name="type">Loại hình công cụ (RestApi).</param>
        /// <param name="endpointUrl">Địa chỉ URL API endpoint.</param>
        /// <param name="httpMethod">Phương thức HTTP (GET, POST, PUT, DELETE, PATCH).</param>
        /// <param name="headersConfig">Cấu hình Headers dạng JSON (tùy chọn).</param>
        /// <param name="parametersSchema">Schema tham số dạng JSON (tùy chọn).</param>
        /// <param name="requiresConfirmation">Cờ yêu cầu phê duyệt trước khi gọi.</param>
        /// <param name="timeoutSeconds">Thời gian chờ tối đa tính theo giây (mặc định 30, > 0).</param>
        /// <returns>Kết quả Result chứa thực thể ToolDefinition nếu hợp lệ.</returns>
        public static Result<ToolDefinition> Create(
            string name,
            string displayName,
            string description,
            ToolType type,
            string endpointUrl,
            string httpMethod,
            string? headersConfig = null,
            string? parametersSchema = null,
            bool requiresConfirmation = false,
            int timeoutSeconds = 30)
        {
            if (string.IsNullOrWhiteSpace(name) || !NameRegex.IsMatch(name))
            {
                return Result.Failure<ToolDefinition>(Error.Validation(
                    "ToolDefinition.InvalidName",
                    "Tên định danh công cụ (Name) không được để trống và chỉ được chứa chữ cái, số, dấu gạch dưới hoặc gạch nối."));
            }

            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                return Result.Failure<ToolDefinition>(Error.Validation(
                    "ToolDefinition.EndpointRequired",
                    "Endpoint URL không được để trống."));
            }

            if (string.IsNullOrWhiteSpace(httpMethod) || !AllowedHttpMethods.Contains(httpMethod))
            {
                return Result.Failure<ToolDefinition>(Error.Validation(
                    "ToolDefinition.InvalidHttpMethod",
                    $"Phương thức HTTP '{httpMethod}' không hợp lệ. Phải là một trong: GET, POST, PUT, DELETE, PATCH."));
            }

            if (timeoutSeconds <= 0)
            {
                return Result.Failure<ToolDefinition>(Error.Validation(
                    "ToolDefinition.InvalidTimeout",
                    "Thời gian chờ (TimeoutSeconds) phải là số nguyên dương (> 0)."));
            }

            var tool = new ToolDefinition(
                name.Trim(),
                displayName?.Trim() ?? string.Empty,
                description?.Trim() ?? string.Empty,
                type,
                endpointUrl.Trim(),
                httpMethod.Trim(),
                string.IsNullOrWhiteSpace(headersConfig) ? "{}" : headersConfig.Trim(),
                string.IsNullOrWhiteSpace(parametersSchema) ? "{}" : parametersSchema.Trim(),
                requiresConfirmation,
                timeoutSeconds);

            return Result.Success(tool);
        }

        /// <summary>
        /// Cập nhật thông tin chi tiết của công cụ.
        /// </summary>
        public Result Update(
            string displayName,
            string description,
            string endpointUrl,
            string httpMethod,
            string? headersConfig,
            string? parametersSchema,
            bool requiresConfirmation,
            int timeoutSeconds)
        {
            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                return Result.Failure(Error.Validation(
                    "ToolDefinition.EndpointRequired",
                    "Endpoint URL không được để trống."));
            }

            if (string.IsNullOrWhiteSpace(httpMethod) || !AllowedHttpMethods.Contains(httpMethod))
            {
                return Result.Failure(Error.Validation(
                    "ToolDefinition.InvalidHttpMethod",
                    $"Phương thức HTTP '{httpMethod}' không hợp lệ. Phải là một trong: GET, POST, PUT, DELETE, PATCH."));
            }

            if (timeoutSeconds <= 0)
            {
                return Result.Failure(Error.Validation(
                    "ToolDefinition.InvalidTimeout",
                    "Thời gian chờ (TimeoutSeconds) phải là số nguyên dương (> 0)."));
            }

            DisplayName = displayName?.Trim() ?? string.Empty;
            Description = description?.Trim() ?? string.Empty;
            EndpointUrl = endpointUrl.Trim();
            HttpMethod = httpMethod.Trim().ToUpperInvariant();
            HeadersConfig = string.IsNullOrWhiteSpace(headersConfig) ? "{}" : headersConfig.Trim();
            ParametersSchema = string.IsNullOrWhiteSpace(parametersSchema) ? "{}" : parametersSchema.Trim();
            RequiresConfirmation = requiresConfirmation;
            TimeoutSeconds = timeoutSeconds;

            return Result.Success();
        }

        /// <summary>
        /// Kích hoạt công cụ.
        /// </summary>
        public void Activate()
        {
            IsActive = true;
        }

        /// <summary>
        /// Hủy kích hoạt / tạm ngưng công cụ.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
