// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Application.Contracts
{
    /// <summary>
    /// Giao diện DbContext cho phân hệ Agents (Tool Builder & Execution).
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Cung cấp các DbSet thực thể công cụ, phân quyền và nhật ký thực thi.</para>
    /// <para><strong>Why:</strong> Tuân thủ Clean Architecture, cho phép Application Handlers truy cập dữ liệu mà không bị phụ thuộc vào tầng Infrastructure.</para>
    /// </remarks>
    public interface IAgentsDbContext
    {
        // ── DbSets ──

        /// <summary>
        /// Tập hợp các định nghĩa công cụ (Tools).
        /// </summary>
        DbSet<ToolDefinition> Tools { get; }

        /// <summary>
        /// Tập hợp các định nghĩa công cụ (tên định danh thay thế).
        /// </summary>
        DbSet<ToolDefinition> ToolDefinitions { get; }

        /// <summary>
        /// Tập hợp các quyền truy cập công cụ được cấp cho Agent.
        /// </summary>
        DbSet<AgentToolAccess> AgentToolAccesses { get; }

        /// <summary>
        /// Tập hợp nhật ký các lần thực thi công cụ.
        /// </summary>
        DbSet<ToolExecutionLog> ToolExecutionLogs { get; }

        // ── Persistence Operations ──

        /// <summary>
        /// Lưu các thay đổi vào cơ sở dữ liệu trên schema 'agent'.
        /// </summary>
        /// <param name="cancellationToken">Mã hủy bỏ tác vụ không đồng bộ.</param>
        /// <returns>Số lượng bản ghi bị tác động trong cơ sở dữ liệu.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
