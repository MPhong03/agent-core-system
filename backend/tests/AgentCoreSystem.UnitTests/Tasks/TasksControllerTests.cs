// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Tasks.Application.Contracts;
using AgentCoreSystem.Module.Tasks.Application.Tasks.Commands;
using AgentCoreSystem.Module.Tasks.Application.Tasks.DTOs;
using AgentCoreSystem.Module.Tasks.Application.Tasks.Queries;
using AgentCoreSystem.Module.Tasks.Domain.Enums;
using AgentCoreSystem.Module.Tasks.Presentation.Contracts;
using AgentCoreSystem.Module.Tasks.Presentation.Controllers.v1;
using AgentCoreSystem.Shared.Kernel.Primitives;
using AgentCoreSystem.Shared.Kernel.Responses;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests.Tasks
{
    public class TasksControllerTests
    {
        private readonly ISender _mediator = Substitute.For<ISender>();
        private readonly IArtifactStorageService _storageService = Substitute.For<IArtifactStorageService>();
        private readonly ITasksRealtimeNotifier _realtimeNotifier = Substitute.For<ITasksRealtimeNotifier>();
        private readonly IBackgroundJobClient _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        private readonly TasksController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public TasksControllerTests()
        {
            _controller = new TasksController(_mediator, _storageService, _realtimeNotifier, _backgroundJobClient);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
            }, "TestAuth"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetChannelTasks_ShouldReturnOkWithTasks()
        {
            // Arrange
            var channelId = Guid.NewGuid();
            var tasks = new List<TaskDto>
            {
                new(Guid.NewGuid(), channelId, "TSK-001", "Task 1", "Desc", TaskItemStatus.ToDo, TaskPriority.High, null, null, null, DateTimeOffset.UtcNow, new())
            };
            _mediator.Send(Arg.Any<GetChannelTasksQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success(tasks));

            // Act
            var actionResult = await _controller.GetChannelTasks(channelId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<List<TaskDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Single(response.Data);
        }

        [Fact]
        public async Task CreateTask_WithAgentAssignee_ShouldReturnCreatedAndEnqueueJob()
        {
            // Arrange
            var channelId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var request = new CreateTaskRequest("Tác vụ phân tích", "Chi tiết", TaskPriority.Medium, null, agentId, IsAgent: true);

            var createdTask = new TaskDto(
                Guid.NewGuid(),
                channelId,
                "TSK-002",
                request.Title,
                request.Description,
                TaskItemStatus.ToDo,
                request.Priority,
                null, null, null, DateTimeOffset.UtcNow,
                new() { new(Guid.NewGuid(), Guid.NewGuid(), agentId, true, "Assignee") });

            _mediator.Send(Arg.Any<CreateTaskCommand>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success(createdTask));

            // Act
            var actionResult = await _controller.CreateTask(channelId, request, CancellationToken.None);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status201Created, statusResult.StatusCode);
            await _realtimeNotifier.Received(1).NotifyTaskCreatedAsync(channelId, createdTask, Arg.Any<CancellationToken>());
            _backgroundJobClient.Received(1).Create(Arg.Any<Job>(), Arg.Any<IState>());
        }

        [Fact]
        public async Task ReviewTask_ShouldReturnOk()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var request = new ReviewTaskRequest(IsApproved: true, "Nghiệm thu đạt");
            var taskDto = new TaskDto(
                taskId,
                Guid.NewGuid(),
                "TSK-003",
                "Task",
                "Desc",
                TaskItemStatus.Done,
                TaskPriority.Low,
                null, null, null, DateTimeOffset.UtcNow,
                new());

            _mediator.Send(Arg.Any<ReviewTaskCommand>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success(taskDto));

            // Act
            var actionResult = await _controller.ReviewTask(taskId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<TaskDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(TaskItemStatus.Done, response.Data.Status);
        }
    }
}
