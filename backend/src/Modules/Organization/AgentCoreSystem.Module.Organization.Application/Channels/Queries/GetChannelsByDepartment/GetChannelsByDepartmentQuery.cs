// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.Channels.DTOs;
using AgentCoreSystem.Module.Organization.Application.Contracts;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Organization.Application.Channels.Queries.GetChannelsByDepartment
{
    /// <summary>
    /// Truy vấn lấy danh sách các kênh làm việc thuộc về một phòng ban.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Lọc danh sách các kênh nhóm theo DepartmentId kèm số lượng thành viên.</para>
    /// <para><strong>Why:</strong> Cung cấp danh mục kênh khi người dùng truy cập một phòng ban cụ thể.</para>
    /// </remarks>
    /// <param name="DepartmentId">Mã định danh phòng ban cần tra cứu.</param>
    public record GetChannelsByDepartmentQuery(Guid DepartmentId) : IQuery<IReadOnlyList<TeamChannelDto>>;

    /// <summary>
    /// Handler xử lý truy vấn danh sách kênh nhóm theo phòng ban.
    /// </summary>
    public class GetChannelsByDepartmentQueryHandler(IOrganizationDbContext dbContext) 
        : IQueryHandler<GetChannelsByDepartmentQuery, IReadOnlyList<TeamChannelDto>>
    {
        public async Task<Result<IReadOnlyList<TeamChannelDto>>> Handle(
            GetChannelsByDepartmentQuery request, 
            CancellationToken cancellationToken)
        {
            var departmentExists = await dbContext.Departments
                .AnyAsync(d => d.Id == request.DepartmentId, cancellationToken);

            if (!departmentExists)
            {
                return Result.Failure<IReadOnlyList<TeamChannelDto>>(Error.NotFound(
                    "Department.NotFound", 
                    $"Không tìm thấy phòng ban với mã '{request.DepartmentId}'."));
            }

            var channels = await dbContext.Channels
                .AsNoTracking()
                .Where(c => c.DepartmentId == request.DepartmentId)
                .OrderBy(c => c.CreatedDate)
                .Select(c => new TeamChannelDto(
                    c.Id,
                    c.DepartmentId,
                    c.Name,
                    c.Description,
                    c.CreatedDate,
                    c.Members.Count(m => !m.IsDeleted)))
                .ToListAsync(cancellationToken);

            return Result.Success<IReadOnlyList<TeamChannelDto>>(channels);
        }
    }
}
