// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Tasks.Application.Tasks.DTOs;
using AgentCoreSystem.Module.Tasks.Domain.Enums;

namespace AgentCoreSystem.Module.Tasks.Application.Contracts
{
    /// <summary>
    /// Giao diện thông báo thời gian thực cho bảng Kanban qua SignalR.
    /// </summary>
    public interface ITasksRealtimeNotifier
    {
        Task NotifyTaskCreatedAsync(Guid channelId, TaskDto task, CancellationToken cancellationToken = default);
        Task NotifyTaskStatusChangedAsync(Guid channelId, Guid taskId, TaskItemStatus oldStatus, TaskItemStatus newStatus, Guid actorMemberId, CancellationToken cancellationToken = default);
        Task NotifyTaskAssignedAsync(Guid channelId, Guid taskId, TaskAssignmentDto assignment, CancellationToken cancellationToken = default);
        Task NotifyTaskArtifactAddedAsync(Guid channelId, Guid taskId, TaskArtifactDto artifact, CancellationToken cancellationToken = default);
        Task NotifyTaskReviewDecisionAsync(Guid channelId, Guid taskId, string decision, string? feedback, Guid reviewerMemberId, CancellationToken cancellationToken = default);
    }
}
