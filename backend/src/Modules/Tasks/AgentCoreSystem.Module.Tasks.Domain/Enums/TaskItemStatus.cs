// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace AgentCoreSystem.Module.Tasks.Domain.Enums
{
    /// <summary>
    /// Trạng thái vòng đời của một tác vụ trên bảng Kanban.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskItemStatus
    {
        /// <summary>
        /// Tác vụ mới tạo, đang chờ xử lý.
        /// </summary>
        ToDo = 0,

        /// <summary>
        /// Tác vụ đang được thực thi (bởi Human hoặc Agent chạy ngầm qua Hangfire).
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Cổng nghiệm thu (Review Gate) - Agent đã hoàn tất, chờ con người duyệt.
        /// </summary>
        InReview = 2,

        /// <summary>
        /// Tác vụ đã hoàn thành và được nghiệm thu chính thức.
        /// </summary>
        Done = 3,

        /// <summary>
        /// Tác vụ thất bại hoặc gặp sự cố không thể khắc phục.
        /// </summary>
        Failed = 4
    }
}
