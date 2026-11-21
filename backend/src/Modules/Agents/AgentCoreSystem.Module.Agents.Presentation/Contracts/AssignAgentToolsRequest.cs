// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace AgentCoreSystem.Module.Agents.Presentation.Contracts
{
    /// <summary>
    /// Dữ liệu yêu cầu phân quyền danh sách công cụ được phép sử dụng cho một Agent Profile.
    /// </summary>
    /// <param name="ToolDefinitionIds">Danh sách mã UUIDv7 các công cụ được cấp quyền cho Agent.</param>
    public sealed record AssignAgentToolsRequest(
        List<Guid> ToolDefinitionIds);
}
