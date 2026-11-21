// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Tools.Commands;
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Module.Agents.Application.Tools.Queries;
using AgentCoreSystem.Module.Agents.Domain.Enums;
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
using AgentCoreSystem.Shared.Kernel.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests.Tasks
{
    /// <summary>
    /// Kiểm thử tích hợp Tool Calling cho tác vụ nền Hangfire trong phân hệ Tasks (Sprint 3 - Task 7).
    /// </summary>
    public class TaskAgentExecutionToolCallingTests
    {
        [Fact]
        public async Task ExecuteTaskAsync_SafeToolExecution_ShouldCallToolAndRecordAndSaveArtifact()
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
            var agentProfile = new AgentProfile(
                "WeatherBot",
                null,
                ModelProviderType.OpenAI,
                "gpt-4o",
                "You are a weather expert.",
                toolExecutionMode: ToolExecutionMode.SemiAuto);

            var member = Member.CreateAgent(channelId, agentProfile.Id, "WeatherBot");

            await using (var orgContext = new OrganizationDbContext(orgDbOptions))
            {
                orgContext.AgentProfiles.Add(agentProfile);
                orgContext.Members.Add(member);
                await orgContext.SaveChangesAsync();
            }

            var task = TaskItem.Create(channelId, "TSK-TOOL-001", "Tra cứu thời tiết", "Lấy dữ liệu thời tiết Hà Nội", TaskPriority.Medium, null, creatorId).Value;
            task.AssignTo(member.Id, isAgent: true, "Researcher", creatorId);

            await using (var taskContext = new TasksDbContext(tasksDbOptions))
            {
                taskContext.Tasks.Add(task);
                await taskContext.SaveChangesAsync();
            }

            var toolId = Guid.NewGuid();
            var safeToolDto = new ToolDefinitionDto(
                toolId,
                "get_weather",
                "Get Weather",
                "Tra cứu thời tiết theo thành phố",
                ToolType.RestApi,
                "https://api.weather.com/v1",
                "GET",
                "{}",
                "{\"type\":\"object\"}",
                RequiresConfirmation: false,
                TimeoutSeconds: 30,
                IsActive: true,
                CreatedDate: DateTimeOffset.UtcNow,
                UpdatedDate: null);

            var mediator = Substitute.For<ISender>();
            mediator.Send(
                Arg.Is<GetAgentToolsQuery>(q => q.AgentProfileId == agentProfile.Id),
                Arg.Any<CancellationToken>())
                .Returns(Result.Success(new List<ToolDefinitionDto> { safeToolDto }));

            var llmClient = Substitute.For<ILLMClient>();
            var toolCallPayload = new LlmToolCallPayload("call_weather_1", "get_weather", "{\"city\":\"Hanoi\"}");

            // Lượt 1: LLM yêu cầu gọi tool
            var responseWithTool = new LlmChatResponse(
                Content: null,
                ToolCalls: new List<LlmToolCallPayload> { toolCallPayload });

            // Lượt 2: LLM trả về nội dung hoàn chỉnh
            var responseFinal = new LlmChatResponse(
                Content: "# Báo cáo Thời tiết Hà Nội\nNhiệt độ hiện tại 28 độ C, trời nắng nhẹ.",
                ToolCalls: null);

            llmClient.ChatWithToolsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<ChatMessagePayload>>(),
                Arg.Any<IEnumerable<LlmToolDefinitionPayload>>(),
                Arg.Any<decimal?>(),
                Arg.Any<CancellationToken>())
                .Returns(responseWithTool, responseFinal);

            var restApiExecutor = Substitute.For<IRestApiExecutor>();
            restApiExecutor.ExecuteAsync(Arg.Any<RestApiExecutionRequest>(), Arg.Any<CancellationToken>())
                .Returns(RestApiExecutionResult.Success(200, "{\"temp\":28,\"condition\":\"sunny\"}", 150));

            var storageService = Substitute.For<IArtifactStorageService>();
            storageService.SaveArtifactAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns("tsk-tool-001/artifact.md");

            var notifier = Substitute.For<ITasksRealtimeNotifier>();
            var logger = Substitute.For<ILogger<TaskAgentJobRunner>>();

            await using var executeTaskContext = new TasksDbContext(tasksDbOptions);
            await using var executeOrgContext = new OrganizationDbContext(orgDbOptions);

            var runner = new TaskAgentJobRunner(
                executeTaskContext,
                executeOrgContext,
                llmClient,
                storageService,
                notifier,
                mediator,
                restApiExecutor,
                logger);

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

            // Xác nhận Tool đã được thực thi an toàn
            await restApiExecutor.Received(1).ExecuteAsync(
                Arg.Is<RestApiExecutionRequest>(r => r.Url == safeToolDto.EndpointUrl && r.Method == safeToolDto.HttpMethod),
                Arg.Any<CancellationToken>());

            // Xác nhận Tool Execution Log đã được lưu với CallerType = Task
            await mediator.Received(1).Send(
                Arg.Is<RecordToolExecutionCommand>(cmd =>
                    cmd.ToolDefinitionId == safeToolDto.Id &&
                    cmd.AgentProfileId == agentProfile.Id &&
                    cmd.CallerType == ToolCallerType.Task &&
                    cmd.ExecutionSourceId == task.Id &&
                    cmd.IsSuccess),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteTaskAsync_ToolRequiringConfirmation_ShouldLogWarningAndSkipExecution_AndMoveToInReview()
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
            var agentProfile = new AgentProfile(
                "AdminBot",
                null,
                ModelProviderType.OpenAI,
                "gpt-4o",
                "You are an admin bot.",
                toolExecutionMode: ToolExecutionMode.SemiAuto);

            var member = Member.CreateAgent(channelId, agentProfile.Id, "AdminBot");

            await using (var orgContext = new OrganizationDbContext(orgDbOptions))
            {
                orgContext.AgentProfiles.Add(agentProfile);
                orgContext.Members.Add(member);
                await orgContext.SaveChangesAsync();
            }

            var task = TaskItem.Create(channelId, "TSK-TOOL-002", "Xóa database cũ", "Yêu cầu dọn dẹp hệ thống", TaskPriority.High, null, creatorId).Value;
            task.AssignTo(member.Id, isAgent: true, "Admin", creatorId);

            await using (var taskContext = new TasksDbContext(tasksDbOptions))
            {
                taskContext.Tasks.Add(task);
                await taskContext.SaveChangesAsync();
            }

            var toolId = Guid.NewGuid();
            var sensitiveToolDto = new ToolDefinitionDto(
                toolId,
                "delete_database",
                "Delete Database",
                "Xóa cơ sở dữ liệu",
                ToolType.RestApi,
                "https://api.internal.com/db/delete",
                "POST",
                "{}",
                "{\"type\":\"object\"}",
                RequiresConfirmation: true, // 🔴 Cần phê duyệt của con người
                TimeoutSeconds: 30,
                IsActive: true,
                CreatedDate: DateTimeOffset.UtcNow,
                UpdatedDate: null);

            var mediator = Substitute.For<ISender>();
            mediator.Send(
                Arg.Is<GetAgentToolsQuery>(q => q.AgentProfileId == agentProfile.Id),
                Arg.Any<CancellationToken>())
                .Returns(Result.Success(new List<ToolDefinitionDto> { sensitiveToolDto }));

            var llmClient = Substitute.For<ILLMClient>();
            var toolCallPayload = new LlmToolCallPayload("call_del_1", "delete_database", "{\"db\":\"test_db\"}");

            // Lượt 1: LLM yêu cầu gọi công cụ nhạy cảm
            var responseWithTool = new LlmChatResponse(
                Content: null,
                ToolCalls: new List<LlmToolCallPayload> { toolCallPayload });

            // Lượt 2: LLM nhận được thông báo Tool bị bỏ qua do cần phê duyệt và hoàn tất báo cáo
            var responseFinal = new LlmChatResponse(
                Content: "# Báo cáo Thực thi\nCông cụ delete_database yêu cầu phê duyệt nên đã được bỏ qua an toàn.",
                ToolCalls: null);

            llmClient.ChatWithToolsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<ChatMessagePayload>>(),
                Arg.Any<IEnumerable<LlmToolDefinitionPayload>>(),
                Arg.Any<decimal?>(),
                Arg.Any<CancellationToken>())
                .Returns(responseWithTool, responseFinal);

            var restApiExecutor = Substitute.For<IRestApiExecutor>();
            var storageService = Substitute.For<IArtifactStorageService>();
            storageService.SaveArtifactAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns("tsk-tool-002/artifact.md");

            var notifier = Substitute.For<ITasksRealtimeNotifier>();
            var logger = Substitute.For<ILogger<TaskAgentJobRunner>>();

            await using var executeTaskContext = new TasksDbContext(tasksDbOptions);
            await using var executeOrgContext = new OrganizationDbContext(orgDbOptions);

            var runner = new TaskAgentJobRunner(
                executeTaskContext,
                executeOrgContext,
                llmClient,
                storageService,
                notifier,
                mediator,
                restApiExecutor,
                logger);

            // Act
            await runner.ExecuteTaskAsync(task.Id, CancellationToken.None);

            // Assert
            var updatedTask = await executeTaskContext.Tasks
                .Include(t => t.Artifacts)
                .Include(t => t.ActivityLogs)
                .FirstOrDefaultAsync(t => t.Id == task.Id);

            Assert.NotNull(updatedTask);
            Assert.Equal(TaskItemStatus.InReview, updatedTask.Status);

            // Tuyệt đối không được gọi execute trên API đối với tool nhạy cảm trong background task
            await restApiExecutor.DidNotReceive().ExecuteAsync(
                Arg.Any<RestApiExecutionRequest>(),
                Arg.Any<CancellationToken>());

            // Tuyệt đối không ghi nhận thành công cho lệnh gọi này
            await mediator.DidNotReceive().Send(
                Arg.Is<RecordToolExecutionCommand>(c => c.ToolDefinitionId == sensitiveToolDto.Id),
                Arg.Any<CancellationToken>());

            // Xác nhận có cảnh báo trong TaskActivityLog về việc công cụ cần phê duyệt
            Assert.Contains(updatedTask.ActivityLogs, log =>
                log.Comment != null && (log.Comment.Contains("delete_database") || log.ActionType == "ToolConfirmationRequired"));
        }
    }
}
