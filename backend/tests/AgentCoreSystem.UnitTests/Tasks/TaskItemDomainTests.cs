// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using AgentCoreSystem.Module.Tasks.Domain.Entities;
using AgentCoreSystem.Module.Tasks.Domain.Enums;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Xunit;

namespace AgentCoreSystem.UnitTests.Tasks
{
    public class TaskItemDomainTests
    {
        private readonly Guid _channelId = Guid.NewGuid();
        private readonly Guid _humanUserId = Guid.NewGuid();
        private readonly Guid _agentMemberId = Guid.NewGuid();

        [Fact]
        public void Create_WithValidParameters_ShouldInitializeCorrectly()
        {
            // Act
            var result = TaskItem.Create(
                _channelId,
                "TSK-001",
                "Thu thập dữ liệu đối thủ",
                "Mô tả chi tiết tác vụ",
                TaskPriority.High,
                DateTimeOffset.UtcNow.AddDays(3),
                _humanUserId);

            // Assert
            Assert.True(result.IsSuccess);
            var task = result.Value;
            Assert.Equal("TSK-001", task.TaskCode);
            Assert.Equal(TaskItemStatus.ToDo, task.Status);
            Assert.Equal(TaskPriority.High, task.Priority);
            Assert.Equal(_channelId, task.ChannelId);
            Assert.Single(task.ActivityLogs);
            Assert.Equal("Created", task.ActivityLogs.First().ActionType);
        }

        [Fact]
        public void MoveTo_FromToDoToInProgress_ShouldSucceed()
        {
            // Arrange
            var task = TaskItem.Create(_channelId, "TSK-001", "Title", "Desc", TaskPriority.Medium, null, _humanUserId).Value;

            // Act
            var moveResult = task.MoveTo(TaskItemStatus.InProgress, _humanUserId, isActorAgent: false, "Bắt đầu làm việc");

            // Assert
            Assert.True(moveResult.IsSuccess);
            Assert.Equal(TaskItemStatus.InProgress, task.Status);
            Assert.NotNull(task.StartedDate);
            Assert.Equal(2, task.ActivityLogs.Count);
        }

        [Fact]
        public void MoveTo_FromInProgressToInReview_ShouldSucceed()
        {
            // Arrange
            var task = TaskItem.Create(_channelId, "TSK-001", "Title", "Desc", TaskPriority.Medium, null, _humanUserId).Value;
            task.MoveTo(TaskItemStatus.InProgress, _humanUserId, isActorAgent: false);

            // Act
            var moveResult = task.MoveTo(TaskItemStatus.InReview, _agentMemberId, isActorAgent: true, "Agent đã hoàn tất báo cáo");

            // Assert
            Assert.True(moveResult.IsSuccess);
            Assert.Equal(TaskItemStatus.InReview, task.Status);
        }

        [Fact]
        public void MoveTo_FromInReviewToDone_ByAgent_ShouldBeForbidden()
        {
            // Arrange (Agent làm xong đưa vào InReview)
            var task = TaskItem.Create(_channelId, "TSK-001", "Title", "Desc", TaskPriority.Medium, null, _humanUserId).Value;
            task.MoveTo(TaskItemStatus.InProgress, _humanUserId, false);
            task.MoveTo(TaskItemStatus.InReview, _agentMemberId, true);

            // Act (Agent tự duyệt chuyển sang Done)
            var approveResult = task.MoveTo(TaskItemStatus.Done, _agentMemberId, isActorAgent: true, "Agent tự duyệt");

            // Assert
            Assert.True(approveResult.IsFailure);
            Assert.Equal(ErrorType.Forbidden, approveResult.Error.Type);
            Assert.Equal(TaskItemStatus.InReview, task.Status);
        }

        [Fact]
        public void MoveTo_FromInReviewToDone_ByHuman_ShouldSucceed()
        {
            // Arrange
            var task = TaskItem.Create(_channelId, "TSK-001", "Title", "Desc", TaskPriority.Medium, null, _humanUserId).Value;
            task.MoveTo(TaskItemStatus.InProgress, _humanUserId, false);
            task.MoveTo(TaskItemStatus.InReview, _agentMemberId, true);

            // Act (Human duyệt nghiệm thu)
            var approveResult = task.MoveTo(TaskItemStatus.Done, _humanUserId, isActorAgent: false, "Quản lý đã nghiệm thu");

            // Assert
            Assert.True(approveResult.IsSuccess);
            Assert.Equal(TaskItemStatus.Done, task.Status);
            Assert.NotNull(task.CompletedDate);
        }

        [Fact]
        public void MoveTo_FromInReviewToInProgress_ByHuman_ShouldRequestRevision()
        {
            // Arrange
            var task = TaskItem.Create(_channelId, "TSK-001", "Title", "Desc", TaskPriority.Medium, null, _humanUserId).Value;
            task.MoveTo(TaskItemStatus.InProgress, _humanUserId, false);
            task.MoveTo(TaskItemStatus.InReview, _agentMemberId, true);

            // Act (Human yêu cầu làm lại)
            var reviseResult = task.MoveTo(TaskItemStatus.InProgress, _humanUserId, isActorAgent: false, "Cần bổ sung nguồn tin năm 2026");

            // Assert
            Assert.True(reviseResult.IsSuccess);
            Assert.Equal(TaskItemStatus.InProgress, task.Status);
        }

        [Fact]
        public void MoveTo_InvalidJump_FromToDoToDone_ShouldFail()
        {
            // Arrange
            var task = TaskItem.Create(_channelId, "TSK-001", "Title", "Desc", TaskPriority.Medium, null, _humanUserId).Value;

            // Act
            var moveResult = task.MoveTo(TaskItemStatus.Done, _humanUserId, isActorAgent: false);

            // Assert
            Assert.True(moveResult.IsFailure);
            Assert.Equal(ErrorType.Validation, moveResult.Error.Type);
            Assert.Equal(TaskItemStatus.ToDo, task.Status);
        }

        [Fact]
        public void AssignTo_ShouldAddAssignmentAndLog()
        {
            // Arrange
            var task = TaskItem.Create(_channelId, "TSK-001", "Title", "Desc", TaskPriority.Medium, null, _humanUserId).Value;

            // Act
            var assignResult = task.AssignTo(_agentMemberId, isAgent: true, "ResearchBot", _humanUserId);

            // Assert
            Assert.True(assignResult.IsSuccess);
            Assert.Single(task.Assignments);
            var assignment = task.Assignments.First();
            Assert.Equal(_agentMemberId, assignment.AssigneeMemberId);
            Assert.True(assignment.IsAgent);
        }

        [Fact]
        public void AddArtifact_ShouldAppendArtifactAndLog()
        {
            // Arrange
            var task = TaskItem.Create(_channelId, "TSK-001", "Title", "Desc", TaskPriority.Medium, null, _humanUserId).Value;

            // Act
            var artifactResult = task.AddArtifact("report.md", "Storage/Artifacts/report.md", "text/markdown", 1024, "Tóm tắt báo cáo", _agentMemberId);

            // Assert
            Assert.True(artifactResult.IsSuccess);
            Assert.Single(task.Artifacts);
            var artifact = task.Artifacts.First();
            Assert.Equal("report.md", artifact.FileName);
            Assert.Equal(1024, artifact.FileSize);
        }
    }
}
