// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.Commands.AppendAgentMemory;
using AgentCoreSystem.Module.Tasks.Application.Contracts;
using AgentCoreSystem.Module.Tasks.Application.Tasks.DTOs;
using AgentCoreSystem.Module.Tasks.Domain.Enums;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Tasks.Application.Tasks.Commands
{
    /// <summary>
    /// Lệnh nghiệm thu tác vụ tại Cổng Nghiệm Thu (Review Gate: Human-in-the-Loop).
    /// </summary>
    public record ReviewTaskCommand(
        Guid TaskId,
        bool IsApproved,
        string? Feedback,
        Guid ReviewerMemberId) : ICommand<TaskDto>;

    /// <summary>
    /// Handler xử lý nghiệm thu tác vụ và kích hoạt Feedback Loop nếu yêu cầu sửa đổi.
    /// </summary>
    public class ReviewTaskCommandHandler(
        ITasksDbContext dbContext,
        ISender sender) : ICommandHandler<ReviewTaskCommand, TaskDto>
    {
        /// <inheritdoc />
        public async Task<Result<TaskDto>> Handle(
            ReviewTaskCommand request,
            CancellationToken cancellationToken)
        {
            var task = await dbContext.Tasks
                .Include(t => t.Assignments)
                .Include(t => t.Artifacts)
                .Include(t => t.ActivityLogs)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

            if (task == null)
            {
                return Result.Failure<TaskDto>(Error.NotFound("Task.NotFound", "Không tìm thấy tác vụ được chỉ định."));
            }

            if (task.Status != TaskItemStatus.InReview)
            {
                return Result.Failure<TaskDto>(Error.Validation(
                    "Task.NotInReview",
                    $"Chỉ có thể thực hiện nghiệm thu khi tác vụ ở trạng thái Chờ Nghiệm Thu (InReview). Trạng thái hiện tại: {task.Status}."));
            }

            if (request.IsApproved)
            {
                var approveResult = task.MoveTo(
                    TaskItemStatus.Done,
                    request.ReviewerMemberId,
                    isActorAgent: false,
                    request.Feedback ?? "Quản lý đã nghiệm thu đạt yêu cầu.");

                if (approveResult.IsFailure)
                {
                    return Result.Failure<TaskDto>(approveResult.Error);
                }
            }
            else
            {
                var reviseResult = task.MoveTo(
                    TaskItemStatus.InProgress,
                    request.ReviewerMemberId,
                    isActorAgent: false,
                    request.Feedback ?? "Yêu cầu chỉnh sửa và thực hiện lại.");

                if (reviseResult.IsFailure)
                {
                    return Result.Failure<TaskDto>(reviseResult.Error);
                }

                // 🔄 Feedback Loop: Tìm Agent được phân công để nạp phản hồi vào PrivateMemory
                var agentAssignment = task.Assignments.FirstOrDefault(a => a.IsAgent);
                if (agentAssignment != null && !string.IsNullOrWhiteSpace(request.Feedback))
                {
                    await sender.Send(
                        new AppendAgentMemoryByMemberIdCommand(agentAssignment.AssigneeMemberId, request.Feedback),
                        cancellationToken);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(TaskDto.FromEntity(task));
        }
    }
}
