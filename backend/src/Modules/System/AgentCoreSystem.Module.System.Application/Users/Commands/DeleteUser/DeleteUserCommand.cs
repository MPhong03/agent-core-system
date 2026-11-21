// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Domain.Entities;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.AspNetCore.Identity;

namespace AgentCoreSystem.Module.System.Application.Users.Commands.DeleteUser
{
    /// <summary>
    /// Command yêu cầu xóa mềm (Soft Delete) một tài khoản người dùng khỏi hệ thống.
    /// </summary>
    /// <param name="Id">Mã định danh duy nhất của tài khoản cần xóa.</param>
    public record DeleteUserCommand(Guid Id) : ICommand;

    /// <summary>
    /// Handler xử lý việc vô hiệu hóa và đánh dấu xóa mềm tài khoản.
    /// </summary>
    /// <remarks>
    /// <para><strong>When to use:</strong> Được kích hoạt khi Quản trị viên xóa một tài khoản khỏi danh sách.</para>
    /// <para><strong>Why:</strong> Không xóa vật lý khỏi SQL Server nhằm bảo toàn lịch sử kiểm toán (Audit Logs) và toàn vẹn dữ liệu liên kết. Gán IsDeleted = true, DeletedDate = UtcNow và khóa IsActive = false.</para>
    /// </remarks>
    public class DeleteUserCommandHandler(
        UserManager<ApplicationUser> userManager) : ICommandHandler<DeleteUserCommand>
    {
        public async Task<Result> Handle(
            DeleteUserCommand request, 
            CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(request.Id.ToString());
            if (user == null || user.IsDeleted)
            {
                return Result.Failure(Error.NotFound("User.NotFound", $"Không tìm thấy người dùng có mã định danh: '{request.Id}'."));
            }

            // AI-NOTE: 🔴 Bảo vệ tài khoản SuperAdmin không bị xóa mềm nhằm tránh rủi ro System Lockout
            var isSuperAdmin = await userManager.IsInRoleAsync(user, "SuperAdmin");
            if (isSuperAdmin)
            {
                return Result.Failure(Error.Conflict("User.SuperAdminProtected", "Không thể xóa tài khoản Quản trị viên cấp cao (SuperAdmin) nhằm đảm bảo an toàn hệ thống."));
            }

            user.IsDeleted = true;
            user.DeletedDate = DateTimeOffset.UtcNow;
            user.IsActive = false;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return Result.Failure(Error.Validation("User.DeleteFailed", "Không thể hoàn tất việc xóa tài khoản."));
            }

            return Result.Success();
        }
    }
}

