// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.Contracts;
using AgentCoreSystem.Module.Organization.Application.Departments.DTOs;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Organization.Application.Departments.Queries.GetDepartments
{
    /// <summary>
    /// Truy vấn lấy danh sách toàn bộ phòng ban kèm số lượng kênh trực thuộc.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Lấy danh sách các phòng ban hoạt động trong tổ chức.</para>
    /// <para><strong>Why:</strong> Phục vụ màn hình sơ đồ tổ chức và quản trị kênh làm việc.</para>
    /// </remarks>
    public record GetDepartmentsQuery() : IQuery<IReadOnlyList<DepartmentDto>>;

    /// <summary>
    /// Handler xử lý truy vấn danh sách phòng ban.
    /// </summary>
    public class GetDepartmentsQueryHandler(IOrganizationDbContext dbContext) 
        : IQueryHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
    {
        public async Task<Result<IReadOnlyList<DepartmentDto>>> Handle(
            GetDepartmentsQuery request, 
            CancellationToken cancellationToken)
        {
            var departments = await dbContext.Departments
                .AsNoTracking()
                .OrderBy(d => d.CreatedDate)
                .Select(d => new DepartmentDto(
                    d.Id,
                    d.Name,
                    d.Description,
                    d.CreatedDate,
                    d.Channels.Count(c => !c.IsDeleted)))
                .ToListAsync(cancellationToken);

            return Result.Success<IReadOnlyList<DepartmentDto>>(departments);
        }
    }
}
