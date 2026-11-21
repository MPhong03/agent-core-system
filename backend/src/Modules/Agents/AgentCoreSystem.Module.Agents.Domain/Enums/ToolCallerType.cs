// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Module.Agents.Domain.Enums
{
    /// <summary>
    /// Nguồn gốc ngữ cảnh gọi công cụ (Caller Type).
    /// </summary>
    public enum ToolCallerType
    {
        /// <summary>
        /// Gọi từ luồng hội thoại Chat trực tiếp trong kênh.
        /// </summary>
        Chat = 1,

        /// <summary>
        /// Gọi từ luồng thực thi tác vụ nền Kanban Task.
        /// </summary>
        Task = 2
    }
}
