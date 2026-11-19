// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Application.Contracts;
using AgentCoreSystem.Module.System.Domain.Entities;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace AgentCoreSystem.Module.System.Application.Auth.Commands.Login
{
    /// <summary>
    /// Command yêu cầu đăng nhập và xác thực danh tính người dùng.
    /// </summary>
    public record LoginCommand(string Email, string Password) : ICommand<AuthResponseDto>;

    /// <summary>
    /// DTO chứa thông tin JWT Token và dữ liệu người dùng sau khi đăng nhập thành công.
    /// </summary>
    public record AuthResponseDto(
        string Token,
        string RefreshToken,
        DateTime ExpiresAt,
        Guid UserId,
        string Email,
        string FullName,
        IReadOnlyList<string> Roles);

    /// <summary>
    /// Validator kiểm tra tính hợp lệ của dữ liệu đầu vào khi đăng nhập.
    /// </summary>
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Định dạng email không hợp lệ.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.");
        }
    }

    /// <summary>
    /// Handler xử lý xác thực thông tin đăng nhập và sinh chuỗi JWT Token.
    /// </summary>
    /// <remarks>
    /// <para><strong>When to use:</strong> Được gọi bởi AuthController endpoint POST /api/v1/auth/login.</para>
    /// <para><strong>Why:</strong> Sử dụng UserManager.CheckPasswordAsync để xác thực hash PBKDF2 của Microsoft Identity, đảm bảo an toàn tuyệt đối trước khi cấp JWT Token mang các quyền hạn được ký số.</para>
    /// </remarks>
    public class LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator jwtTokenGenerator) : ICommandHandler<LoginCommand, AuthResponseDto>
    {
        public async Task<Result<AuthResponseDto>> Handle(
            LoginCommand request, 
            CancellationToken cancellationToken)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return Result.Failure<AuthResponseDto>(
                    Error.Unauthorized("Auth.InvalidCredentials", "Tài khoản hoặc mật khẩu không chính xác."));
            }

            var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return Result.Failure<AuthResponseDto>(
                    Error.Unauthorized("Auth.InvalidCredentials", "Tài khoản hoặc mật khẩu không chính xác."));
            }

            var roles = await userManager.GetRolesAsync(user);

            // Sinh chuỗi JWT Token thật có chữ ký số HMAC-SHA256
            var token = jwtTokenGenerator.GenerateToken(user, roles);
            var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(120);

            return Result.Success(new AuthResponseDto(
                token,
                refreshToken,
                expiresAt,
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                [.. roles]));
        }
    }
}

