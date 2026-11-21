// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Tasks.Domain.Enums;

namespace AgentCoreSystem.Module.Tasks.Presentation.Contracts
{
    public record CreateTaskRequest(
        string Title,
        string Description,
        TaskPriority Priority,
        DateTimeOffset? DueDate,
        Guid? AssigneeMemberId,
        bool? IsAgent);

    public record MoveTaskStatusRequest(
        TaskItemStatus NewStatus,
        string? Comment);

    public record AssignTaskRequest(
        Guid AssigneeMemberId,
        bool IsAgent,
        string RoleInTask);

    public record ReviewTaskRequest(
        bool IsApproved,
        string? Feedback);
}
