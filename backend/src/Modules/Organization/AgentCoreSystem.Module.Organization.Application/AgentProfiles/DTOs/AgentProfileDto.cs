// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Organization.Domain.Enums;

namespace AgentCoreSystem.Module.Organization.Application.AgentProfiles.DTOs
{
    /// <summary>
    /// Đối tượng truyền tải dữ liệu hồ sơ nhân sự AI Agent.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Chứa thông tin cấu hình Agent bao gồm Model, System Prompt, Temperature và bộ nhớ cá nhân.</para>
    /// <para><strong>When:</strong> Được trả về từ các truy vấn danh sách hoặc chi tiết hồ sơ Agent.</para>
    /// </remarks>
    /// <param name="Id">Mã định danh hồ sơ Agent.</param>
    /// <param name="Name">Tên định danh của Agent.</param>
    /// <param name="AvatarUrl">Đường dẫn ảnh đại diện.</param>
    /// <param name="Provider">Nhà cung cấp LLM (Gemini, OpenAI, Claude, Ollama).</param>
    /// <param name="ModelName">Tên model kỹ thuật.</param>
    /// <param name="SystemPrompt">Chỉ dẫn hệ thống cốt lõi định hình vai trò.</param>
    /// <param name="Temperature">Mức độ sáng tạo của mô hình.</param>
    /// <param name="PrivateMemoryNotes">Ghi chú bộ nhớ riêng.</param>
    /// <param name="CreatedDate">Thời điểm khởi tạo.</param>
    /// <param name="ToolExecutionMode">Chế độ thực thi công cụ (Auto, SemiAuto, NeedApprove).</param>
    public record AgentProfileDto(
        Guid Id,
        string Name,
        string? AvatarUrl,
        ModelProviderType Provider,
        string ModelName,
        string SystemPrompt,
        decimal Temperature,
        string? PrivateMemoryNotes,
        DateTimeOffset CreatedDate,
        ToolExecutionMode ToolExecutionMode = ToolExecutionMode.SemiAuto);
}
