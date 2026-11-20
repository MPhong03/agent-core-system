// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.Contracts;
using AgentCoreSystem.Module.Organization.Application.Members.DTOs;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Organization.Application.Members.Queries.GetChannelMembers
{
    /// <summary>
    /// Truy vấn danh sách các thành viên đang tham gia vào một kênh làm việc.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Lấy danh sách thành viên gồm Con người và Agent trực thuộc ChannelId.</para>
    /// <para><strong>Why:</strong> Phục vụ danh sách @mention trong Chat và danh sách giao việc trên Kanban.</para>
    /// </remarks>
    /// <param name="ChannelId">Mã định danh kênh nhóm.</param>
    public record GetChannelMembersQuery(Guid ChannelId) : IQuery<IReadOnlyList<MemberDto>>;

    /// <summary>
    /// Handler xử lý truy vấn danh sách thành viên kênh.
    /// </summary>
    /// <remarks>
    /// <para>Xác thực kênh tồn tại trước khi lấy danh sách thành viên.</para>
    /// <para>⇒ Danh sách bao gồm cả Human và Agent tham gia kênh, phục vụ phân tích @mention.</para>
    /// </remarks>
    public class GetChannelMembersQueryHandler(IOrganizationDbContext dbContext) 
        : IQueryHandler<GetChannelMembersQuery, IReadOnlyList<MemberDto>>
    {
        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<MemberDto>>> Handle(
            GetChannelMembersQuery request, 
            CancellationToken cancellationToken)
        {
            var channelExists = await dbContext.Channels
                .AnyAsync(c => c.Id == request.ChannelId, cancellationToken);

            if (!channelExists)
            {
                return Result.Failure<IReadOnlyList<MemberDto>>(Error.NotFound(
                    "Channel.NotFound", 
                    $"Không tìm thấy kênh với mã '{request.ChannelId}'."));
            }

            var members = await dbContext.Members
                .AsNoTracking()
                .Where(m => m.ChannelId == request.ChannelId)
                .OrderBy(m => m.CreatedDate)
                .Select(m => new MemberDto(
                    m.Id,
                    m.ChannelId,
                    m.Type,
                    m.UserId,
                    m.AgentProfileId,
                    m.DisplayName,
                    m.CreatedDate))
                .ToListAsync(cancellationToken);

            return Result.Success<IReadOnlyList<MemberDto>>(members);
        }
    }
}
