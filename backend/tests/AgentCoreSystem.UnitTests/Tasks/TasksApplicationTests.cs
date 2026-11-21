// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.Commands.AppendAgentMemory;
using AgentCoreSystem.Module.Tasks.Application.Tasks.Commands;
using AgentCoreSystem.Module.Tasks.Application.Tasks.Queries;
using AgentCoreSystem.Module.Tasks.Domain.Entities;
using AgentCoreSystem.Module.Tasks.Domain.Enums;
using AgentCoreSystem.Module.Tasks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests.Tasks
{
    public class TasksApplicationTests
    {
        private DbContextOptions<TasksDbContext> CreateInMemoryOptions()
        {
            return new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task CreateTaskCommand_ShouldCreateTaskWithAutoCode()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            await using var context = new TasksDbContext(options);
            var handler = new CreateTaskCommandHandler(context);
            var channelId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();

            var command = new CreateTaskCommand(
                channelId,
                "Khảo sát thị trường",
                "Mô tả chi tiết",
                TaskPriority.High,
                DateTimeOffset.UtcNow.AddDays(2),
                null,
                null,
                creatorId);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("TSK-001", result.Value.TaskCode);
            Assert.Equal("Khảo sát thị trường", result.Value.Title);
            Assert.Equal(TaskItemStatus.ToDo, result.Value.Status);
        }

        [Fact]
        public async Task MoveTaskStatusCommand_ShouldUpdateStatus()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var channelId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var task = TaskItem.Create(channelId, "TSK-001", "Title", "Desc", TaskPriority.Medium, null, creatorId).Value;

            await using (var context = new TasksDbContext(options))
            {
                context.Tasks.Add(task);
                await context.SaveChangesAsync();
            }

            await using (var context = new TasksDbContext(options))
            {
                var handler = new MoveTaskStatusCommandHandler(context);
                var command = new MoveTaskStatusCommand(task.Id, TaskItemStatus.InProgress, creatorId, IsActorAgent: false);

                // Act
                var result = await handler.Handle(command, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(TaskItemStatus.InProgress, result.Value.Status);
            }
        }

        [Fact]
        public async Task AssignTaskCommand_ShouldAssignMemberToTask()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var channelId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var task = TaskItem.Create(channelId, "TSK-001", "Title", "Desc", TaskPriority.Medium, null, creatorId).Value;

            await using (var context = new TasksDbContext(options))
            {
                context.Tasks.Add(task);
                await context.SaveChangesAsync();
            }

            await using (var context = new TasksDbContext(options))
            {
                var handler = new AssignTaskCommandHandler(context);
                var command = new AssignTaskCommand(task.Id, agentId, IsAgent: true, "ResearchBot", creatorId);

                // Act
                var result = await handler.Handle(command, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Single(result.Value.Assignments);
                Assert.Equal(agentId, result.Value.Assignments.First().AssigneeMemberId);
                Assert.True(result.Value.Assignments.First().IsAgent);
            }
        }

        [Fact]
        public async Task ReviewTaskCommand_Approve_ShouldSetStatusDone()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var channelId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var task = TaskItem.Create(channelId, "TSK-001", "Title", "Desc", TaskPriority.Medium, null, creatorId).Value;
            task.MoveTo(TaskItemStatus.InProgress, creatorId, false);
            task.MoveTo(TaskItemStatus.InReview, agentId, true);

            await using (var context = new TasksDbContext(options))
            {
                context.Tasks.Add(task);
                await context.SaveChangesAsync();
            }

            var sender = Substitute.For<ISender>();

            await using (var context = new TasksDbContext(options))
            {
                var handler = new ReviewTaskCommandHandler(context, sender);
                var command = new ReviewTaskCommand(task.Id, IsApproved: true, "Đạt yêu cầu xuất sắc", creatorId);

                // Act
                var result = await handler.Handle(command, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(TaskItemStatus.Done, result.Value.Status);
            }
        }

        [Fact]
        public async Task ReviewTaskCommand_RequestRevision_ShouldSetStatusInProgressAndTriggerFeedback()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var channelId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var task = TaskItem.Create(channelId, "TSK-001", "Title", "Desc", TaskPriority.Medium, null, creatorId).Value;
            task.AssignTo(agentId, isAgent: true, "Researcher", creatorId);
            task.MoveTo(TaskItemStatus.InProgress, creatorId, false);
            task.MoveTo(TaskItemStatus.InReview, agentId, true);

            await using (var context = new TasksDbContext(options))
            {
                context.Tasks.Add(task);
                await context.SaveChangesAsync();
            }

            var sender = Substitute.For<ISender>();

            await using (var context = new TasksDbContext(options))
            {
                var handler = new ReviewTaskCommandHandler(context, sender);
                var command = new ReviewTaskCommand(task.Id, IsApproved: false, "Cần bổ sung số liệu 2026", creatorId);

                // Act
                var result = await handler.Handle(command, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(TaskItemStatus.InProgress, result.Value.Status);
                await sender.Received(1).Send(
                    Arg.Is<AppendAgentMemoryByMemberIdCommand>(c => c.MemberId == agentId && c.Feedback.Contains("2026")),
                    Arg.Any<CancellationToken>());
            }
        }

        [Fact]
        public async Task GetChannelTasksQuery_ShouldReturnTasksForChannel()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var channelId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var task1 = TaskItem.Create(channelId, "TSK-001", "Task 1", "Desc", TaskPriority.Low, null, creatorId).Value;
            var task2 = TaskItem.Create(channelId, "TSK-002", "Task 2", "Desc", TaskPriority.High, null, creatorId).Value;

            await using (var context = new TasksDbContext(options))
            {
                context.Tasks.AddRange(task1, task2);
                await context.SaveChangesAsync();
            }

            await using (var context = new TasksDbContext(options))
            {
                var handler = new GetChannelTasksQueryHandler(context);
                var query = new GetChannelTasksQuery(channelId);

                // Act
                var result = await handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(2, result.Value.Count);
            }
        }

        [Fact]
        public async Task GetTaskByIdQuery_ShouldReturnFullDetails()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var channelId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var task = TaskItem.Create(channelId, "TSK-001", "Task 1", "Desc", TaskPriority.Low, null, creatorId).Value;
            task.AddArtifact("report.md", "Storage/Artifacts/report.md", "text/markdown", 500, "Summary", creatorId);

            await using (var context = new TasksDbContext(options))
            {
                context.Tasks.Add(task);
                await context.SaveChangesAsync();
            }

            await using (var context = new TasksDbContext(options))
            {
                var handler = new GetTaskByIdQueryHandler(context);
                var query = new GetTaskByIdQuery(task.Id);

                // Act
                var result = await handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal("TSK-001", result.Value.TaskCode);
                Assert.Single(result.Value.Artifacts);
                Assert.Equal("report.md", result.Value.Artifacts.First().FileName);
            }
        }
    }
}
