// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgentCoreSystem.Module.System.Infrastructure.Jobs
{
    /// <summary>
    /// Background Job của Hangfire tự động dọn dẹp các bản ghi AuditLog cũ hơn 90 ngày.
    /// </summary>
    /// <remarks>
    /// <strong>Why:</strong> Đảm bảo dung lượng cơ sở dữ liệu luôn được tối ưu và tuân thủ chính sách lưu trữ kiểm toán.
    /// </remarks>
    public class SystemAuditLogCleanupJob(
        SystemDbContext dbContext,
        ILogger<SystemAuditLogCleanupJob> logger)
    {
        [Queue("system")]
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Hangfire Job: Bắt đầu quét và dọn dẹp AuditLogs quá hạn...");

            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-90);
            var deletedCount = await dbContext.AuditLogs
                .Where(a => a.CreatedDate < cutoffDate)
                .ExecuteDeleteAsync(cancellationToken);

            logger.LogInformation("Hangfire Job: Đã dọn dẹp hoàn tất {DeletedCount} bản ghi AuditLog cũ hơn 90 ngày.", deletedCount);
        }
    }
}

