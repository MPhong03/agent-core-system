// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using AgentCoreSystem.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Agents.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext độc quyền cho phân hệ Agents, quản lý dữ liệu trên schema 'agent'.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Quản lý các thực thể Công cụ (ToolDefinition), Phân quyền công cụ (AgentToolAccess) và Nhật ký thực thi (ToolExecutionLog).</para>
    /// <para><strong>Why:</strong> Thiết lập HasDefaultSchema("agent") và MigrationsHistoryTable riêng để đảm bảo tính độc lập và tự chủ (Autonomy) theo kiến trúc Modular Monolith.</para>
    /// <para>🔴 <strong>Lưu ý:</strong> Bảng lịch sử di chuyển dữ liệu được tách riêng thành <c>__EFMigrationsHistory_Agents</c>.</para>
    /// <para>⛔ <strong>Cấm:</strong> Không tạo Foreign Key vật lý sang các schema khác (org, chat, task, system).</para>
    /// </remarks>
    public class AgentsDbContext(DbContextOptions<AgentsDbContext> options) : BaseDbContext(options), IAgentsDbContext
    {
        // ── Schema & Constants ──

        /// <summary>
        /// Tên schema cơ sở dữ liệu độc quyền của phân hệ Agents.
        /// </summary>
        public const string SchemaName = "agent";

        // ── DbSets ──

        /// <inheritdoc />
        public DbSet<ToolDefinition> Tools => Set<ToolDefinition>();

        /// <inheritdoc />
        public DbSet<ToolDefinition> ToolDefinitions => Set<ToolDefinition>();

        /// <inheritdoc />
        public DbSet<AgentToolAccess> AgentToolAccesses => Set<AgentToolAccess>();

        /// <inheritdoc />
        public DbSet<ToolExecutionLog> ToolExecutionLogs => Set<ToolExecutionLog>();

        /// <inheritdoc />
        public DbSet<AgentWorkspaceConfig> AgentWorkspaceConfigs => Set<AgentWorkspaceConfig>();

        /// <inheritdoc />
        public DbSet<ScriptExecutionHistory> ScriptExecutionHistories => Set<ScriptExecutionHistory>();

        // ── Model Configuration ──

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema(SchemaName);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgentsDbContext).Assembly);
        }
    }
}
