// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Tasks.Domain.Entities
{
    /// <summary>
    /// Đại diện sản phẩm đầu ra (Artifacts: báo cáo markdown, file dữ liệu, tài liệu trích xuất) do Agent hoặc Human bàn giao.
    /// </summary>
    public class TaskArtifact : BaseEntity
    {
        private TaskArtifact()
        {
        }

        public TaskArtifact(
            Guid taskId,
            string fileName,
            string filePath,
            string fileType,
            long fileSize,
            string summary,
            Guid createdByMemberId)
        {
            TaskId = taskId;
            FileName = fileName;
            FilePath = filePath;
            FileType = fileType;
            FileSize = fileSize;
            Summary = summary;
            CreatedByMemberId = createdByMemberId;
        }

        /// <summary>
        /// Mã tác vụ sở hữu artifact này.
        /// </summary>
        public Guid TaskId { get; private set; }

        /// <summary>
        /// Tên file hiển thị (ví dụ: market_research.md).
        /// </summary>
        public string FileName { get; private set; } = string.Empty;

        /// <summary>
        /// Đường dẫn lưu trữ vật lý trên server (Storage/Artifacts/{channelId}/{taskId}/...).
        /// </summary>
        public string FilePath { get; private set; } = string.Empty;

        /// <summary>
        /// MIME type hoặc phần mở rộng file (text/markdown, application/json, etc.).
        /// </summary>
        public string FileType { get; private set; } = string.Empty;

        /// <summary>
        /// Kích thước file tính theo bytes.
        /// </summary>
        public long FileSize { get; private set; }

        /// <summary>
        /// Tóm tắt nội dung chính của artifact.
        /// </summary>
        public string Summary { get; private set; } = string.Empty;

        /// <summary>
        /// Mã thành viên (Agent/Human) tạo ra artifact.
        /// </summary>
        public Guid CreatedByMemberId { get; private set; }
    }
}
