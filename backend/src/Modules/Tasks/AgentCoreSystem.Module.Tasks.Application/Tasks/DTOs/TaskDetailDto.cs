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
    /// DTO sản phẩm đầu ra (Artifact).
    /// </summary>
    public record TaskArtifactDto(
        Guid Id,
        Guid TaskId,
        string FileName,
        string FilePath,
        string FileType,
        long FileSize,
        string Summary,
        Guid CreatedByMemberId,
        DateTimeOffset CreatedDate);

    /// <summary>
    /// DTO nhật ký hoạt động của tác vụ.
    /// </summary>
    public record TaskActivityLogDto(
        Guid Id,
        Guid TaskId,
        Guid ActorMemberId,
        string ActionType,
        TaskItemStatus? FromStatus,
        TaskItemStatus? ToStatus,
        string? Comment,
        DateTimeOffset CreatedDate);

    /// <summary>
    /// DTO chi tiết đầy đủ của một tác vụ bao gồm artifacts và lịch sử hoạt động.
    /// </summary>
    public record TaskDetailDto(
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
        List<TaskAssignmentDto> Assignments,
        List<TaskArtifactDto> Artifacts,
        List<TaskActivityLogDto> ActivityLogs)
    {
        public static TaskDetailDto FromEntity(TaskItem entity)
        {
            var assignments = new List<TaskAssignmentDto>();
            foreach (var a in entity.Assignments)
            {
                assignments.Add(new TaskAssignmentDto(a.Id, a.TaskId, a.AssigneeMemberId, a.IsAgent, a.RoleInTask));
            }

            var artifacts = new List<TaskArtifactDto>();
            foreach (var art in entity.Artifacts)
            {
                artifacts.Add(new TaskArtifactDto(
                    art.Id,
                    art.TaskId,
                    art.FileName,
                    art.FilePath,
                    art.FileType,
                    art.FileSize,
                    art.Summary,
                    art.CreatedByMemberId,
                    art.CreatedDate));
            }

            var logs = new List<TaskActivityLogDto>();
            foreach (var log in entity.ActivityLogs)
            {
                logs.Add(new TaskActivityLogDto(
                    log.Id,
                    log.TaskId,
                    log.ActorMemberId,
                    log.ActionType,
                    log.FromStatus,
                    log.ToStatus,
                    log.Comment,
                    log.CreatedDate));
            }

            return new TaskDetailDto(
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
                assignments,
                artifacts,
                logs);
        }
    }
}
