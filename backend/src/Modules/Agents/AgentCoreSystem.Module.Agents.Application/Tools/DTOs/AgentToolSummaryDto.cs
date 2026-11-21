// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Agents.Domain.Enums;

namespace AgentCoreSystem.Module.Agents.Application.Tools.DTOs
{
    /// <summary>
    /// DTO tóm tắt quyền truy cập công cụ của Agent phục vụ hiển thị trên bảng phân quyền.
    /// </summary>
    /// <param name="ToolDefinitionId">Mã định danh công cụ.</param>
    /// <param name="Name">Tên kỹ thuật của công cụ.</param>
    /// <param name="DisplayName">Tên hiển thị thân thiện.</param>
    /// <param name="Description">Mô tả mục đích công cụ.</param>
    /// <param name="Type">Loại hình công cụ (RestApi).</param>
    /// <param name="IsAssigned">Cờ xác định công cụ này đã được phân quyền cho Agent hay chưa.</param>
    /// <param name="IsActive">Trạng thái hoạt động của công cụ.</param>
    public sealed record AgentToolSummaryDto(
        Guid ToolDefinitionId,
        string Name,
        string DisplayName,
        string Description,
        ToolType Type,
        bool IsAssigned,
        bool IsActive);
}
