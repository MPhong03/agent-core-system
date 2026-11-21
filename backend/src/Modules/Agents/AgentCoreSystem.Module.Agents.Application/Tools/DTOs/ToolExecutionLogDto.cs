// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using AgentCoreSystem.Module.Agents.Domain.Enums;

namespace AgentCoreSystem.Module.Agents.Application.Tools.DTOs
{
    /// <summary>
    /// DTO thông tin nhật ký thực thi công cụ (Tool Execution Log).
    /// </summary>
    /// <param name="Id">Mã định danh bản ghi nhật ký (UUIDv7).</param>
    /// <param name="ToolDefinitionId">Mã công cụ được gọi.</param>
    /// <param name="AgentProfileId">Mã Agent thực hiện cuộc gọi.</param>
    /// <param name="CallerType">Ngữ cảnh gọi công cụ (Chat hoặc Task).</param>
    /// <param name="ExecutionSourceId">Mã nguồn phát sinh cuộc gọi (ChatMessageId hoặc TaskItemId).</param>
    /// <param name="InputArguments">Chuỗi JSON tham số đầu vào.</param>
    /// <param name="ResponsePayload">Chuỗi dữ liệu phản hồi từ API mục tiêu.</param>
    /// <param name="StatusCode">Mã trạng thái HTTP trả về.</param>
    /// <param name="DurationMs">Thời gian xử lý tính theo mili-giây.</param>
    /// <param name="IsSuccess">Cờ xác định cuộc gọi thành công hay thất bại.</param>
    /// <param name="ErrorMessage">Thông điệp lỗi chi tiết (nếu có).</param>
    /// <param name="CreatedDate">Thời điểm thực thi cuộc gọi.</param>
    public sealed record ToolExecutionLogDto(
        Guid Id,
        Guid ToolDefinitionId,
        Guid AgentProfileId,
        ToolCallerType CallerType,
        Guid ExecutionSourceId,
        string? InputArguments,
        string? ResponsePayload,
        int? StatusCode,
        long DurationMs,
        bool IsSuccess,
        string? ErrorMessage,
        DateTimeOffset CreatedDate)
    {
        /// <summary>
        /// Chuyển đổi từ thực thể Domain <see cref="ToolExecutionLog"/> sang DTO <see cref="ToolExecutionLogDto"/>.
        /// </summary>
        public static ToolExecutionLogDto FromEntity(ToolExecutionLog entity)
        {
            return new ToolExecutionLogDto(
                entity.Id,
                entity.ToolDefinitionId,
                entity.AgentProfileId,
                entity.CallerType,
                entity.ExecutionSourceId,
                entity.InputArguments,
                entity.ResponsePayload,
                entity.StatusCode,
                entity.DurationMs,
                entity.IsSuccess,
                entity.ErrorMessage,
                entity.CreatedDate);
        }
    }
}
