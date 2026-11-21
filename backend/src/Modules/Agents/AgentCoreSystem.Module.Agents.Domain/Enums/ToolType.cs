// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Module.Agents.Domain.Enums
{
    /// <summary>
    /// Loại hình công cụ được định nghĩa trong hệ thống Tool Builder.
    /// </summary>
    /// <remarks>
    /// <para>Hiện tại hỗ trợ tích hợp gọi REST API trực tiếp.</para>
    /// </remarks>
    public enum ToolType
    {
        /// <summary>
        /// Công cụ thực thi qua giao thức REST API HTTP request.
        /// </summary>
        RestApi = 1
    }
}
