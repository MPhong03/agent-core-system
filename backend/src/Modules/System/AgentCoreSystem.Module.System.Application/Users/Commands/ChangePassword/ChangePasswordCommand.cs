// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Domain.Entities;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace AgentCoreSystem.Module.System.Application.Users.Commands.ChangePassword
{
    /// <summary>
    /// Command yêu cầu đổi mật khẩu tài khoản người dùng.
    /// </summary>
    public record ChangePasswordCommand(
        Guid UserId,
        string CurrentPassword,
        string NewPassword) : ICommand;

    /// <summary>
    /// Validator kiểm tra độ phức tạp của mật khẩu và tính hợp lệ khi đổi mật khẩu.
    /// </summary>
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("Mã người dùng không được để trống.");
            RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống.");
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu mới phải có tối thiểu 6 ký tự.")
                .NotEqual(x => x.CurrentPassword).WithMessage("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
        }
    }

    /// <summary>
    /// Handler xử lý việc xác thực mật khẩu cũ và cập nhật mật khẩu mới mã hóa.
    /// </summary>
    /// <remarks>
    /// <para><strong>When to use:</strong> Được kích hoạt khi người dùng tự chủ động đổi mật khẩu hoặc quản trị viên đổi mật khẩu.</para>
    /// <para><strong>Why:</strong> Sử dụng trực tiếp UserManager.ChangePasswordAsync để đảm bảo thuật toán PBKDF2 tự động xác thực hash cũ theo thời gian hằng số (chống timing attack) và sinh một chuỗi salt 128-bit mới hoàn toàn cho mật khẩu mới.</para>
    /// </remarks>
    public class ChangePasswordCommandHandler(
        UserManager<ApplicationUser> userManager) : ICommandHandler<ChangePasswordCommand>
    {
        public async Task<Result> Handle(
            ChangePasswordCommand request, 
            CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null || user.IsDeleted)
            {
                return Result.Failure(Error.NotFound("User.NotFound", $"Không tìm thấy người dùng có mã định danh: '{request.UserId}'."));
            }

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errorMessages = string.Join("; ", result.Errors.Select(e => e.Description));
                return Result.Failure(Error.Validation("User.ChangePasswordFailed", errorMessages));
            }

            return Result.Success();
        }
    }
}

