// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Domain.Entities;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace AgentCoreSystem.Module.System.Application.Users.Commands.UpdateUser
{
    /// <summary>
    /// Command yêu cầu cập nhật thông tin cá nhân và vai trò của tài khoản người dùng.
    /// </summary>
    public record UpdateUserCommand(
        Guid Id,
        string FullName,
        string? PhoneNumber,
        bool IsActive,
        IReadOnlyList<string> Roles) : ICommand;

    /// <summary>
    /// Validator kiểm tra tính hợp lệ của dữ liệu chỉnh sửa người dùng.
    /// </summary>
    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Mã người dùng không được để trống.");
            RuleFor(x => x.FullName).NotEmpty().WithMessage("Họ và tên không được để trống.").MaximumLength(150);
            RuleFor(x => x.Roles).NotEmpty().WithMessage("Tài khoản phải có ít nhất một vai trò.");
        }
    }

    /// <summary>
    /// Handler xử lý việc cập nhật thông tin tài khoản và điều chỉnh phân quyền vai trò.
    /// </summary>
    /// <remarks>
    /// <para><strong>When to use:</strong> Được kích hoạt khi Quản trị viên cập nhật thông tin nhân viên hoặc điều chỉnh Role.</para>
    /// <para><strong>Why:</strong> Sử dụng Identity UserManager để đồng bộ lại UserRoles và cập nhật AuditableEntityInterceptor để tự động gán UpdatedDate mới nhất.</para>
    /// </remarks>
    public class UpdateUserCommandHandler(
        UserManager<ApplicationUser> userManager) : ICommandHandler<UpdateUserCommand>
    {
        public async Task<Result> Handle(
            UpdateUserCommand request, 
            CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(request.Id.ToString());
            if (user == null || user.IsDeleted)
            {
                return Result.Failure(Error.NotFound("User.NotFound", $"Không tìm thấy người dùng có mã định danh: '{request.Id}'."));
            }

            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.IsActive = request.IsActive;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                return Result.Failure(Error.Validation("User.UpdateFailed", errors));
            }

            // Đồng bộ lại vai trò (Roles)
            var currentRoles = await userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(request.Roles).ToList();
            var rolesToAdd = request.Roles.Except(currentRoles).ToList();

            if (rolesToRemove.Count > 0)
            {
                await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            if (rolesToAdd.Count > 0)
            {
                await userManager.AddToRolesAsync(user, rolesToAdd);
            }

            return Result.Success();
        }
    }
}

