// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Module.Tasks.Domain.Enums
{
    /// <summary>
    /// Mức độ ưu tiên của tác vụ.
    /// </summary>
    public enum TaskPriority
    {
        /// <summary>
        /// Mức ưu tiên thấp.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Mức ưu tiên trung bình.
        /// </summary>
        Medium = 1,

        /// <summary>
        /// Mức ưu tiên cao.
        /// </summary>
        High = 2,

        /// <summary>
        /// Mức ưu tiên khẩn cấp.
        /// </summary>
        Urgent = 3
    }
}
