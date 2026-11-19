// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Chat.Application.Contracts;
using AgentCoreSystem.Module.Chat.Application.Messages.Commands;
using AgentCoreSystem.Module.Chat.Application.Messages.DTOs;
using AgentCoreSystem.Module.Chat.Domain.Entities;
using AgentCoreSystem.Module.Chat.Infrastructure.Hubs;
using AgentCoreSystem.Module.Chat.Infrastructure.Persistence;
using AgentCoreSystem.Module.Chat.Infrastructure.Services;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.DTOs;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.Queries.GetAgentProfiles;
using AgentCoreSystem.Module.Organization.Application.Members.DTOs;
using AgentCoreSystem.Module.Organization.Application.Members.Queries.GetChannelMembers;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Shared.Infrastructure.LLM;
using AgentCoreSystem.Shared.Kernel.Primitives;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests.Chat
{
    public class MessageDispatcherServiceTests
    {
        private static async IAsyncEnumerable<string> ToAsyncEnumerable(IEnumerable<string> items)
        {
            foreach (var item in items)
            {
                yield return item;
            }
            await Task.CompletedTask;
        }

        private static ChatDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ChatDbContext(options);
        }

        [Fact]
        public async Task DispatchMessageAsync_ShouldStopImmediately_WhenCallDepthIsThreeOrGreater()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var llmFactory = Substitute.For<ILLMProviderFactory>();
            var hubContext = Substitute.For<IHubContext<SignalRChatHub>>();

            var dispatcher = new MessageDispatcherService(dbContext, sender, llmFactory, hubContext);

            var userMessage = new ChatMessageDto(
                Id: Guid.NewGuid(),
                ChannelId: Guid.NewGuid(),
                SenderMemberId: Guid.NewGuid(),
                SenderDisplayName: "Human User",
                IsAgent: false,
                Content: "Hello @Agent",
                CallDepth: 3,
                ParentMessageId: null,
                Mentions: new List<Guid> { Guid.NewGuid() },
                CreatedDate: DateTimeOffset.UtcNow);

            // Act
            await dispatcher.DispatchMessageAsync(userMessage, new List<Guid> { Guid.NewGuid() });

            // Assert
            llmFactory.DidNotReceiveWithAnyArgs().GetClient(Arg.Any<string>());
            await sender.DidNotReceiveWithAnyArgs().Send(Arg.Any<IRequest<Result<ChatMessageDto>>>(), Arg.Any<CancellationToken>());
            hubContext.Clients.DidNotReceive();
        }

        [Fact]
        public async Task DispatchMessageAsync_ShouldStreamAndPersistReply_WhenSingleAgentMentioned()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var llmFactory = Substitute.For<ILLMProviderFactory>();
            var llmClient = Substitute.For<ILLMClient>();
            var hubContext = Substitute.For<IHubContext<SignalRChatHub>>();
            var clients = Substitute.For<IHubClients>();
            var clientProxy = Substitute.For<IClientProxy>();

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
                DisplayName: "Code Reviewer Bot",
                CreatedDate: DateTimeOffset.UtcNow.AddHours(-1));

            var profileDto = new AgentProfileDto(
                Id: agentProfileId,
                Name: "Code Reviewer Bot",
                AvatarUrl: null,
                Provider: ModelProviderType.OpenAI,
                ModelName: "gpt-4o",
                SystemPrompt: "You are a code review expert.",
                Temperature: 0.2m,
                PrivateMemoryNotes: null,
                CreatedDate: DateTimeOffset.UtcNow.AddHours(-2));

            sender.Send(Arg.Is<GetChannelMembersQuery>(q => q.ChannelId == channelId), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<MemberDto>>(new List<MemberDto> { memberDto }));

            sender.Send(Arg.Any<GetAgentProfilesQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<AgentProfileDto>>(new List<AgentProfileDto> { profileDto }));

            llmFactory.GetClient("OpenAI").Returns(llmClient);

            var streamChunks = new[] { "Code looks ", "great and ", "clean!" };
            llmClient.StreamChatAsync(
                model: "gpt-4o",
                messages: Arg.Any<IEnumerable<ChatMessagePayload>>(),
                temperature: 0.2m,
                ct: Arg.Any<CancellationToken>())
                .Returns(ToAsyncEnumerable(streamChunks));

            var userMessage = new ChatMessageDto(
                Id: Guid.NewGuid(),
                ChannelId: channelId,
                SenderMemberId: Guid.NewGuid(),
                SenderDisplayName: "Developer",
                IsAgent: false,
                Content: "Can you review this code?",
                CallDepth: 0,
                ParentMessageId: null,
                Mentions: new List<Guid> { agentMemberId },
                CreatedDate: DateTimeOffset.UtcNow);

            // Add user message to dbContext to mimic real behavior
            var dbUserMsg = new ChatMessage(
                channelId,
                userMessage.SenderMemberId,
                userMessage.SenderDisplayName,
                userMessage.IsAgent,
                userMessage.Content,
                userMessage.CallDepth,
                userMessage.ParentMessageId,
                userMessage.Id);
            dbContext.Messages.Add(dbUserMsg);
            await dbContext.SaveChangesAsync();

            ChatMessageDto? savedAgentReply = null;
            sender.Send(Arg.Any<SaveChatMessageCommand>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var cmd = callInfo.Arg<SaveChatMessageCommand>();
                    savedAgentReply = new ChatMessageDto(
                        Id: cmd.MessageId ?? Guid.NewGuid(),
                        ChannelId: cmd.ChannelId,
                        SenderMemberId: cmd.SenderMemberId,
                        SenderDisplayName: cmd.SenderDisplayName,
                        IsAgent: cmd.IsAgent,
                        Content: cmd.Content,
                        CallDepth: cmd.CallDepth,
                        ParentMessageId: cmd.ParentMessageId,
                        Mentions: new List<Guid>(),
                        CreatedDate: DateTimeOffset.UtcNow);
                    return Result.Success(savedAgentReply);
                });

            var dispatcher = new MessageDispatcherService(dbContext, sender, llmFactory, hubContext);

            // Act
            await dispatcher.DispatchMessageAsync(userMessage, new List<Guid> { agentMemberId });

            // Assert
            // 1. Group resolution (3 chunks + 1 completed = 4 calls to Group)
            clients.Received(4).Group($"channel_{channelId}");

            // 2. Chunks broadcasted
            await clientProxy.Received(3).SendCoreAsync(
                "ReceiveMessageChunk",
                Arg.Any<object[]>(),
                Arg.Any<CancellationToken>());

            // 3. Saved with CallDepth = 1, ParentMessageId = userMessage.Id
            await sender.Received(1).Send(
                Arg.Is<SaveChatMessageCommand>(cmd =>
                    cmd.ChannelId == channelId &&
                    cmd.SenderMemberId == agentMemberId &&
                    cmd.SenderDisplayName == "Code Reviewer Bot" &&
                    cmd.IsAgent == true &&
                    cmd.CallDepth == 1 &&
                    cmd.ParentMessageId == userMessage.Id &&
                    cmd.Content == "Code looks great and clean!"),
                Arg.Any<CancellationToken>());

            // 4. Completed broadcasted
            await clientProxy.Received(1).SendCoreAsync(
                "ReceiveMessageCompleted",
                Arg.Is<object[]>(args =>
                    args.Length == 3 &&
                    (Guid)args[1] == agentMemberId &&
                    (string)args[2] == "Code looks great and clean!"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DispatchMessageAsync_ShouldProcessMultipleAgentsSequentially()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var llmFactory = Substitute.For<ILLMProviderFactory>();
            var llmClient1 = Substitute.For<ILLMClient>();
            var llmClient2 = Substitute.For<ILLMClient>();
            var hubContext = Substitute.For<IHubContext<SignalRChatHub>>();
            var clients = Substitute.For<IHubClients>();
            var clientProxy = Substitute.For<IClientProxy>();

            hubContext.Clients.Returns(clients);
            clients.Group(Arg.Any<string>()).Returns(clientProxy);

            var channelId = Guid.NewGuid();
            var agent1MemberId = Guid.NewGuid();
            var agent1ProfileId = Guid.NewGuid();
            var agent2MemberId = Guid.NewGuid();
            var agent2ProfileId = Guid.NewGuid();

            var member1 = new MemberDto(agent1MemberId, channelId, MemberType.Agent, null, agent1ProfileId, "Agent 1", DateTimeOffset.UtcNow);
            var member2 = new MemberDto(agent2MemberId, channelId, MemberType.Agent, null, agent2ProfileId, "Agent 2", DateTimeOffset.UtcNow);

            var profile1 = new AgentProfileDto(agent1ProfileId, "Agent 1", null, ModelProviderType.OpenAI, "gpt-4o", "Prompt 1", 0.1m, null, DateTimeOffset.UtcNow);
            var profile2 = new AgentProfileDto(agent2ProfileId, "Agent 2", null, ModelProviderType.Claude, "claude-3-7-sonnet", "Prompt 2", 0.5m, null, DateTimeOffset.UtcNow);

            sender.Send(Arg.Is<GetChannelMembersQuery>(q => q.ChannelId == channelId), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<MemberDto>>(new List<MemberDto> { member1, member2 }));

            sender.Send(Arg.Any<GetAgentProfilesQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<AgentProfileDto>>(new List<AgentProfileDto> { profile1, profile2 }));

            llmFactory.GetClient("OpenAI").Returns(llmClient1);
            llmFactory.GetClient("Claude").Returns(llmClient2);

            var orderLog = new List<string>();

            llmClient1.StreamChatAsync(Arg.Any<string>(), Arg.Any<IEnumerable<ChatMessagePayload>>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    orderLog.Add("Agent1_StreamStart");
                    return ToAsyncEnumerable(new[] { "Agent 1 reply" });
                });

            llmClient2.StreamChatAsync(Arg.Any<string>(), Arg.Any<IEnumerable<ChatMessagePayload>>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    orderLog.Add("Agent2_StreamStart");
                    return ToAsyncEnumerable(new[] { "Agent 2 reply" });
                });

            sender.Send(Arg.Any<SaveChatMessageCommand>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var cmd = call.Arg<SaveChatMessageCommand>();
                    orderLog.Add($"{cmd.SenderDisplayName}_Saved");
                    return Result.Success(new ChatMessageDto(
                        Id: Guid.NewGuid(),
                        ChannelId: cmd.ChannelId,
                        SenderMemberId: cmd.SenderMemberId,
                        SenderDisplayName: cmd.SenderDisplayName,
                        IsAgent: cmd.IsAgent,
                        Content: cmd.Content,
                        CallDepth: cmd.CallDepth,
                        ParentMessageId: cmd.ParentMessageId,
                        Mentions: new List<Guid>(),
                        CreatedDate: DateTimeOffset.UtcNow));
                });

            var userMessage = new ChatMessageDto(
                Id: Guid.NewGuid(),
                ChannelId: channelId,
                SenderMemberId: Guid.NewGuid(),
                SenderDisplayName: "User",
                IsAgent: false,
                Content: "Hello @Agent1 and @Agent2",
                CallDepth: 0,
                ParentMessageId: null,
                Mentions: new List<Guid> { agent1MemberId, agent2MemberId },
                CreatedDate: DateTimeOffset.UtcNow);

            var dispatcher = new MessageDispatcherService(dbContext, sender, llmFactory, hubContext);

            // Act
            await dispatcher.DispatchMessageAsync(userMessage, new List<Guid> { agent1MemberId, agent2MemberId });

            // Assert
            Assert.Equal(4, orderLog.Count);
            Assert.Equal("Agent1_StreamStart", orderLog[0]);
            Assert.Equal("Agent 1_Saved", orderLog[1]);
            Assert.Equal("Agent2_StreamStart", orderLog[2]);
            Assert.Equal("Agent 2_Saved", orderLog[3]);
        }

        [Fact]
        public async Task DispatchMessageAsync_ShouldLimitHistoryToLast20Messages_SlidingWindow()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var llmFactory = Substitute.For<ILLMProviderFactory>();
            var llmClient = Substitute.For<ILLMClient>();
            var hubContext = Substitute.For<IHubContext<SignalRChatHub>>();
            var clients = Substitute.For<IHubClients>();
            var clientProxy = Substitute.For<IClientProxy>();

            hubContext.Clients.Returns(clients);
            clients.Group(Arg.Any<string>()).Returns(clientProxy);

            var channelId = Guid.NewGuid();
            var agentMemberId = Guid.NewGuid();
            var agentProfileId = Guid.NewGuid();

            // Seed 25 messages in the channel
            var baseTime = DateTimeOffset.UtcNow.AddMinutes(-50);
            for (int i = 1; i <= 25; i++)
            {
                var msg = new ChatMessage(
                    channelId: channelId,
                    senderMemberId: Guid.NewGuid(),
                    senderDisplayName: $"User{i}",
                    isAgent: false,
                    content: $"Message {i}",
                    callDepth: 0);

                // Set distinct created dates
                msg.CreatedDate = baseTime.AddMinutes(i);
                dbContext.Messages.Add(msg);
            }
            await dbContext.SaveChangesAsync();

            var member = new MemberDto(agentMemberId, channelId, MemberType.Agent, null, agentProfileId, "Agent", DateTimeOffset.UtcNow);
            var profile = new AgentProfileDto(agentProfileId, "Agent", null, ModelProviderType.OpenAI, "gpt-4o", "System Prompt", 0.3m, null, DateTimeOffset.UtcNow);

            sender.Send(Arg.Is<GetChannelMembersQuery>(q => q.ChannelId == channelId), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<MemberDto>>(new List<MemberDto> { member }));

            sender.Send(Arg.Any<GetAgentProfilesQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<AgentProfileDto>>(new List<AgentProfileDto> { profile }));

            llmFactory.GetClient(Arg.Any<string>()).Returns(llmClient);

            IEnumerable<ChatMessagePayload>? capturedPayload = null;
            llmClient.StreamChatAsync(
                model: Arg.Any<string>(),
                messages: Arg.Do<IEnumerable<ChatMessagePayload>>(p => capturedPayload = p.ToList()),
                temperature: Arg.Any<decimal?>(),
                ct: Arg.Any<CancellationToken>())
                .Returns(ToAsyncEnumerable(new[] { "OK" }));

            sender.Send(Arg.Any<SaveChatMessageCommand>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success(new ChatMessageDto(
                    Guid.NewGuid(), channelId, agentMemberId, "Agent", true, "OK", 1, null, new List<Guid>(), DateTimeOffset.UtcNow)));

            var userMessage = new ChatMessageDto(
                Id: Guid.NewGuid(),
                ChannelId: channelId,
                SenderMemberId: Guid.NewGuid(),
                SenderDisplayName: "User",
                IsAgent: false,
                Content: "Test sliding window",
                CallDepth: 0,
                ParentMessageId: null,
                Mentions: new List<Guid> { agentMemberId },
                CreatedDate: DateTimeOffset.UtcNow);

            var dispatcher = new MessageDispatcherService(dbContext, sender, llmFactory, hubContext);

            // Act
            await dispatcher.DispatchMessageAsync(userMessage, new List<Guid> { agentMemberId });

            // Assert
            Assert.NotNull(capturedPayload);
            var payloadList = capturedPayload.ToList();

            // 1 system message + 20 history messages = 21 items
            Assert.Equal(21, payloadList.Count);
            Assert.Equal("system", payloadList[0].Role);
            Assert.Equal("System Prompt", payloadList[0].Content);

            // First message in sliding window must be Message 6 (messages 1..5 skipped as oldest)
            Assert.Equal("Message 6", payloadList[1].Content);
            Assert.Equal("Message 25", payloadList[20].Content);
        }

        [Fact]
        public async Task DispatchMessageAsync_ShouldIgnoreNonAgentOrMissingMembers()
        {
            // Arrange
            await using var dbContext = CreateInMemoryDbContext();
            var sender = Substitute.For<ISender>();
            var llmFactory = Substitute.For<ILLMProviderFactory>();
            var hubContext = Substitute.For<IHubContext<SignalRChatHub>>();

            var channelId = Guid.NewGuid();
            var humanMemberId = Guid.NewGuid();
            var nonExistentMemberId = Guid.NewGuid();

            var humanMember = new MemberDto(
                Id: humanMemberId,
                ChannelId: channelId,
                Type: MemberType.Human,
                UserId: Guid.NewGuid(),
                AgentProfileId: null,
                DisplayName: "Human User",
                CreatedDate: DateTimeOffset.UtcNow);

            sender.Send(Arg.Is<GetChannelMembersQuery>(q => q.ChannelId == channelId), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<MemberDto>>(new List<MemberDto> { humanMember }));

            sender.Send(Arg.Any<GetAgentProfilesQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<AgentProfileDto>>(new List<AgentProfileDto>()));

            var dispatcher = new MessageDispatcherService(dbContext, sender, llmFactory, hubContext);

            var userMessage = new ChatMessageDto(
                Id: Guid.NewGuid(),
                ChannelId: channelId,
                SenderMemberId: humanMemberId,
                SenderDisplayName: "Human User",
                IsAgent: false,
                Content: "Hi @Human",
                CallDepth: 0,
                ParentMessageId: null,
                Mentions: new List<Guid> { humanMemberId, nonExistentMemberId },
                CreatedDate: DateTimeOffset.UtcNow);

            // Act
            await dispatcher.DispatchMessageAsync(userMessage, new List<Guid> { humanMemberId, nonExistentMemberId });

            // Assert
            llmFactory.DidNotReceiveWithAnyArgs().GetClient(Arg.Any<string>());
            await sender.DidNotReceive().Send(Arg.Any<SaveChatMessageCommand>(), Arg.Any<CancellationToken>());
        }

        private static IServiceScopeFactory CreateMockScopeFactory(IMessageDispatcherService? dispatcher = null)
        {
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            var scope = Substitute.For<IServiceScope>();
            var serviceProvider = Substitute.For<IServiceProvider>();

            scopeFactory.CreateScope().Returns(scope);
            scope.ServiceProvider.Returns(serviceProvider);

            if (dispatcher != null)
            {
                serviceProvider.GetService(typeof(IMessageDispatcherService)).Returns(dispatcher);
            }

            return scopeFactory;
        }

        [Fact]
        public void SignalRChatHub_ShouldHaveAuthorizeAttribute()
        {
            var attributes = typeof(SignalRChatHub)
                .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: false);
            Assert.NotEmpty(attributes);
        }

        [Fact]
        public async Task SignalRChatHub_JoinAndLeaveChannel_ShouldCallGroupManager()
        {
            // Arrange
            var sender = Substitute.For<ISender>();
            var scopeFactory = CreateMockScopeFactory();
            var hub = new SignalRChatHub(sender, scopeFactory);

            var context = Substitute.For<HubCallerContext>();
            context.ConnectionId.Returns("conn-123");
            var groups = Substitute.For<IGroupManager>();

            hub.Context = context;
            hub.Groups = groups;

            var channelId = Guid.NewGuid();

            // Act
            await hub.JoinChannel(channelId);
            await hub.LeaveChannel(channelId);

            // Assert
            await groups.Received(1).AddToGroupAsync("conn-123", $"channel_{channelId}", Arg.Any<CancellationToken>());
            await groups.Received(1).RemoveFromGroupAsync("conn-123", $"channel_{channelId}", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SignalRChatHub_SendMessage_ShouldThrow_WhenUnauthenticated()
        {
            // Arrange
            var sender = Substitute.For<ISender>();
            var scopeFactory = CreateMockScopeFactory();
            var hub = new SignalRChatHub(sender, scopeFactory);

            var context = Substitute.For<HubCallerContext>();
            context.UserIdentifier.Returns((string?)null);
            context.User.Returns((System.Security.Claims.ClaimsPrincipal?)null);
            hub.Context = context;

            // Act & Assert
            await Assert.ThrowsAsync<HubException>(() =>
                hub.SendMessage(Guid.NewGuid(), "Hello world", null));
        }

        [Fact]
        public async Task SignalRChatHub_SendMessage_ShouldSaveAndBroadcast_WhenValidMember()
        {
            // Arrange
            var sender = Substitute.For<ISender>();
            var dispatcher = Substitute.For<IMessageDispatcherService>();
            var scopeFactory = CreateMockScopeFactory(dispatcher);
            var hub = new SignalRChatHub(sender, scopeFactory);

            var userId = Guid.NewGuid();
            var channelId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var context = Substitute.For<HubCallerContext>();
            context.UserIdentifier.Returns(userId.ToString());
            hub.Context = context;

            var clients = Substitute.For<IHubCallerClients>();
            var clientProxy = Substitute.For<IClientProxy>();
            clients.Group($"channel_{channelId}").Returns(clientProxy);
            hub.Clients = clients;

            var memberDto = new MemberDto(memberId, channelId, MemberType.Human, userId, null, "User", DateTimeOffset.UtcNow);
            sender.Send(Arg.Any<AgentCoreSystem.Module.Organization.Application.Members.Queries.GetMemberByUserIdAndChannelQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success(memberDto));

            var savedMessage = new ChatMessageDto(
                Guid.NewGuid(), channelId, memberId, "User", false, "Hello", 0, null, new List<Guid>(), DateTimeOffset.UtcNow);
            sender.Send(Arg.Any<SaveChatMessageCommand>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success(savedMessage));

            // Act
            await hub.SendMessage(channelId, "Hello", null);

            // Assert
            await clientProxy.Received(1).SendCoreAsync(
                "ReceiveMessage",
                Arg.Is<object[]>(args => args.Length == 1 && ((ChatMessageDto)args[0]).Id == savedMessage.Id),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public void AddChatInfrastructure_ShouldRegisterServices()
        {
            // Arrange
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddLogging();
            var configData = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;"
            };
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            services.AddSingleton(new AgentCoreSystem.Shared.Persistence.Interceptors.AuditableEntityInterceptor());
            services.AddSingleton(Substitute.For<ISender>());
            services.AddSingleton(Substitute.For<ILLMProviderFactory>());

            // Act
            AgentCoreSystem.Module.Chat.Infrastructure.DependencyInjection.AddChatInfrastructure(services, config);

            // Assert
            var provider = services.BuildServiceProvider();
            var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetService<AgentCoreSystem.Module.Chat.Application.Contracts.IChatDbContext>();
            var dispatcher = scope.ServiceProvider.GetService<AgentCoreSystem.Module.Chat.Application.Contracts.IMessageDispatcherService>();

            Assert.NotNull(db);
            Assert.NotNull(dispatcher);
        }
    }
}
