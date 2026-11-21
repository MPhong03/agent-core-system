// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Agents.Application.Common;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using AgentCoreSystem.Module.Agents.Domain.Enums;

namespace AgentCoreSystem.Module.Agents.Application.Tools.DTOs
{
    /// <summary>
    /// DTO đối tượng định nghĩa công cụ (Tool Definition) trả về cho Client hoặc các Service liên module.
    /// </summary>
    /// <param name="Id">Mã định danh duy nhất của công cụ (UUIDv7).</param>
    /// <param name="Name">Tên định danh kỹ thuật duy nhất của công cụ.</param>
    /// <param name="DisplayName">Tên hiển thị thân thiện trên giao diện người dùng.</param>
    /// <param name="Description">Mô tả chức năng cho LLM Function Calling.</param>
    /// <param name="Type">Loại hình công cụ (RestApi).</param>
    /// <param name="EndpointUrl">Địa chỉ URL API endpoint.</param>
    /// <param name="HttpMethod">Phương thức HTTP (GET, POST, PUT, DELETE, PATCH).</param>
    /// <param name="HeadersConfig">Cấu hình Headers dạng JSON (nhạy cảm đã được che giấu dạng '••••••••').</param>
    /// <param name="ParametersSchema">JSON Schema của các tham số đầu vào.</param>
    /// <param name="RequiresConfirmation">Cờ yêu cầu phê duyệt trước khi thực thi.</param>
    /// <param name="TimeoutSeconds">Thời gian chờ tối đa khi gọi API (giây).</param>
    /// <param name="IsActive">Trạng thái kích hoạt công cụ.</param>
    /// <param name="CreatedDate">Thời điểm tạo bản ghi.</param>
    /// <param name="UpdatedDate">Thời điểm cập nhật bản ghi gần nhất.</param>
    public sealed record ToolDefinitionDto(
        Guid Id,
        string Name,
        string DisplayName,
        string Description,
        ToolType Type,
        string EndpointUrl,
        string HttpMethod,
        string HeadersConfig,
        string ParametersSchema,
        bool RequiresConfirmation,
        int TimeoutSeconds,
        bool IsActive,
        DateTimeOffset CreatedDate,
        DateTimeOffset? UpdatedDate)
    {
        /// <summary>
        /// Chuyển đổi từ thực thể Domain <see cref="ToolDefinition"/> sang DTO <see cref="ToolDefinitionDto"/>.
        /// </summary>
        /// <param name="entity">Thực thể Domain.</param>
        /// <param name="maskSecrets">Cờ yêu cầu che giấu các trường Header nhạy cảm (mặc định True).</param>
        /// <returns>Đối tượng DTO an toàn trả về Client.</returns>
        public static ToolDefinitionDto FromEntity(ToolDefinition entity, bool maskSecrets = true)
        {
            var headers = maskSecrets
                ? SecretMaskingHelper.MaskHeadersJson(entity.HeadersConfig)
                : entity.HeadersConfig;

            return new ToolDefinitionDto(
                entity.Id,
                entity.Name,
                entity.DisplayName,
                entity.Description,
                entity.Type,
                entity.EndpointUrl,
                entity.HttpMethod,
                headers,
                entity.ParametersSchema,
                entity.RequiresConfirmation,
                entity.TimeoutSeconds,
                entity.IsActive,
                entity.CreatedDate,
                entity.UpdatedDate);
        }
    }
}
