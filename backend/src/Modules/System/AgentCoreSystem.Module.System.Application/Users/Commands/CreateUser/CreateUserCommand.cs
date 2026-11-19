// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Domain.Constants;
using AgentCoreSystem.Module.System.Domain.Entities;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace AgentCoreSystem.Module.System.Application.Users.Commands.CreateUser
{
    public record CreateUserCommand(
        string Email,
        string Password,
        string FullName,
        string? PhoneNumber,
        IReadOnlyList<string>? Roles) : ICommand<Guid>;

    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Địa chỉ Email không đúng định dạng.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu phải có tối thiểu 6 ký tự.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ và tên không được để trống.")
                .MaximumLength(150).WithMessage("Họ và tên không được vượt quá 150 ký tự.");
        }
    }

    public class CreateUserCommandHandler(
        UserManager<ApplicationUser> userManager) 
        : ICommandHandler<CreateUserCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(
            CreateUserCommand request, 
            CancellationToken cancellationToken)
        {
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Result.Failure<Guid>(Error.Conflict("User.EmailConflict", "Email này đã được sử dụng trong hệ thống."));
            }

            var newUser = new ApplicationUser
            {
                Email = request.Email,
                UserName = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(newUser, request.Password);
            if (!createResult.Succeeded)
            {
                var errorMessages = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return Result.Failure<Guid>(Error.Validation("User.CreateFailed", errorMessages));
            }

            var roles = request.Roles is { Count: > 0 } 
                ? request.Roles 
                : [SystemRoles.Employee];

            var roleResult = await userManager.AddToRolesAsync(newUser, roles);
            if (!roleResult.Succeeded)
            {
                var errorMessages = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                return Result.Failure<Guid>(Error.Validation("User.AssignRoleFailed", errorMessages));
            }

            return Result.Success(newUser.Id);
        }
    }
}

