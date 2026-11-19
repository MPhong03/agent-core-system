// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Application.Users.DTOs;
using AgentCoreSystem.Module.System.Domain.Repositories;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using AgentCoreSystem.Shared.Kernel.Responses;

namespace AgentCoreSystem.Module.System.Application.Users.Queries.GetUsers
{
    public record GetUsersQuery(int PageIndex = 1, int PageSize = 10, string? Keyword = null) 
        : IQuery<PagedResult<UserDto>>;

    public class GetUsersQueryHandler(IUserRepository userRepository) 
        : IQueryHandler<GetUsersQuery, PagedResult<UserDto>>
    {
        public async Task<Result<PagedResult<UserDto>>> Handle(
            GetUsersQuery request, 
            CancellationToken cancellationToken)
        {
            var pagedUsers = await userRepository.GetPagedAsync(
                request.PageIndex, 
                request.PageSize, 
                request.Keyword, 
                cancellationToken);

            var dtoList = new List<UserDto>();
            foreach (var user in pagedUsers.Items)
            {
                var roles = await userRepository.GetRolesAsync(user, cancellationToken);
                dtoList.Add(new UserDto(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.FullName,
                    user.PhoneNumber,
                    user.AvatarUrl,
                    user.IsActive,
                    roles,
                    user.CreatedDate));
            }

            var result = new PagedResult<UserDto>(dtoList, pagedUsers.TotalCount, pagedUsers.PageIndex, pagedUsers.PageSize);
            return Result.Success(result);
        }
    }
}

