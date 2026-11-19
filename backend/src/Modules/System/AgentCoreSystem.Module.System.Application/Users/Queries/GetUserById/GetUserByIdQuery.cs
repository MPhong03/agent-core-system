// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Application.Users.DTOs;
using AgentCoreSystem.Module.System.Domain.Repositories;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.System.Application.Users.Queries.GetUserById
{
    /// <summary>
    /// Query truy vấn thông tin chi tiết của một tài khoản theo mã định danh UUIDv7.
    /// </summary>
    /// <param name="Id">Mã định danh duy nhất của người dùng.</param>
    public record GetUserByIdQuery(Guid Id) : IQuery<UserDetailDto>;

    /// <summary>
    /// Handler xử lý việc tìm kiếm và chuyển đổi dữ liệu chi tiết người dùng.
    /// </summary>
    /// <remarks>
    /// <para><strong>When to use:</strong> Được kích hoạt khi client cần xem màn hình chi tiết hoặc load dữ liệu vào Form chỉnh sửa.</para>
    /// <para><strong>Why:</strong> Truy vấn qua IUserRepository để tự động áp dụng Global Query Filter (loại trừ các bản ghi xóa mềm) và nạp danh sách Roles tương ứng.</para>
    /// </remarks>
    public class GetUserByIdQueryHandler(IUserRepository userRepository) 
        : IQueryHandler<GetUserByIdQuery, UserDetailDto>
    {
        public async Task<Result<UserDetailDto>> Handle(
            GetUserByIdQuery request, 
            CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(request.Id, cancellationToken);
            if (user == null)
            {
                return Result.Failure<UserDetailDto>(
                    Error.NotFound("User.NotFound", $"Không tìm thấy người dùng có mã định danh: '{request.Id}'."));
            }

            var roles = await userRepository.GetRolesAsync(user, cancellationToken);

            var detail = new UserDetailDto(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                user.PhoneNumber,
                user.AvatarUrl,
                user.IsActive,
                roles,
                user.CreatedDate,
                user.UpdatedDate);

            return Result.Success(detail);
        }
    }
}

