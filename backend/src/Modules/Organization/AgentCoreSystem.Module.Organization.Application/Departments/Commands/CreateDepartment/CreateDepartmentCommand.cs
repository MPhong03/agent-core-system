// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.Contracts;
using AgentCoreSystem.Module.Organization.Domain.Entities;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Organization.Application.Departments.Commands.CreateDepartment
{
    /// <summary>
    /// Lệnh tạo mới một phòng ban / khối chức năng.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Tiếp nhận tên và mô tả phòng ban để thiết lập một ranh giới tổ chức mới.</para>
    /// <para><strong>Why:</strong> Cung cấp đơn vị tổ chức cấp cao nhất chứa các kênh làm việc.</para>
    /// </remarks>
    /// <param name="Name">Tên phòng ban duy nhất.</param>
    /// <param name="Description">Mô tả chức năng nhiệm vụ của phòng ban.</param>
    public record CreateDepartmentCommand(string Name, string? Description = null) : ICommand<Guid>;

    /// <summary>
    /// Bộ xác thực tính hợp lệ của dữ liệu yêu cầu tạo phòng ban.
    /// </summary>
    public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
    {
        public CreateDepartmentCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên phòng ban không được để trống.")
                .MaximumLength(150).WithMessage("Tên phòng ban không được vượt quá 150 ký tự.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.");
        }
    }

    /// <summary>
    /// Handler xử lý nghiệp vụ tạo mới phòng ban.
    /// </summary>
    public class CreateDepartmentCommandHandler(IOrganizationDbContext dbContext) 
        : ICommandHandler<CreateDepartmentCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(
            CreateDepartmentCommand request, 
            CancellationToken cancellationToken)
        {
            var isNameConflict = await dbContext.Departments
                .AnyAsync(d => d.Name == request.Name, cancellationToken);

            if (isNameConflict)
            {
                return Result.Failure<Guid>(Error.Conflict(
                    "Department.NameConflict", 
                    $"Phòng ban với tên '{request.Name}' đã tồn tại trong hệ thống."));
            }

            var department = new Department(request.Name, request.Description);

            dbContext.Departments.Add(department);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(department.Id);
        }
    }
}
