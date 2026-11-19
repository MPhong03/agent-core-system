// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AgentCoreSystem.Shared.Persistence.Interceptors
{
    /// <summary>
    /// Interceptor tự động gán ngày kiểm toán và chuyển đổi xóa vật lý thành xóa mềm (Soft Delete).
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Can thiệp vào pipeline lưu dữ liệu của EF Core.</para>
    /// <para><strong>Why:</strong> Loại bỏ sự phụ thuộc vào dev phải tự gán CreatedDate, UpdatedDate thủ công và đảm bảo dữ liệu không bao giờ bị xóa mất khỏi database.</para>
    /// </remarks>
    public class AuditableEntityInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, 
            InterceptionResult<int> result)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, 
            InterceptionResult<int> result, 
            CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static void UpdateEntities(DbContext? context)
        {
            if (context is null) return;

            var utcNow = DateTimeOffset.UtcNow;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                // Kiểm toán Audit Dates
                if (entry.Entity is IAuditableEntity auditable)
                {
                    if (entry.State == EntityState.Added)
                    {
                        auditable.CreatedDate = utcNow;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        auditable.UpdatedDate = utcNow;
                    }
                }

                // Xử lý Xóa mềm (Soft Delete)
                if (entry.Entity is ISoftDeletable softDeletable && entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    softDeletable.IsDeleted = true;
                    softDeletable.DeletedDate = utcNow;
                }
            }
        }
    }
}

