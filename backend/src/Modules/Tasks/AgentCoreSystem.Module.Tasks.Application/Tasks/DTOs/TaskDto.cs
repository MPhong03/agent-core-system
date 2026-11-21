// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AgentCoreSystem.Module.Tasks.Domain.Entities;
using AgentCoreSystem.Module.Tasks.Domain.Enums;

namespace AgentCoreSystem.Module.Tasks.Application.Tasks.DTOs
{
    /// <summary>
    /// DTO thông tin phân công tác vụ.
    /// </summary>
    public record TaskAssignmentDto(
        Guid Id,
        Guid TaskId,
        Guid AssigneeMemberId,
        bool IsAgent,
        string RoleInTask);

    /// <summary>
    /// DTO tóm tắt tác vụ trên bảng Kanban.
    /// </summary>
    public record TaskDto(
        Guid Id,
        Guid ChannelId,
        string TaskCode,
        string Title,
        string Description,
        TaskItemStatus Status,
        TaskPriority Priority,
        DateTimeOffset? DueDate,
        DateTimeOffset? StartedDate,
        DateTimeOffset? CompletedDate,
        DateTimeOffset CreatedDate,
        List<TaskAssignmentDto> Assignments)
    {
        public static TaskDto FromEntity(TaskItem entity)
        {
            var assignments = new List<TaskAssignmentDto>();
            foreach (var a in entity.Assignments)
            {
                assignments.Add(new TaskAssignmentDto(a.Id, a.TaskId, a.AssigneeMemberId, a.IsAgent, a.RoleInTask));
            }

            return new TaskDto(
                entity.Id,
                entity.ChannelId,
                entity.TaskCode,
                entity.Title,
                entity.Description,
                entity.Status,
                entity.Priority,
                entity.DueDate,
                entity.StartedDate,
                entity.CompletedDate,
                entity.CreatedDate,
                assignments);
        }
    }
}
