// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.Contracts;
using AgentCoreSystem.Module.Organization.Domain.Entities;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Module.Organization.Infrastructure.Persistence;
using AgentCoreSystem.Module.Tasks.Application.Contracts;
using AgentCoreSystem.Module.Tasks.Domain.Entities;
using AgentCoreSystem.Module.Tasks.Domain.Enums;
using AgentCoreSystem.Module.Tasks.Infrastructure.Jobs;
using AgentCoreSystem.Module.Tasks.Infrastructure.Persistence;
using AgentCoreSystem.Shared.Infrastructure.Http;
using AgentCoreSystem.Shared.Infrastructure.LLM;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests.Tasks
{
    public class TaskAgentJobRunnerTests
    {
        [Fact]
        public async Task ExecuteTaskAsync_SuccessfulRun_ShouldGenerateArtifactAndMoveToInReview()
        {
            // Arrange
            var tasksDbOptions = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var orgDbOptions = new DbContextOptionsBuilder<OrganizationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var channelId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var agentProfile = new AgentProfile("ResearchBot", null, ModelProviderType.OpenAI, "gpt-4o", "You are a researcher.");
            var member = Member.CreateAgent(channelId, agentProfile.Id, "ResearchBot");

            await using (var orgContext = new OrganizationDbContext(orgDbOptions))
            {
                orgContext.AgentProfiles.Add(agentProfile);
                orgContext.Members.Add(member);
                await orgContext.SaveChangesAsync();
            }

            var task = TaskItem.Create(channelId, "TSK-001", "Nghiên cứu AI", "Phân tích xu hướng", TaskPriority.High, null, creatorId).Value;
            task.AssignTo(member.Id, isAgent: true, "Researcher", creatorId);

            await using (var taskContext = new TasksDbContext(tasksDbOptions))
            {
                taskContext.Tasks.Add(task);
                await taskContext.SaveChangesAsync();
            }

            var llmClient = Substitute.For<ILLMClient>();
            async IAsyncEnumerable<string> MockStream()
            {
                yield return "# Báo cáo Nghiên cứu\n";
                yield return "Xu hướng năm 2026 là Multi-Agent.";
                await Task.CompletedTask;
            }
            llmClient.StreamChatAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<ChatMessagePayload>>(),
                Arg.Any<decimal?>(),
                Arg.Any<CancellationToken>())
                .Returns(_ => MockStream());

            var storageService = Substitute.For<IArtifactStorageService>();
            storageService.SaveArtifactAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns("tsk-001/report.md");

            var notifier = Substitute.For<ITasksRealtimeNotifier>();
            var mediator = Substitute.For<ISender>();
            var restApiExecutor = Substitute.For<IRestApiExecutor>();
            var logger = Substitute.For<ILogger<TaskAgentJobRunner>>();

            await using var executeTaskContext = new TasksDbContext(tasksDbOptions);
            await using var executeOrgContext = new OrganizationDbContext(orgDbOptions);

            var runner = new TaskAgentJobRunner(executeTaskContext, executeOrgContext, llmClient, storageService, notifier, mediator, restApiExecutor, logger);

            // Act
            await runner.ExecuteTaskAsync(task.Id, CancellationToken.None);

            // Assert
            var updatedTask = await executeTaskContext.Tasks
                .Include(t => t.Artifacts)
                .Include(t => t.ActivityLogs)
                .FirstOrDefaultAsync(t => t.Id == task.Id);

            Assert.NotNull(updatedTask);
            Assert.Equal(TaskItemStatus.InReview, updatedTask.Status);
            Assert.Single(updatedTask.Artifacts);
            Assert.Equal("tsk-001/report.md", updatedTask.Artifacts.First().FilePath);
            await notifier.Received(1).NotifyTaskStatusChangedAsync(
                channelId, task.Id, Arg.Any<TaskItemStatus>(), TaskItemStatus.InReview, member.Id, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteTaskAsync_WhenLLMThrowsException_ShouldSetStatusToFailed()
        {
            // Arrange
            var tasksDbOptions = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var orgDbOptions = new DbContextOptionsBuilder<OrganizationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var channelId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var member = Member.CreateAgent(channelId, Guid.NewGuid(), "BrokenBot");

            await using (var orgContext = new OrganizationDbContext(orgDbOptions))
            {
                orgContext.Members.Add(member);
                await orgContext.SaveChangesAsync();
            }

            var task = TaskItem.Create(channelId, "TSK-002", "Task lỗi", "Desc", TaskPriority.Low, null, creatorId).Value;
            task.AssignTo(member.Id, isAgent: true, "Worker", creatorId);

            await using (var taskContext = new TasksDbContext(tasksDbOptions))
            {
                taskContext.Tasks.Add(task);
                await taskContext.SaveChangesAsync();
            }

            var llmClient = Substitute.For<ILLMClient>();
#pragma warning disable CS0162
            async IAsyncEnumerable<string> ThrowStream()
            {
                throw new InvalidOperationException("API Key hết hạn");
                yield break;
            }
#pragma warning restore CS0162
            llmClient.StreamChatAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<ChatMessagePayload>>(),
                Arg.Any<decimal?>(),
                Arg.Any<CancellationToken>())
                .Returns(_ => ThrowStream());

            var storageService = Substitute.For<IArtifactStorageService>();
            var notifier = Substitute.For<ITasksRealtimeNotifier>();
            var mediator = Substitute.For<ISender>();
            var restApiExecutor = Substitute.For<IRestApiExecutor>();
            var logger = Substitute.For<ILogger<TaskAgentJobRunner>>();

            await using var executeTaskContext = new TasksDbContext(tasksDbOptions);
            await using var executeOrgContext = new OrganizationDbContext(orgDbOptions);

            var runner = new TaskAgentJobRunner(executeTaskContext, executeOrgContext, llmClient, storageService, notifier, mediator, restApiExecutor, logger);

            // Act
            await runner.ExecuteTaskAsync(task.Id, CancellationToken.None);

            // Assert
            var updatedTask = await executeTaskContext.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id);
            Assert.NotNull(updatedTask);
            Assert.Equal(TaskItemStatus.Failed, updatedTask.Status);
        }
    }
}
