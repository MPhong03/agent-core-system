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
using AgentCoreSystem.Module.Chat.Application.Contracts;
using AgentCoreSystem.Module.Chat.Application.Messages.DTOs;
using AgentCoreSystem.Module.Chat.Application.Tools.Commands;
using AgentCoreSystem.Module.Chat.Application.Tools.DTOs;
using AgentCoreSystem.Module.Chat.Domain.Entities;
using AgentCoreSystem.Module.Chat.Domain.Enums;
using AgentCoreSystem.Module.Chat.Infrastructure.Hubs;
using AgentCoreSystem.Module.Chat.Infrastructure.Persistence;
using AgentCoreSystem.Module.Chat.Infrastructure.Services;
using AgentCoreSystem.Module.Chat.Presentation.Controllers.v1;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.DTOs;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.Queries.GetAgentProfiles;
using AgentCoreSystem.Module.Organization.Application.Members.DTOs;
using AgentCoreSystem.Module.Organization.Application.Members.Queries;
using AgentCoreSystem.Module.Organization.Application.Members.Queries.GetChannelMembers;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Shared.Application.Contracts;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Infrastructure.Http;
using AgentCoreSystem.Shared.Infrastructure.LLM;
using AgentCoreSystem.Shared.Kernel.Primitives;
using AgentCoreSystem.Shared.Kernel.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests.Chat
{
    public class ToolApprovalDispatcherTests
    {
        private static ChatDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ChatDbContext(options);
        }

        private static ToolDefinitionDto CreateSampleTool(
            string name = "get_weather",
            bool requiresConfirmation = false,
            string url = "https://api.weather.com/v1")
        {
            return new ToolDefinitionDto(
                Id: Guid.NewGuid(),
                Name: name,
                DisplayName: name,
                Description: "Fetches weather information",
                Type: ToolType.RestApi,
                EndpointUrl: url,
                HttpMethod: "GET",
                HeadersConfig: "{}",
                ParametersSchema: "{\"type\":\"object\"}",
                RequiresConfirmation: requiresConfirmation,
                TimeoutSeconds: 30,
                IsActive: true,
                CreatedDate: DateTimeOffset.UtcNow,
                UpdatedDate: null);
        }

        [Fact]
        public async Task DispatchMessageAsync_AutoMode_ShouldExecuteToolImmediatelyWithoutPausing()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var llmFactory = Substitute.For<ILLMProviderFactory>();
            var llmClient = Substitute.For<ILLMClient>();
            var hubContext = Substitute.For<IHubContext<SignalRChatHub>>();
            var clients = Substitute.For<IHubClients>();
            var clientProxy = Substitute.For<IClientProxy>();
            var restApiExecutor = Substitute.For<IRestApiExecutor>();

            hubContext.Clients.Returns(clients);
            clients.Group(Arg.Any<string>()).Returns(clientProxy);

            var channelId = Guid.NewGuid();
            var agentMemberId = Guid.NewGuid();
            var agentProfileId = Guid.NewGuid();

            var memberDto = new MemberDto(
                Id: agentMemberId,
                ChannelId: channelId,
                Type: MemberType.Agent,
                UserId: null,
                AgentProfileId: agentProfileId,
                DisplayName: "WeatherBot",
                CreatedDate: DateTimeOffset.UtcNow);

            var profileDto = new AgentProfileDto(
                Id: agentProfileId,
                Name: "WeatherBot",
                AvatarUrl: null,
                Provider: ModelProviderType.OpenAI,
                ModelName: "gpt-4o",
                SystemPrompt: "You are a weather bot.",
                Temperature: 0.2m,
                PrivateMemoryNotes: null,
                CreatedDate: DateTimeOffset.UtcNow,
                ToolExecutionMode: ToolExecutionMode.Auto);

            var weatherTool = CreateSampleTool("get_weather", requiresConfirmation: true); // even with confirmation = true, Auto mode overrides

            sender.Send(Arg.Is<GetChannelMembersQuery>(q => q.ChannelId == channelId), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<MemberDto>>(new List<MemberDto> { memberDto }));

            sender.Send(Arg.Any<GetAgentProfilesQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<AgentProfileDto>>(new List<AgentProfileDto> { profileDto }));

            sender.Send(Arg.Is<GetAgentToolsQuery>(q => q.AgentProfileId == agentProfileId), Arg.Any<CancellationToken>())
                .Returns(Result.Success(new List<ToolDefinitionDto> { weatherTool }));

            llmFactory.GetClient(Arg.Any<string>()).Returns(llmClient);

            // Turn 1: LLM requests tool call
            var toolCallPayload = new LlmToolCallPayload("call_w1", "get_weather", "{\"city\":\"Hanoi\"}");
            var turn1Response = new LlmChatResponse(
                Content: null,
                ToolCalls: new List<LlmToolCallPayload> { toolCallPayload });

            // Turn 2: LLM completes answer after tool output
            var turn2Response = new LlmChatResponse(
                Content: "Thời tiết tại Hà Nội hiện tại là 25°C, trời quang đãng.",
                ToolCalls: null);

            llmClient.ChatWithToolsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<ChatMessagePayload>>(),
                Arg.Any<IEnumerable<LlmToolDefinitionPayload>>(),
                Arg.Any<decimal?>(),
                Arg.Any<CancellationToken>())
                .Returns(turn1Response, turn2Response);

            restApiExecutor.ExecuteAsync(Arg.Any<RestApiExecutionRequest>(), Arg.Any<CancellationToken>())
                .Returns(RestApiExecutionResult.Success(200, "{\"temp\": 25, \"desc\": \"Clear\"}", 120));

            var dispatcher = new MessageDispatcherService(dbContext, sender, llmFactory, hubContext, restApiExecutor: restApiExecutor);

            var userMessage = new ChatMessageDto(
                Id: Guid.NewGuid(),
                ChannelId: channelId,
                SenderMemberId: Guid.NewGuid(),
                SenderDisplayName: "Human User",
                IsAgent: false,
                Content: "Cho tôi biết thời tiết ở Hà Nội @WeatherBot",
                CallDepth: 0,
                ParentMessageId: null,
                Mentions: new List<Guid> { agentMemberId },
                CreatedDate: DateTimeOffset.UtcNow);

            // Act
            await dispatcher.DispatchMessageAsync(userMessage, new List<Guid> { agentMemberId });

            // Assert
            // 1. Tool executed immediately via IRestApiExecutor
            await restApiExecutor.Received(1).ExecuteAsync(Arg.Is<RestApiExecutionRequest>(r =>
                r.Url == weatherTool.EndpointUrl), Arg.Any<CancellationToken>());

            // 2. Broadcasted ToolExecuting SignalR event
            await clientProxy.Received().SendCoreAsync(
                "ToolExecuting",
                Arg.Any<object[]>(),
                Arg.Any<CancellationToken>());

            // 3. No PendingToolCall created
            Assert.Empty(await dbContext.PendingToolCalls.ToListAsync());

            // 4. Completed message sent to SignalR
            await clientProxy.Received().SendCoreAsync(
                "ReceiveMessageCompleted",
                Arg.Any<object[]>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DispatchMessageAsync_SemiAutoMode_WithRequiresConfirmation_ShouldPauseAndCreatePendingToolCall()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var llmFactory = Substitute.For<ILLMProviderFactory>();
            var llmClient = Substitute.For<ILLMClient>();
            var hubContext = Substitute.For<IHubContext<SignalRChatHub>>();
            var clients = Substitute.For<IHubClients>();
            var clientProxy = Substitute.For<IClientProxy>();
            var restApiExecutor = Substitute.For<IRestApiExecutor>();

            hubContext.Clients.Returns(clients);
            clients.Group(Arg.Any<string>()).Returns(clientProxy);

            var channelId = Guid.NewGuid();
            var agentMemberId = Guid.NewGuid();
            var agentProfileId = Guid.NewGuid();

            var memberDto = new MemberDto(
                Id: agentMemberId,
                ChannelId: channelId,
                Type: MemberType.Agent,
                UserId: null,
                AgentProfileId: agentProfileId,
                DisplayName: "OrderAgent",
                CreatedDate: DateTimeOffset.UtcNow);

            var profileDto = new AgentProfileDto(
                Id: agentProfileId,
                Name: "OrderAgent",
                AvatarUrl: null,
                Provider: ModelProviderType.OpenAI,
                ModelName: "gpt-4o",
                SystemPrompt: "You manage orders.",
                Temperature: 0.1m,
                PrivateMemoryNotes: null,
                CreatedDate: DateTimeOffset.UtcNow,
                ToolExecutionMode: ToolExecutionMode.SemiAuto);

            var cancelTool = CreateSampleTool("cancel_order", requiresConfirmation: true, url: "https://api.orders.com/cancel");

            sender.Send(Arg.Is<GetChannelMembersQuery>(q => q.ChannelId == channelId), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<MemberDto>>(new List<MemberDto> { memberDto }));

            sender.Send(Arg.Any<GetAgentProfilesQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<AgentProfileDto>>(new List<AgentProfileDto> { profileDto }));

            sender.Send(Arg.Is<GetAgentToolsQuery>(q => q.AgentProfileId == agentProfileId), Arg.Any<CancellationToken>())
                .Returns(Result.Success(new List<ToolDefinitionDto> { cancelTool }));

            llmFactory.GetClient(Arg.Any<string>()).Returns(llmClient);

            var toolCallPayload = new LlmToolCallPayload("call_cancel_1", "cancel_order", "{\"orderId\":\"12345\"}");
            var llmResponse = new LlmChatResponse(
                Content: "Tôi sẽ giúp bạn hủy đơn hàng này.",
                ToolCalls: new List<LlmToolCallPayload> { toolCallPayload });

            llmClient.ChatWithToolsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<ChatMessagePayload>>(),
                Arg.Any<IEnumerable<LlmToolDefinitionPayload>>(),
                Arg.Any<decimal?>(),
                Arg.Any<CancellationToken>())
                .Returns(llmResponse);

            var dispatcher = new MessageDispatcherService(dbContext, sender, llmFactory, hubContext, restApiExecutor: restApiExecutor);

            var userMessage = new ChatMessageDto(
                Id: Guid.NewGuid(),
                ChannelId: channelId,
                SenderMemberId: Guid.NewGuid(),
                SenderDisplayName: "Customer",
                IsAgent: false,
                Content: "@OrderAgent hủy đơn 12345 giúp tôi",
                CallDepth: 0,
                ParentMessageId: null,
                Mentions: new List<Guid> { agentMemberId },
                CreatedDate: DateTimeOffset.UtcNow);

            // Act
            await dispatcher.DispatchMessageAsync(userMessage, new List<Guid> { agentMemberId });

            // Assert
            // 1. Tool should NOT be executed yet
            await restApiExecutor.DidNotReceiveWithAnyArgs().ExecuteAsync(Arg.Any<RestApiExecutionRequest>(), Arg.Any<CancellationToken>());

            // 2. PendingToolCall must be created
            var pendingCalls = await dbContext.PendingToolCalls.ToListAsync();
            Assert.Single(pendingCalls);
            var pending = pendingCalls[0];
            Assert.Equal("call_cancel_1", pending.ToolCallId);
            Assert.Equal("cancel_order", pending.ToolName);
            Assert.Equal(PendingToolCallStatus.Pending, pending.Status);
            Assert.True(pending.ExpiresAt > DateTimeOffset.UtcNow);

            // 3. Broadcasted ToolApprovalRequested SignalR event
            await clientProxy.Received().SendCoreAsync(
                "ToolApprovalRequested",
                Arg.Any<object[]>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DispatchMessageAsync_NeedApproveMode_ShouldPauseAndCreatePendingToolCall_EvenWhenRequiresConfirmationFalse()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var llmFactory = Substitute.For<ILLMProviderFactory>();
            var llmClient = Substitute.For<ILLMClient>();
            var hubContext = Substitute.For<IHubContext<SignalRChatHub>>();
            var clients = Substitute.For<IHubClients>();
            var clientProxy = Substitute.For<IClientProxy>();
            var restApiExecutor = Substitute.For<IRestApiExecutor>();

            hubContext.Clients.Returns(clients);
            clients.Group(Arg.Any<string>()).Returns(clientProxy);

            var channelId = Guid.NewGuid();
            var agentMemberId = Guid.NewGuid();
            var agentProfileId = Guid.NewGuid();

            var memberDto = new MemberDto(
                Id: agentMemberId,
                ChannelId: channelId,
                Type: MemberType.Agent,
                UserId: null,
                AgentProfileId: agentProfileId,
                DisplayName: "StrictAgent",
                CreatedDate: DateTimeOffset.UtcNow);

            var profileDto = new AgentProfileDto(
                Id: agentProfileId,
                Name: "StrictAgent",
                AvatarUrl: null,
                Provider: ModelProviderType.OpenAI,
                ModelName: "gpt-4o",
                SystemPrompt: "Strict agent.",
                Temperature: 0.1m,
                PrivateMemoryNotes: null,
                CreatedDate: DateTimeOffset.UtcNow,
                ToolExecutionMode: ToolExecutionMode.NeedApprove);

            var safeTool = CreateSampleTool("get_info", requiresConfirmation: false);

            sender.Send(Arg.Is<GetChannelMembersQuery>(q => q.ChannelId == channelId), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<MemberDto>>(new List<MemberDto> { memberDto }));

            sender.Send(Arg.Any<GetAgentProfilesQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<AgentProfileDto>>(new List<AgentProfileDto> { profileDto }));

            sender.Send(Arg.Is<GetAgentToolsQuery>(q => q.AgentProfileId == agentProfileId), Arg.Any<CancellationToken>())
                .Returns(Result.Success(new List<ToolDefinitionDto> { safeTool }));

            llmFactory.GetClient(Arg.Any<string>()).Returns(llmClient);

            var toolCallPayload = new LlmToolCallPayload("call_safe_1", "get_info", "{\"id\":\"1\"}");
            llmClient.ChatWithToolsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<ChatMessagePayload>>(),
                Arg.Any<IEnumerable<LlmToolDefinitionPayload>>(),
                Arg.Any<decimal?>(),
                Arg.Any<CancellationToken>())
                .Returns(new LlmChatResponse(null, new List<LlmToolCallPayload> { toolCallPayload }));

            var dispatcher = new MessageDispatcherService(dbContext, sender, llmFactory, hubContext, restApiExecutor: restApiExecutor);

            var userMessage = new ChatMessageDto(
                Id: Guid.NewGuid(),
                ChannelId: channelId,
                SenderMemberId: Guid.NewGuid(),
                SenderDisplayName: "User",
                IsAgent: false,
                Content: "@StrictAgent show info",
                CallDepth: 0,
                ParentMessageId: null,
                Mentions: new List<Guid> { agentMemberId },
                CreatedDate: DateTimeOffset.UtcNow);

            // Act
            await dispatcher.DispatchMessageAsync(userMessage, new List<Guid> { agentMemberId });

            // Assert
            await restApiExecutor.DidNotReceiveWithAnyArgs().ExecuteAsync(Arg.Any<RestApiExecutionRequest>(), Arg.Any<CancellationToken>());
            var pendingCalls = await dbContext.PendingToolCalls.ToListAsync();
            Assert.Single(pendingCalls);
            Assert.Equal("call_safe_1", pendingCalls[0].ToolCallId);
        }

        [Fact]
        public async Task ConfirmToolCallCommand_ShouldExecuteTool_SaveToolMessage_MarkConfirmed_AndResumeDispatcher()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var restApiExecutor = Substitute.For<IRestApiExecutor>();
            var messageDispatcher = Substitute.For<IMessageDispatcherService>();

            var channelId = Guid.NewGuid();
            var assistantMsg = new ChatMessage(
                channelId: channelId,
                senderMemberId: Guid.NewGuid(),
                senderDisplayName: "OrderAgent",
                isAgent: true,
                content: "Yêu cầu gọi công cụ cancel_order",
                callDepth: 1);
            dbContext.Messages.Add(assistantMsg);

            var pendingCall = new PendingToolCall(
                channelId: channelId,
                chatMessageId: assistantMsg.Id,
                toolCallId: "call_abc999",
                toolName: "cancel_order",
                argumentsJson: "{\"orderId\":\"12345\"}",
                expiresAt: DateTimeOffset.UtcNow.AddMinutes(5));
            dbContext.PendingToolCalls.Add(pendingCall);
            await dbContext.SaveChangesAsync();

            var sampleTool = CreateSampleTool("cancel_order", requiresConfirmation: true, url: "https://api.orders.com/cancel");
            var toolsList = new List<ToolDefinitionDto> { sampleTool };

            sender.Send(Arg.Any<GetToolDefinitionsQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success(toolsList));

            restApiExecutor.ExecuteAsync(Arg.Any<RestApiExecutionRequest>(), Arg.Any<CancellationToken>())
                .Returns(RestApiExecutionResult.Success(200, "{\"success\": true}", 95));

            var handler = new ConfirmToolCallCommandHandler(dbContext, sender, restApiExecutor, messageDispatcher);
            var command = new ConfirmToolCallCommand(pendingCall.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);

            // 1. Tool executed via RestApiExecutor
            await restApiExecutor.Received(1).ExecuteAsync(Arg.Is<RestApiExecutionRequest>(r =>
                r.Url == sampleTool.EndpointUrl), Arg.Any<CancellationToken>());

            // 2. PendingToolCall marked Confirmed
            var updatedCall = await dbContext.PendingToolCalls.FindAsync(pendingCall.Id);
            Assert.NotNull(updatedCall);
            Assert.Equal(PendingToolCallStatus.Confirmed, updatedCall.Status);

            // 3. Tool response message saved
            var toolMessage = await dbContext.Messages.FirstOrDefaultAsync(m => m.ParentMessageId == assistantMsg.Id);
            Assert.NotNull(toolMessage);
            Assert.Contains("{\"success\": true}", toolMessage.Content);

            // 4. Dispatcher resumed
            await messageDispatcher.Received(1).ResumeAfterToolCallAsync(pendingCall.Id, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RejectToolCallCommand_ShouldMarkRejected_SaveRejectionMessage_AndResumeDispatcher()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var messageDispatcher = Substitute.For<IMessageDispatcherService>();

            var channelId = Guid.NewGuid();
            var assistantMsg = new ChatMessage(
                channelId: channelId,
                senderMemberId: Guid.NewGuid(),
                senderDisplayName: "OrderAgent",
                isAgent: true,
                content: "Yêu cầu gọi công cụ cancel_order",
                callDepth: 1);
            dbContext.Messages.Add(assistantMsg);

            var pendingCall = new PendingToolCall(
                channelId: channelId,
                chatMessageId: assistantMsg.Id,
                toolCallId: "call_abc888",
                toolName: "cancel_order",
                argumentsJson: "{\"orderId\":\"12345\"}",
                expiresAt: DateTimeOffset.UtcNow.AddMinutes(5));
            dbContext.PendingToolCalls.Add(pendingCall);
            await dbContext.SaveChangesAsync();

            var sender = Substitute.For<ISender>();
            var handler = new RejectToolCallCommandHandler(dbContext, sender, messageDispatcher);
            var command = new RejectToolCallCommand(pendingCall.Id, "Hành động này quá nguy hiểm");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);

            // 1. PendingToolCall marked Rejected
            var updatedCall = await dbContext.PendingToolCalls.FindAsync(pendingCall.Id);
            Assert.NotNull(updatedCall);
            Assert.Equal(PendingToolCallStatus.Rejected, updatedCall.Status);

            // 2. Rejection message saved
            var toolMessage = await dbContext.Messages.FirstOrDefaultAsync(m => m.ParentMessageId == assistantMsg.Id);
            Assert.NotNull(toolMessage);
            Assert.Contains("User rejected tool execution", toolMessage.Content);
            Assert.Contains("Hành động này quá nguy hiểm", toolMessage.Content);

            // 3. Dispatcher resumed
            await messageDispatcher.Received(1).ResumeAfterToolCallAsync(pendingCall.Id, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ConfirmToolCallCommand_ShouldFail_WhenToolCallIsNotFoundOrNotPending()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var restApiExecutor = Substitute.For<IRestApiExecutor>();
            var messageDispatcher = Substitute.For<IMessageDispatcherService>();

            var handler = new ConfirmToolCallCommandHandler(dbContext, sender, restApiExecutor, messageDispatcher);

            // Act & Assert 1: Not Found
            var notFoundResult = await handler.Handle(new ConfirmToolCallCommand(Guid.NewGuid()), CancellationToken.None);
            Assert.True(notFoundResult.IsFailure);
            Assert.Equal(ErrorType.NotFound, notFoundResult.Error.Type);

            // Act & Assert 2: Not Pending (already confirmed)
            var call = new PendingToolCall(Guid.NewGuid(), Guid.NewGuid(), "c1", "t1", "{}", DateTimeOffset.UtcNow.AddMinutes(5));
            call.Confirm();
            dbContext.PendingToolCalls.Add(call);
            await dbContext.SaveChangesAsync();

            var conflictResult = await handler.Handle(new ConfirmToolCallCommand(call.Id), CancellationToken.None);
            Assert.True(conflictResult.IsFailure);
            Assert.Equal(ErrorType.Conflict, conflictResult.Error.Type);

            // Act & Assert 3: Expired
            var expiredCall = new PendingToolCall(Guid.NewGuid(), Guid.NewGuid(), "c2", "t2", "{}", DateTimeOffset.UtcNow.AddMinutes(-5));
            dbContext.PendingToolCalls.Add(expiredCall);
            await dbContext.SaveChangesAsync();

            var expiredResult = await handler.Handle(new ConfirmToolCallCommand(expiredCall.Id), CancellationToken.None);
            Assert.True(expiredResult.IsFailure);
        }

        [Fact]
        public async Task ChatController_ConfirmAndRejectEndpoints_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var mediator = Substitute.For<ISender>();
            var controller = new ChatController(mediator)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var callId = Guid.NewGuid();
            mediator.Send(Arg.Is<ConfirmToolCallCommand>(c => c.PendingToolCallId == callId), Arg.Any<CancellationToken>())
                .Returns(Result.Success(true));

            mediator.Send(Arg.Is<RejectToolCallCommand>(c => c.PendingToolCallId == callId), Arg.Any<CancellationToken>())
                .Returns(Result.Success(true));

            // Act 1: Confirm
            var confirmResponse = await controller.ConfirmToolCall(callId, CancellationToken.None);
            var okConfirm = Assert.IsType<OkObjectResult>(confirmResponse);
            var confirmApiRes = Assert.IsType<ApiResponse<bool>>(okConfirm.Value);
            Assert.True(confirmApiRes.Success);
            Assert.True(confirmApiRes.Data);

            // Act 2: Reject
            var rejectResponse = await controller.RejectToolCall(callId, new RejectToolCallRequest("Reason"), CancellationToken.None);
            var okReject = Assert.IsType<OkObjectResult>(rejectResponse);
            var rejectApiRes = Assert.IsType<ApiResponse<bool>>(okReject.Value);
            Assert.True(rejectApiRes.Success);
            Assert.True(rejectApiRes.Data);
        }

        [Fact]
        public async Task DispatchMessageAsync_MaxToolIterationsExceeded_ShouldEmitWarningAndComplete()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var llmFactory = Substitute.For<ILLMProviderFactory>();
            var llmClient = Substitute.For<ILLMClient>();
            var hubContext = Substitute.For<IHubContext<SignalRChatHub>>();
            var clients = Substitute.For<IHubClients>();
            var clientProxy = Substitute.For<IClientProxy>();
            var restApiExecutor = Substitute.For<IRestApiExecutor>();

            hubContext.Clients.Returns(clients);
            clients.Group(Arg.Any<string>()).Returns(clientProxy);

            var channelId = Guid.NewGuid();
            var agentMemberId = Guid.NewGuid();
            var agentProfileId = Guid.NewGuid();

            var memberDto = new MemberDto(
                Id: agentMemberId,
                ChannelId: channelId,
                Type: MemberType.Agent,
                UserId: null,
                AgentProfileId: agentProfileId,
                DisplayName: "LoopBot",
                CreatedDate: DateTimeOffset.UtcNow);

            var profileDto = new AgentProfileDto(
                Id: agentProfileId,
                Name: "LoopBot",
                AvatarUrl: null,
                Provider: ModelProviderType.OpenAI,
                ModelName: "gpt-4o",
                SystemPrompt: "Loop agent.",
                Temperature: 0.2m,
                PrivateMemoryNotes: null,
                CreatedDate: DateTimeOffset.UtcNow,
                ToolExecutionMode: ToolExecutionMode.Auto);

            var sampleTool = CreateSampleTool("ping", requiresConfirmation: false);

            sender.Send(Arg.Is<GetChannelMembersQuery>(q => q.ChannelId == channelId), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<MemberDto>>(new List<MemberDto> { memberDto }));

            sender.Send(Arg.Any<GetAgentProfilesQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<AgentProfileDto>>(new List<AgentProfileDto> { profileDto }));

            sender.Send(Arg.Is<GetAgentToolsQuery>(q => q.AgentProfileId == agentProfileId), Arg.Any<CancellationToken>())
                .Returns(Result.Success(new List<ToolDefinitionDto> { sampleTool }));

            llmFactory.GetClient(Arg.Any<string>()).Returns(llmClient);

            // Always returns a tool call to simulate an infinite loop
            llmClient.ChatWithToolsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<ChatMessagePayload>>(),
                Arg.Any<IEnumerable<LlmToolDefinitionPayload>>(),
                Arg.Any<decimal?>(),
                Arg.Any<CancellationToken>())
                .Returns(new LlmChatResponse(null, new List<LlmToolCallPayload> { new("call_loop", "ping", "{}") }));

            restApiExecutor.ExecuteAsync(Arg.Any<RestApiExecutionRequest>(), Arg.Any<CancellationToken>())
                .Returns(RestApiExecutionResult.Success(200, "pong", 10));

            var dispatcher = new MessageDispatcherService(dbContext, sender, llmFactory, hubContext, restApiExecutor: restApiExecutor);

            var userMessage = new ChatMessageDto(
                Id: Guid.NewGuid(),
                ChannelId: channelId,
                SenderMemberId: Guid.NewGuid(),
                SenderDisplayName: "Human User",
                IsAgent: false,
                Content: "Test loop",
                CallDepth: 0,
                ParentMessageId: null,
                Mentions: new List<Guid> { agentMemberId },
                CreatedDate: DateTimeOffset.UtcNow);

            // Act
            await dispatcher.DispatchMessageAsync(userMessage, new List<Guid> { agentMemberId });

            // Assert
            // RestApiExecutor called exactly 5 times (MaxToolIterations)
            await restApiExecutor.Received(5).ExecuteAsync(Arg.Any<RestApiExecutionRequest>(), Arg.Any<CancellationToken>());

            // Client receives ReceiveMessageCompleted explaining max tool iterations reached
            await clientProxy.Received().SendCoreAsync(
                "ReceiveMessageCompleted",
                Arg.Is<object[]>(args => args.Any(a => a != null && a.ToString()!.Contains("tối đa"))),
                Arg.Any<CancellationToken>());

            // Database saved warning message
            var messages = await dbContext.Messages.ToListAsync();
            Assert.Contains(messages, m => m.Content.Contains("tối đa"));
        }

        [Fact]
        public async Task ConfirmToolCallCommand_ShouldFail_WhenUserIsNotHumanMemberOfChannel()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var restApiExecutor = Substitute.For<IRestApiExecutor>();
            var messageDispatcher = Substitute.For<IMessageDispatcherService>();
            var currentUser = Substitute.For<ICurrentUser>();

            var currentUserId = Guid.NewGuid();
            currentUser.UserId.Returns(currentUserId);

            var channelId = Guid.NewGuid();
            var assistantMsg = new ChatMessage(channelId, Guid.NewGuid(), "Agent", true, "Call tool", 1);
            dbContext.Messages.Add(assistantMsg);

            var pendingCall = new PendingToolCall(channelId, assistantMsg.Id, "call_1", "tool_1", "{}", DateTimeOffset.UtcNow.AddMinutes(5));
            dbContext.PendingToolCalls.Add(pendingCall);
            await dbContext.SaveChangesAsync();

            // Scenario 1: User is not a member of the channel
            sender.Send(Arg.Is<GetMemberByUserIdAndChannelQuery>(q => q.UserId == currentUserId && q.ChannelId == channelId), Arg.Any<CancellationToken>())
                .Returns(Result.Failure<MemberDto>(Error.NotFound("Member.NotFound", "User not in channel")));

            var handler = new ConfirmToolCallCommandHandler(dbContext, sender, restApiExecutor, messageDispatcher, currentUser);

            // Act 1
            var failResult = await handler.Handle(new ConfirmToolCallCommand(pendingCall.Id), CancellationToken.None);

            // Assert 1
            Assert.True(failResult.IsFailure);
            Assert.Equal(ErrorType.Forbidden, failResult.Error.Type);

            // Scenario 2: Member exists but is an Agent, not Human
            var agentMember = new MemberDto(Guid.NewGuid(), channelId, MemberType.Agent, currentUserId, Guid.NewGuid(), "BotMember", DateTimeOffset.UtcNow);
            sender.Send(Arg.Is<GetMemberByUserIdAndChannelQuery>(q => q.UserId == currentUserId && q.ChannelId == channelId), Arg.Any<CancellationToken>())
                .Returns(Result.Success(agentMember));

            // Act 2
            var failAgentResult = await handler.Handle(new ConfirmToolCallCommand(pendingCall.Id), CancellationToken.None);

            // Assert 2
            Assert.True(failAgentResult.IsFailure);
            Assert.Equal(ErrorType.Forbidden, failAgentResult.Error.Type);
        }

        [Fact]
        public async Task RejectToolCallCommand_ShouldFail_WhenUserIsNotHumanMemberOfChannel()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var messageDispatcher = Substitute.For<IMessageDispatcherService>();
            var currentUser = Substitute.For<ICurrentUser>();

            var currentUserId = Guid.NewGuid();
            currentUser.UserId.Returns(currentUserId);

            var channelId = Guid.NewGuid();
            var assistantMsg = new ChatMessage(channelId, Guid.NewGuid(), "Agent", true, "Call tool", 1);
            dbContext.Messages.Add(assistantMsg);

            var pendingCall = new PendingToolCall(channelId, assistantMsg.Id, "call_2", "tool_2", "{}", DateTimeOffset.UtcNow.AddMinutes(5));
            dbContext.PendingToolCalls.Add(pendingCall);
            await dbContext.SaveChangesAsync();

            sender.Send(Arg.Is<GetMemberByUserIdAndChannelQuery>(q => q.UserId == currentUserId && q.ChannelId == channelId), Arg.Any<CancellationToken>())
                .Returns(Result.Failure<MemberDto>(Error.NotFound("Member.NotFound", "User not in channel")));

            var handler = new RejectToolCallCommandHandler(dbContext, sender, messageDispatcher, currentUser);

            // Act
            var result = await handler.Handle(new RejectToolCallCommand(pendingCall.Id), CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        }

        [Fact]
        public async Task ConfirmToolCallCommand_ShouldDispatchRecordToolExecutionCommand()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var restApiExecutor = Substitute.For<IRestApiExecutor>();
            var messageDispatcher = Substitute.For<IMessageDispatcherService>();

            var channelId = Guid.NewGuid();
            var agentMemberId = Guid.NewGuid();
            var agentProfileId = Guid.NewGuid();

            var assistantMsg = new ChatMessage(channelId, agentMemberId, "Agent", true, "Call tool", 1);
            dbContext.Messages.Add(assistantMsg);

            var pendingCall = new PendingToolCall(channelId, assistantMsg.Id, "call_log", "test_tool", "{\"x\":1}", DateTimeOffset.UtcNow.AddMinutes(5));
            dbContext.PendingToolCalls.Add(pendingCall);
            await dbContext.SaveChangesAsync();

            var sampleTool = CreateSampleTool("test_tool", requiresConfirmation: true, url: "https://api.test.com/run");
            sender.Send(Arg.Any<GetToolDefinitionsQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success(new List<ToolDefinitionDto> { sampleTool }));

            var memberDto = new MemberDto(agentMemberId, channelId, MemberType.Agent, null, agentProfileId, "Agent", DateTimeOffset.UtcNow);
            sender.Send(Arg.Is<GetChannelMembersQuery>(q => q.ChannelId == channelId), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<MemberDto>>(new List<MemberDto> { memberDto }));

            restApiExecutor.ExecuteAsync(Arg.Any<RestApiExecutionRequest>(), Arg.Any<CancellationToken>())
                .Returns(RestApiExecutionResult.Success(200, "{\"ok\":true}", 45));

            var handler = new ConfirmToolCallCommandHandler(dbContext, sender, restApiExecutor, messageDispatcher);

            // Act
            var result = await handler.Handle(new ConfirmToolCallCommand(pendingCall.Id), CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            await sender.Received(1).Send(
                Arg.Is<RecordToolExecutionCommand>(cmd =>
                    cmd.ToolDefinitionId == sampleTool.Id &&
                    cmd.CallerType == ToolCallerType.Chat &&
                    cmd.ExecutionSourceId == assistantMsg.Id &&
                    cmd.IsSuccess == true &&
                    cmd.StatusCode == 200),
                Arg.Any<CancellationToken>());
        }
    }
}
