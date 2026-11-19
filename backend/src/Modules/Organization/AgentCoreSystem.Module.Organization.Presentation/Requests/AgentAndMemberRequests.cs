// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Organization.Domain.Enums;

namespace AgentCoreSystem.Module.Organization.Presentation.Requests
{
    /// <summary>
    /// Dữ liệu yêu cầu gán thành viên vào kênh làm việc.
    /// </summary>
    /// <param name="Type">Loại thành viên (Human hoặc Agent).</param>
    /// <param name="UserId">Mã người dùng (chỉ áp dụng khi Type = Human).</param>
    /// <param name="AgentProfileId">Mã hồ sơ Agent (chỉ áp dụng khi Type = Agent).</param>
    /// <param name="DisplayName">Tên hiển thị của thành viên trong kênh.</param>
    public record AssignMemberRequest(
        MemberType Type,
        Guid? UserId,
        Guid? AgentProfileId,
        string DisplayName);

    /// <summary>
    /// Dữ liệu yêu cầu tạo mới hồ sơ nhân sự AI Agent.
    /// </summary>
    /// <param name="Name">Tên định danh của Agent.</param>
    /// <param name="AvatarUrl">Đường dẫn ảnh đại diện.</param>
    /// <param name="Provider">Nhà cung cấp LLM.</param>
    /// <param name="ModelName">Tên model kỹ thuật.</param>
    /// <param name="SystemPrompt">Chỉ dẫn hệ thống cốt lõi.</param>
    /// <param name="Temperature">Độ sáng tạo của mô hình (0.0 đến 2.0).</param>
    /// <param name="PrivateMemoryNotes">Ghi chú bộ nhớ riêng ban đầu.</param>
    public record CreateAgentProfileRequest(
        string Name,
        string? AvatarUrl,
        ModelProviderType Provider,
        string ModelName,
        string SystemPrompt,
        decimal Temperature = 0.7m,
        string? PrivateMemoryNotes = null);
}
