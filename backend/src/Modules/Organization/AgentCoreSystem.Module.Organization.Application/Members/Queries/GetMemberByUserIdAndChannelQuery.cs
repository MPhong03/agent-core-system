// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.Contracts;
using AgentCoreSystem.Module.Organization.Application.Members.DTOs;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Organization.Application.Members.Queries
{
    /// <summary>
    /// Truy vấn thông tin thành viên kênh dựa trên mã người dùng và mã kênh.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Lấy thông tin thành viên (Member) tương ứng với UserId trong một TeamChannel cụ thể.</para>
    /// <para><strong>Why:</strong> Hỗ trợ liên module (Chat -> Organization) khi người dùng gửi tin nhắn để xác định MemberId và DisplayName trong kênh mà không vi phạm ranh giới Module.</para>
    /// </remarks>
    /// <param name="UserId">Mã định danh người dùng từ hệ thống danh tính.</param>
    /// <param name="ChannelId">Mã định danh kênh làm việc.</param>
    public record GetMemberByUserIdAndChannelQuery(
        Guid UserId, 
        Guid ChannelId) : IQuery<MemberDto>;

    /// <summary>
    /// Handler xử lý truy vấn lấy thành viên kênh theo UserId và ChannelId.
    /// </summary>
    /// <remarks>
    /// <para>Truy vấn định danh thành viên liên module giữa Chat và Organization.</para>
    /// <para>AI-NOTE: Giúp phân hệ Chat xác định MemberId tương ứng khi người dùng gửi tin nhắn qua SignalR.</para>
    /// </remarks>
    public class GetMemberByUserIdAndChannelQueryHandler(IOrganizationDbContext dbContext)
        : IQueryHandler<GetMemberByUserIdAndChannelQuery, MemberDto>
    {
        /// <inheritdoc />
        public async Task<Result<MemberDto>> Handle(
            GetMemberByUserIdAndChannelQuery request, 
            CancellationToken cancellationToken)
        {
            var member = await dbContext.Members
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.UserId == request.UserId && m.ChannelId == request.ChannelId, 
                    cancellationToken);

            if (member is null)
            {
                return Result.Failure<MemberDto>(Error.NotFound(
                    "Member.NotFound",
                    $"Không tìm thấy thành viên với mã người dùng '{request.UserId}' trong kênh '{request.ChannelId}'."));
            }

            var dto = new MemberDto(
                member.Id,
                member.ChannelId,
                member.Type,
                member.UserId,
                member.AgentProfileId,
                member.DisplayName,
                member.CreatedDate);

            return Result.Success(dto);
        }
    }
}
