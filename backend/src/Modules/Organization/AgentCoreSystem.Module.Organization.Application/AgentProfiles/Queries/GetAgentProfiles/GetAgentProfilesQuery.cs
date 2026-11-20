// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.DTOs;
using AgentCoreSystem.Module.Organization.Application.Contracts;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Organization.Application.AgentProfiles.Queries.GetAgentProfiles
{
    /// <summary>
    /// Truy vấn danh sách toàn bộ hồ sơ nhân sự AI Agent trong hệ thống.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Lấy danh sách hồ sơ cấu hình Agent.</para>
    /// <para><strong>Why:</strong> Phục vụ danh mục Agent để người dùng chọn gán vào kênh làm việc hoặc cấu hình phân quyền.</para>
    /// </remarks>
    public record GetAgentProfilesQuery() : IQuery<IReadOnlyList<AgentProfileDto>>;

    /// <summary>
    /// Handler xử lý truy vấn danh sách hồ sơ Agent.
    /// </summary>
    /// <remarks>
    /// <para>Truy vấn không theo dõi (AsNoTracking) danh sách hồ sơ Agent sắp xếp theo thời điểm khởi tạo.</para>
    /// <para>⇒ Tự động loại bỏ các bản ghi bị xóa mềm nhờ Global Query Filter.</para>
    /// </remarks>
    public class GetAgentProfilesQueryHandler(IOrganizationDbContext dbContext) 
        : IQueryHandler<GetAgentProfilesQuery, IReadOnlyList<AgentProfileDto>>
    {
        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<AgentProfileDto>>> Handle(
            GetAgentProfilesQuery request, 
            CancellationToken cancellationToken)
        {
            var profiles = await dbContext.AgentProfiles
                .AsNoTracking()
                .OrderBy(a => a.CreatedDate)
                .Select(a => new AgentProfileDto(
                    a.Id,
                    a.Name,
                    a.AvatarUrl,
                    a.Provider,
                    a.ModelName,
                    a.SystemPrompt,
                    a.Temperature,
                    a.PrivateMemoryNotes,
                    a.CreatedDate))
                .ToListAsync(cancellationToken);

            return Result.Success<IReadOnlyList<AgentProfileDto>>(profiles);
        }
    }
}
