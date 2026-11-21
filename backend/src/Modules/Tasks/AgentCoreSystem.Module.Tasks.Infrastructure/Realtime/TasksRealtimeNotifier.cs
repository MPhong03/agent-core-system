// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Tasks.Application.Contracts;
using AgentCoreSystem.Module.Tasks.Application.Tasks.DTOs;
using AgentCoreSystem.Module.Tasks.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace AgentCoreSystem.Module.Tasks.Infrastructure.Realtime
{
    /// <summary>
    /// Triển khai dịch vụ phát sự kiện bảng Kanban thời gian thực qua SignalR Hub.
    /// </summary>
    public class TasksRealtimeNotifier(IHubContext<SignalRTasksHub> hubContext) : ITasksRealtimeNotifier
    {
        private string GetChannelGroup(Guid channelId) => $"tasks_channel_{channelId}";

        /// <inheritdoc />
        public async Task NotifyTaskCreatedAsync(
            Guid channelId,
            TaskDto task,
            CancellationToken cancellationToken = default)
        {
            await hubContext.Clients.Group(GetChannelGroup(channelId))
                .SendAsync("TaskCreated", task, cancellationToken);
        }

        /// <inheritdoc />
        public async Task NotifyTaskStatusChangedAsync(
            Guid channelId,
            Guid taskId,
            TaskItemStatus oldStatus,
            TaskItemStatus newStatus,
            Guid actorMemberId,
            CancellationToken cancellationToken = default)
        {
            await hubContext.Clients.Group(GetChannelGroup(channelId))
                .SendAsync("TaskStatusChanged", taskId, oldStatus.ToString(), newStatus.ToString(), actorMemberId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task NotifyTaskAssignedAsync(
            Guid channelId,
            Guid taskId,
            TaskAssignmentDto assignment,
            CancellationToken cancellationToken = default)
        {
            await hubContext.Clients.Group(GetChannelGroup(channelId))
                .SendAsync("TaskAssigned", taskId, assignment, cancellationToken);
        }

        /// <inheritdoc />
        public async Task NotifyTaskArtifactAddedAsync(
            Guid channelId,
            Guid taskId,
            TaskArtifactDto artifact,
            CancellationToken cancellationToken = default)
        {
            await hubContext.Clients.Group(GetChannelGroup(channelId))
                .SendAsync("TaskArtifactAdded", taskId, artifact, cancellationToken);
        }

        /// <inheritdoc />
        public async Task NotifyTaskReviewDecisionAsync(
            Guid channelId,
            Guid taskId,
            string decision,
            string? feedback,
            Guid reviewerMemberId,
            CancellationToken cancellationToken = default)
        {
            await hubContext.Clients.Group(GetChannelGroup(channelId))
                .SendAsync("TaskReviewDecision", taskId, decision, feedback, reviewerMemberId, cancellationToken);
        }
    }
}
