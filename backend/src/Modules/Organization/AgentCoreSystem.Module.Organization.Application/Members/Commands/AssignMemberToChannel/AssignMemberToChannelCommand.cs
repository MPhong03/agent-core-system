// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.Contracts;
using AgentCoreSystem.Module.Organization.Domain.Entities;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Organization.Application.Members.Commands.AssignMemberToChannel
{
    /// <summary>
    /// Lệnh gán một thành viên (Con người hoặc AI Agent) vào một kênh làm việc.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Kết nối một User hoặc AgentProfile vào kênh chỉ định với một tên hiển thị.</para>
    /// <para><strong>Why:</strong> Thiết lập quyền tham gia trao đổi, nhận tác vụ và tương tác nhóm trong kênh.</para>
    /// </remarks>
    /// <param name="ChannelId">Mã định danh kênh nhóm.</param>
    /// <param name="Type">Loại thành viên (Human hoặc Agent).</param>
    /// <param name="UserId">Mã người dùng (bắt buộc nếu Type là Human).</param>
    /// <param name="AgentProfileId">Mã hồ sơ Agent (bắt buộc nếu Type là Agent).</param>
    /// <param name="DisplayName">Tên đại diện/hiển thị trong kênh.</param>
    public record AssignMemberToChannelCommand(
        Guid ChannelId,
        MemberType Type,
        Guid? UserId,
        Guid? AgentProfileId,
        string DisplayName) : ICommand<Guid>;

    /// <summary>
    /// Bộ xác thực tính hợp lệ của lệnh gán thành viên vào kênh.
    /// </summary>
    public class AssignMemberToChannelCommandValidator : AbstractValidator<AssignMemberToChannelCommand>
    {
        public AssignMemberToChannelCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Mã kênh làm việc không được để trống.");

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Tên hiển thị không được để trống.")
                .MaximumLength(150).WithMessage("Tên hiển thị không được vượt quá 150 ký tự.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Mã người dùng (UserId) là bắt buộc đối với thành viên là Con người.")
                .When(x => x.Type == MemberType.Human);

            RuleFor(x => x.AgentProfileId)
                .NotEmpty().WithMessage("Mã hồ sơ Agent (AgentProfileId) là bắt buộc đối với thành viên là Agent.")
                .When(x => x.Type == MemberType.Agent);
        }
    }

    /// <summary>
    /// Handler xử lý gán thành viên vào kênh làm việc.
    /// </summary>
    public class AssignMemberToChannelCommandHandler(IOrganizationDbContext dbContext) 
        : ICommandHandler<AssignMemberToChannelCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(
            AssignMemberToChannelCommand request, 
            CancellationToken cancellationToken)
        {
            var channelExists = await dbContext.Channels
                .AnyAsync(c => c.Id == request.ChannelId, cancellationToken);

            if (!channelExists)
            {
                return Result.Failure<Guid>(Error.NotFound(
                    "Channel.NotFound", 
                    $"Không tìm thấy kênh với mã '{request.ChannelId}'."));
            }

            if (request.Type == MemberType.Agent)
            {
                var agentExists = await dbContext.AgentProfiles
                    .AnyAsync(a => a.Id == request.AgentProfileId, cancellationToken);

                if (!agentExists)
                {
                    return Result.Failure<Guid>(Error.NotFound(
                        "AgentProfile.NotFound", 
                        $"Không tìm thấy hồ sơ Agent với mã '{request.AgentProfileId}'."));
                }
            }

            var isDisplayNameConflict = await dbContext.Members
                .AnyAsync(m => m.ChannelId == request.ChannelId && m.DisplayName == request.DisplayName, cancellationToken);

            if (isDisplayNameConflict)
            {
                return Result.Failure<Guid>(Error.Conflict(
                    "Member.DisplayNameConflict", 
                    $"Thành viên với tên '{request.DisplayName}' đã tồn tại trong kênh này."));
            }

            var member = request.Type == MemberType.Human
                ? Member.CreateHuman(request.ChannelId, request.UserId!.Value, request.DisplayName)
                : Member.CreateAgent(request.ChannelId, request.AgentProfileId!.Value, request.DisplayName);

            dbContext.Members.Add(member);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(member.Id);
        }
    }
}
