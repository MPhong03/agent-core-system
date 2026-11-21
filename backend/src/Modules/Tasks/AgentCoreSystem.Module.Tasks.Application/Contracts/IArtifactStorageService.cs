// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AgentCoreSystem.Module.Tasks.Application.Contracts
{
    /// <summary>
    /// Dịch vụ lưu trữ và truy xuất các file sản phẩm đầu ra (Artifacts) của tác vụ.
    /// </summary>
    public interface IArtifactStorageService
    {
        /// <summary>
        /// Lưu nội dung artifact ra ổ đĩa hoặc storage và trả về đường dẫn tương đối.
        /// </summary>
        Task<string> SaveArtifactAsync(Guid channelId, Guid taskId, string fileName, string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đọc nội dung file artifact từ đường dẫn lưu trữ.
        /// </summary>
        Task<byte[]> ReadArtifactAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
