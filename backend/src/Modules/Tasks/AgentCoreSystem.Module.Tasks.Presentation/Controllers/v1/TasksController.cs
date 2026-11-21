// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Tasks.Application.Contracts;
using AgentCoreSystem.Module.Tasks.Application.Tasks.Commands;
using AgentCoreSystem.Module.Tasks.Application.Tasks.DTOs;
using AgentCoreSystem.Module.Tasks.Application.Tasks.Queries;
using AgentCoreSystem.Module.Tasks.Domain.Enums;
using AgentCoreSystem.Module.Tasks.Presentation.Contracts;
using AgentCoreSystem.Module.Tasks.Presentation.Extensions;
using AgentCoreSystem.Shared.Kernel.Responses;
using Asp.Versioning;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.Tasks.Presentation.Controllers.v1
{
    /// <summary>
    /// Phân hệ Quản lý Tác vụ, Bảng Kanban &amp; Cổng Nghiệm Thu Human-in-the-Loop (Phiên bản API 1.0).
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}")]
    [Authorize]
    public class TasksController(
        ISender mediator,
        IArtifactStorageService storageService,
        ITasksRealtimeNotifier realtimeNotifier,
        IBackgroundJobClient backgroundJobClient) : BaseApiController
    {
        /// <summary>
        /// Truy vấn danh sách các tác vụ trong kênh để hiển thị bảng Kanban.
        /// </summary>
        [HttpGet("channels/{channelId:guid}/tasks")]
        [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetChannelTasks(Guid channelId, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetChannelTasksQuery(channelId), cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            return Ok(ApiResponse<List<TaskDto>>.Ok(result.Value, "Lấy danh sách tác vụ thành công."));
        }

        /// <summary>
        /// Lấy chi tiết đầy đủ của một tác vụ bao gồm artifacts và lịch sử hoạt động.
        /// </summary>
        [HttpGet("tasks/{taskId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<TaskDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTaskById(Guid taskId, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetTaskByIdQuery(taskId), cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            return Ok(ApiResponse<TaskDetailDto>.Ok(result.Value, "Lấy chi tiết tác vụ thành công."));
        }

        /// <summary>
        /// Tạo một tác vụ mới trên bảng Kanban của kênh.
        /// </summary>
        [HttpPost("channels/{channelId:guid}/tasks")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTask(
            Guid channelId,
            [FromBody] CreateTaskRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var command = new CreateTaskCommand(
                channelId,
                request.Title,
                request.Description,
                request.Priority,
                request.DueDate,
                request.AssigneeMemberId,
                request.IsAgent,
                userId);

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            // Broadcast sự kiện tạo task qua SignalR
            await realtimeNotifier.NotifyTaskCreatedAsync(channelId, result.Value, cancellationToken);

            // Nếu giao việc cho Agent ngay khi tạo và được kích hoạt, có thể xếp hàng Hangfire
            if (request.AssigneeMemberId.HasValue && request.IsAgent == true)
            {
                backgroundJobClient.Enqueue<ITaskAgentJobRunner>(runner => runner.ExecuteTaskAsync(result.Value.Id, CancellationToken.None));
            }

            return StatusCode(StatusCodes.Status201Created, ApiResponse<TaskDto>.Ok(result.Value, "Tạo tác vụ thành công."));
        }

        /// <summary>
        /// Kéo thả cập nhật trạng thái tác vụ trên bảng Kanban.
        /// </summary>
        [HttpPatch("tasks/{taskId:guid}/status")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MoveTaskStatus(
            Guid taskId,
            [FromBody] MoveTaskStatusRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var command = new MoveTaskStatusCommand(
                taskId,
                request.NewStatus,
                userId,
                IsActorAgent: false,
                request.Comment);

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            // Broadcast sự kiện đổi trạng thái
            await realtimeNotifier.NotifyTaskStatusChangedAsync(
                result.Value.ChannelId,
                taskId,
                TaskItemStatus.ToDo, // Status được cập nhật tự động
                request.NewStatus,
                userId,
                cancellationToken);

            // Nếu chuyển sang InProgress và người thực hiện là Agent -> kích hoạt Hangfire
            if (request.NewStatus == TaskItemStatus.InProgress && result.Value.Assignments.Any(a => a.IsAgent))
            {
                backgroundJobClient.Enqueue<ITaskAgentJobRunner>(runner => runner.ExecuteTaskAsync(taskId, CancellationToken.None));
            }

            return Ok(ApiResponse<TaskDto>.Ok(result.Value, "Cập nhật trạng thái tác vụ thành công."));
        }

        /// <summary>
        /// Phân công người chịu trách nhiệm cho tác vụ.
        /// </summary>
        [HttpPost("tasks/{taskId:guid}/assign")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignTask(
            Guid taskId,
            [FromBody] AssignTaskRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var command = new AssignTaskCommand(
                taskId,
                request.AssigneeMemberId,
                request.IsAgent,
                request.RoleInTask,
                userId);

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            var assignment = result.Value.Assignments.FirstOrDefault(a => a.AssigneeMemberId == request.AssigneeMemberId);
            if (assignment != null)
            {
                await realtimeNotifier.NotifyTaskAssignedAsync(result.Value.ChannelId, taskId, assignment, cancellationToken);
            }

            if (request.IsAgent && result.Value.Status == TaskItemStatus.InProgress)
            {
                backgroundJobClient.Enqueue<ITaskAgentJobRunner>(runner => runner.ExecuteTaskAsync(taskId, CancellationToken.None));
            }

            return Ok(ApiResponse<TaskDto>.Ok(result.Value, "Phân công tác vụ thành công."));
        }

        /// <summary>
        /// Nghiệm thu tác vụ tại Cổng Nghiệm Thu (Approve hoặc Request Revision).
        /// </summary>
        [HttpPost("tasks/{taskId:guid}/review")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReviewTask(
            Guid taskId,
            [FromBody] ReviewTaskRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var command = new ReviewTaskCommand(
                taskId,
                request.IsApproved,
                request.Feedback,
                userId);

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            var decision = request.IsApproved ? "Approved" : "RevisionRequested";
            await realtimeNotifier.NotifyTaskReviewDecisionAsync(
                result.Value.ChannelId,
                taskId,
                decision,
                request.Feedback,
                userId,
                cancellationToken);

            // Nếu yêu cầu sửa đổi (Request Revision) và task được giao cho Agent -> tự động chạy lại Hangfire
            if (!request.IsApproved && result.Value.Assignments.Any(a => a.IsAgent))
            {
                backgroundJobClient.Enqueue<ITaskAgentJobRunner>(runner => runner.ExecuteTaskAsync(taskId, CancellationToken.None));
            }

            var message = request.IsApproved
                ? "Duyệt nghiệm thu tác vụ thành công."
                : "Yêu cầu làm lại đã được gửi đến Agent.";

            return Ok(ApiResponse<TaskDto>.Ok(result.Value, message));
        }

        /// <summary>
        /// Tải về file artifact đính kèm của tác vụ.
        /// </summary>
        [HttpGet("tasks/{taskId:guid}/artifacts/{artifactId:guid}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadArtifact(
            Guid taskId,
            Guid artifactId,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetTaskByIdQuery(taskId), cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            var artifact = result.Value.Artifacts.FirstOrDefault(a => a.Id == artifactId);
            if (artifact == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Không tìm thấy artifact yêu cầu trong tác vụ."
                });
            }

            try
            {
                var bytes = await storageService.ReadArtifactAsync(artifact.FilePath, cancellationToken);
                return File(bytes, "application/octet-stream", artifact.FileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "File Not Found",
                    Detail = "File vật lý không tồn tại trên máy chủ."
                });
            }
        }
    }
}
