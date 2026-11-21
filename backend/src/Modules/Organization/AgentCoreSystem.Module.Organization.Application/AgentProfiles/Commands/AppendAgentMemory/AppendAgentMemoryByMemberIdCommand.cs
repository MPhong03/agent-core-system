// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.Contracts;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Organization.Application.AgentProfiles.Commands.AppendAgentMemory
{
    /// <summary>
    /// Lệnh bổ sung ghi chú phản hồi nghiệm thu vào bộ nhớ cá nhân của Agent dựa trên MemberId.
    /// </summary>
    /// <remarks>
    /// Phục vụ cơ chế Human-in-the-Loop: Khi con người yêu cầu làm lại (Request Revision) trên Kanban, góp ý được lưu vào bộ nhớ riêng để Agent học hỏi.
    /// </remarks>
    public record AppendAgentMemoryByMemberIdCommand(
        Guid MemberId,
        string Feedback) : ICommand<bool>;

    /// <summary>
    /// Handler xử lý việc ghi phản hồi vào PrivateMemoryNotes của Agent.
    /// </summary>
    public class AppendAgentMemoryByMemberIdCommandHandler(IOrganizationDbContext dbContext)
        : ICommandHandler<AppendAgentMemoryByMemberIdCommand, bool>
    {
        /// <inheritdoc />
        public async Task<Result<bool>> Handle(
            AppendAgentMemoryByMemberIdCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Feedback))
            {
                return Result.Success(true);
            }

            var member = await dbContext.Members
                .FirstOrDefaultAsync(m => m.Id == request.MemberId, cancellationToken);

            if (member == null || !member.AgentProfileId.HasValue)
            {
                // Không phải thành viên Agent, bỏ qua ghi nhận
                return Result.Success(false);
            }

            var agentProfile = await dbContext.AgentProfiles
                .FirstOrDefaultAsync(p => p.Id == member.AgentProfileId.Value, cancellationToken);

            if (agentProfile == null)
            {
                return Result.Failure<bool>(Error.NotFound("AgentProfile.NotFound", "Không tìm thấy hồ sơ Agent tương ứng."));
            }

            var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm");
            var entry = $"[{timestamp}] Phản hồi nghiệm thu: {request.Feedback.Trim()}";

            agentProfile.PrivateMemoryNotes = string.IsNullOrWhiteSpace(agentProfile.PrivateMemoryNotes)
                ? entry
                : $"{agentProfile.PrivateMemoryNotes}\n{entry}";

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success(true);
        }
    }
}
