// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Tasks.Application.Contracts;

namespace AgentCoreSystem.Module.Tasks.Infrastructure.Storage
{
    /// <summary>
    /// Triển khai dịch vụ lưu trữ file artifact trên ổ đĩa cục bộ của máy chủ.
    /// </summary>
    public class LocalArtifactStorageService : IArtifactStorageService
    {
        private readonly string _baseStoragePath;

        public LocalArtifactStorageService()
        {
            _baseStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Artifacts");
            if (!Directory.Exists(_baseStoragePath))
            {
                Directory.CreateDirectory(_baseStoragePath);
            }
        }

        public LocalArtifactStorageService(string customBasePath)
        {
            _baseStoragePath = customBasePath;
            if (!Directory.Exists(_baseStoragePath))
            {
                Directory.CreateDirectory(_baseStoragePath);
            }
        }

        /// <inheritdoc />
        public async Task<string> SaveArtifactAsync(
            Guid channelId,
            Guid taskId,
            string fileName,
            string content,
            CancellationToken cancellationToken = default)
        {
            var relativeDir = Path.Combine(channelId.ToString(), taskId.ToString());
            var fullDir = Path.Combine(_baseStoragePath, relativeDir);

            if (!Directory.Exists(fullDir))
            {
                Directory.CreateDirectory(fullDir);
            }

            var safeFileName = Path.GetFileName(fileName);
            var relativePath = Path.Combine(relativeDir, safeFileName);
            var fullPath = Path.Combine(_baseStoragePath, relativePath);

            await File.WriteAllTextAsync(fullPath, content, Encoding.UTF8, cancellationToken);

            return relativePath.Replace('\\', '/');
        }

        /// <inheritdoc />
        public async Task<byte[]> ReadArtifactAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(_baseStoragePath, filePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Không tìm thấy file artifact tại đường dẫn yêu cầu.", fullPath);
            }

            return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }
    }
}
