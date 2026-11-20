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

namespace AgentCoreSystem.Module.Organization.Application.Channels.Commands.CreateTeamChannel
{
    /// <summary>
    /// Lệnh tạo mới một kênh làm việc chuyên môn hóa theo nhóm trong phòng ban.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Khởi tạo một kênh cộng tác (ví dụ: tech-research, sprint-planning) thuộc một phòng ban cụ thể.</para>
    /// <para><strong>Why:</strong> Thiết lập phạm vi bộ nhớ chia sẻ và kết nối các thành viên Human/Agent cùng thực hiện nhiệm vụ.</para>
    /// </remarks>
    /// <param name="DepartmentId">Mã định danh phòng ban trực thuộc.</param>
    /// <param name="Name">Tên kênh làm việc định danh.</param>
    /// <param name="Description">Mô tả mục tiêu của kênh.</param>
    public record CreateTeamChannelCommand(
        Guid DepartmentId, 
        string Name, 
        string? Description = null) : ICommand<Guid>;

    /// <summary>
    /// Bộ xác thực tính hợp lệ của dữ liệu yêu cầu tạo kênh nhóm.
    /// </summary>
    public class CreateTeamChannelCommandValidator : AbstractValidator<CreateTeamChannelCommand>
    {
        public CreateTeamChannelCommandValidator()
        {
            RuleFor(x => x.DepartmentId)
                .NotEmpty().WithMessage("Mã phòng ban không được để trống.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên kênh làm việc không được để trống.")
                .MaximumLength(150).WithMessage("Tên kênh không được vượt quá 150 ký tự.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.");
        }
    }

    /// <summary>
    /// Handler xử lý nghiệp vụ tạo mới kênh nhóm làm việc.
    /// </summary>
    /// <remarks>
    /// <para>Kiểm tra sự tồn tại của phòng ban cha và tính duy nhất của tên kênh trong phòng ban đó.</para>
    /// <para>🔴 <strong>Ràng buộc:</strong> Trả về lỗi Conflict nếu tên kênh đã tồn tại trong cùng phòng ban.</para>
    /// </remarks>
    public class CreateTeamChannelCommandHandler(IOrganizationDbContext dbContext) 
        : ICommandHandler<CreateTeamChannelCommand, Guid>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> Handle(
            CreateTeamChannelCommand request, 
            CancellationToken cancellationToken)
        {
            var departmentExists = await dbContext.Departments
                .AnyAsync(d => d.Id == request.DepartmentId, cancellationToken);

            if (!departmentExists)
            {
                return Result.Failure<Guid>(Error.NotFound(
                    "Department.NotFound", 
                    $"Không tìm thấy phòng ban với mã '{request.DepartmentId}'."));
            }

            var isNameConflict = await dbContext.Channels
                .AnyAsync(c => c.DepartmentId == request.DepartmentId && c.Name == request.Name, cancellationToken);

            if (isNameConflict)
            {
                return Result.Failure<Guid>(Error.Conflict(
                    "Channel.NameConflict", 
                    $"Kênh với tên '{request.Name}' đã tồn tại trong phòng ban này."));
            }

            var channel = new TeamChannel(request.DepartmentId, request.Name, request.Description);

            dbContext.Channels.Add(channel);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(channel.Id);
        }
    }
}
