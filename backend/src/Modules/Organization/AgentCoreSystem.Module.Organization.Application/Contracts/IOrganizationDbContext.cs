// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Organization.Application.Contracts
{
    /// <summary>
    /// Giao diện DbContext cho phân hệ Organization.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Cung cấp các DbSet thực thể phòng ban, kênh nhóm, thành viên và hồ sơ Agent.</para>
    /// <para><strong>Why:</strong> Tuân thủ Clean Architecture, cho phép Application truy cập dữ liệu mà không bị phụ thuộc vào chi tiết tầng Infrastructure.</para>
    /// </remarks>
    public interface IOrganizationDbContext
    {
        // ── DbSets ──

        /// <summary>
        /// Tập hợp các thực thể phòng ban trong tổ chức.
        /// </summary>
        DbSet<Department> Departments { get; }

        /// <summary>
        /// Tập hợp các thực thể kênh làm việc chuyên môn theo nhóm.
        /// </summary>
        DbSet<TeamChannel> Channels { get; }

        /// <summary>
        /// Tập hợp các thực thể thành viên (Human và Agent) trong các kênh.
        /// </summary>
        DbSet<Member> Members { get; }

        /// <summary>
        /// Tập hợp các thực thể hồ sơ định danh nhân sự AI.
        /// </summary>
        DbSet<AgentProfile> AgentProfiles { get; }

        // ── Persistence Operations ──

        /// <summary>
        /// Lưu các thay đổi hiện tại vào cơ sở dữ liệu trên schema 'org'.
        /// </summary>
        /// <param name="cancellationToken">Mã hủy bỏ tác vụ không đồng bộ.</param>
        /// <returns>Số lượng bản ghi bị tác động trong cơ sở dữ liệu.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
