// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Tasks.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Tasks.Application.Contracts
{
    /// <summary>
    /// Giao diện DbContext cho phân hệ Tasks (schema 'task').
    /// </summary>
    public interface ITasksDbContext
    {
        /// <summary>
        /// Tập hợp các tác vụ trên bảng Kanban.
        /// </summary>
        DbSet<TaskItem> Tasks { get; }

        /// <summary>
        /// Tập hợp các phân công trách nhiệm cho tác vụ.
        /// </summary>
        DbSet<TaskAssignment> TaskAssignments { get; }

        /// <summary>
        /// Tập hợp các sản phẩm đầu ra (Artifacts) của tác vụ.
        /// </summary>
        DbSet<TaskArtifact> TaskArtifacts { get; }

        /// <summary>
        /// Tập hợp các bản ghi lịch sử hoạt động và trạng thái tác vụ.
        /// </summary>
        DbSet<TaskActivityLog> TaskActivityLogs { get; }

        /// <summary>
        /// Lưu các thay đổi vào cơ sở dữ liệu bất đồng bộ.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
