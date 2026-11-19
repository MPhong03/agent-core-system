// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgentCoreSystem.Module.System.Application.Contracts
{
    /// <summary>
    /// Giao diện công khai của Module System dành cho các module khác trong Solution tiêu thụ.
    /// </summary>
    /// <remarks>
    /// <strong>Why:</strong> Tuân thủ nguyên lý Module Boundary, ngăn chặn các module khác truy cập trực tiếp DbContext của System.
    /// </remarks>
    public interface ISystemModuleApi
    {
        Task<SystemUserSnapshot?> GetUserSnapshotAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> HasPermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default);
    }

    public record SystemUserSnapshot(Guid Id, string Email, string FullName, IReadOnlyList<string> Roles, bool IsActive);
}

