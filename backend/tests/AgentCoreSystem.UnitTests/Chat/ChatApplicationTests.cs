// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Chat.Application;
using AgentCoreSystem.Module.Chat.Application.Messages.Commands;
using AgentCoreSystem.Module.Chat.Application.Messages.DTOs;
using AgentCoreSystem.Module.Chat.Application.Messages.Queries;
using AgentCoreSystem.Module.Chat.Infrastructure.Persistence;
using AgentCoreSystem.Module.Organization.Application.Members.Queries;
using AgentCoreSystem.Module.Organization.Domain.Entities;
using AgentCoreSystem.Module.Organization.Infrastructure.Persistence;
using AgentCoreSystem.Shared.Application;
using AgentCoreSystem.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AgentCoreSystem.UnitTests.Chat
{
    public class ChatApplicationTests
    {
        [Fact]
        public async Task GetMemberByUserIdAndChannel_ShouldReturnMember_WhenExists()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<OrganizationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var channelId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var member = Member.CreateHuman(channelId, userId, "Nguyen Van A");

            await using (var context = new OrganizationDbContext(options))
            {
                context.Members.Add(member);
                await context.SaveChangesAsync();
            }

            await using var queryContext = new OrganizationDbContext(options);
            var handler = new GetMemberByUserIdAndChannelQueryHandler(queryContext);
            var query = new GetMemberByUserIdAndChannelQuery(userId, channelId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(member.Id, result.Value.Id);
            Assert.Equal(channelId, result.Value.ChannelId);
            Assert.Equal(userId, result.Value.UserId);
            Assert.Equal("Nguyen Van A", result.Value.DisplayName);
        }

        [Fact]
        public async Task GetMemberByUserIdAndChannel_ShouldReturnNotFound_WhenMemberDoesNotExist()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<OrganizationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new OrganizationDbContext(options);
            var handler = new GetMemberByUserIdAndChannelQueryHandler(context);
            var query = new GetMemberByUserIdAndChannelQuery(Guid.NewGuid(), Guid.NewGuid());

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Member.NotFound", result.Error.Code);
        }

        [Fact]
        public async Task SaveChatMessageCommand_ShouldPersistMessageAndMentions()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var channelId = Guid.NewGuid();
            var senderId = Guid.NewGuid();
            var agent1 = Guid.NewGuid();
            var agent2 = Guid.NewGuid();
            var mentions = new List<Guid> { agent1, agent2 };

            await using var context = new ChatDbContext(options);
            var handler = new SaveChatMessageCommandHandler(context);
            var command = new SaveChatMessageCommand(
                ChannelId: channelId,
                SenderMemberId: senderId,
                SenderDisplayName: "Human User",
                IsAgent: false,
                Content: "Xin chào @Agent1 và @Agent2",
                CallDepth: 1,
                ParentMessageId: null,
                MentionIds: mentions);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("Human User", result.Value.SenderDisplayName);
            Assert.Equal(channelId, result.Value.ChannelId);
            Assert.Equal(1, result.Value.CallDepth);
            Assert.Equal(2, result.Value.Mentions.Count);
            Assert.Contains(agent1, result.Value.Mentions);
            Assert.Contains(agent2, result.Value.Mentions);

            // Verify database
            await using var verifyContext = new ChatDbContext(options);
            var savedMessage = await verifyContext.Messages
                .Include(m => m.Mentions)
                .FirstOrDefaultAsync(m => m.Id == result.Value.Id);

            Assert.NotNull(savedMessage);
            Assert.Equal(2, savedMessage.Mentions.Count);
        }

        [Fact]
        public async Task GetChannelMessagesQuery_ShouldReturnMessagesChronologicallyAscending()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var channelId = Guid.NewGuid();
            var senderId = Guid.NewGuid();

            await using var context = new ChatDbContext(options);
            var handler = new SaveChatMessageCommandHandler(context);

            var cmd1 = new SaveChatMessageCommand(channelId, senderId, "User", false, "Tin nhắn 1", 0, null, null);
            var cmd2 = new SaveChatMessageCommand(channelId, senderId, "User", false, "Tin nhắn 2", 0, null, null);
            var cmd3 = new SaveChatMessageCommand(channelId, senderId, "User", false, "Tin nhắn 3", 0, null, null);

            var msg1 = (await handler.Handle(cmd1, CancellationToken.None)).Value;
            await Task.Delay(10);
            var msg2 = (await handler.Handle(cmd2, CancellationToken.None)).Value;
            await Task.Delay(10);
            var msg3 = (await handler.Handle(cmd3, CancellationToken.None)).Value;

            // Act
            var queryHandler = new GetChannelMessagesQueryHandler(context);
            var query = new GetChannelMessagesQuery(channelId, Limit: 50);
            var result = await queryHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Value.Count);
            Assert.Equal(msg1.Id, result.Value[0].Id);
            Assert.Equal(msg2.Id, result.Value[1].Id);
            Assert.Equal(msg3.Id, result.Value[2].Id);
            Assert.True(result.Value[0].CreatedDate <= result.Value[1].CreatedDate);
            Assert.True(result.Value[1].CreatedDate <= result.Value[2].CreatedDate);
        }

        [Fact]
        public async Task GetChannelMessagesQuery_WithBeforeMessageId_ShouldReturnMessagesBeforeMarker()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var channelId = Guid.NewGuid();
            var senderId = Guid.NewGuid();

            await using var context = new ChatDbContext(options);
            var handler = new SaveChatMessageCommandHandler(context);

            var msg1 = (await handler.Handle(new SaveChatMessageCommand(channelId, senderId, "User", false, "M1", 0, null, null), CancellationToken.None)).Value;
            await Task.Delay(15);
            var msg2 = (await handler.Handle(new SaveChatMessageCommand(channelId, senderId, "User", false, "M2", 0, null, null), CancellationToken.None)).Value;
            await Task.Delay(15);
            var msg3 = (await handler.Handle(new SaveChatMessageCommand(channelId, senderId, "User", false, "M3", 0, null, null), CancellationToken.None)).Value;

            // Act: query messages before msg3
            var queryHandler = new GetChannelMessagesQueryHandler(context);
            var query = new GetChannelMessagesQuery(channelId, Limit: 50, BeforeMessageId: msg3.Id);
            var result = await queryHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.Equal(msg1.Id, result.Value[0].Id);
            Assert.Equal(msg2.Id, result.Value[1].Id);
            Assert.DoesNotContain(result.Value, m => m.Id == msg3.Id);
        }

        [Fact]
        public async Task GetChannelMessagesQuery_WithNonExistentBeforeMessageId_ShouldReturnNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ChatDbContext(options);
            var queryHandler = new GetChannelMessagesQueryHandler(context);
            var query = new GetChannelMessagesQuery(Guid.NewGuid(), Limit: 50, BeforeMessageId: Guid.NewGuid());

            // Act
            var result = await queryHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("ChatMessage.NotFound", result.Error.Code);
        }

        [Fact]
        public void SaveChatMessageCommandValidator_ShouldFail_WhenContentIsEmpty()
        {
            // Arrange
            var validator = new SaveChatMessageCommandValidator();
            var command = new SaveChatMessageCommand(
                ChannelId: Guid.NewGuid(),
                SenderMemberId: Guid.NewGuid(),
                SenderDisplayName: "User",
                IsAgent: false,
                Content: string.Empty);

            // Act
            var validationResult = validator.Validate(command);

            // Assert
            Assert.False(validationResult.IsValid);
            Assert.Contains(validationResult.Errors, e => e.PropertyName == nameof(SaveChatMessageCommand.Content));
        }

        [Fact]
        public void AddChatApplication_ShouldRegisterMediatRHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSharedPersistence();
            services.AddSharedApplication();
            services.AddChatApplication();

            // Mock IChatDbContext
            services.AddScoped<AgentCoreSystem.Module.Chat.Application.Contracts.IChatDbContext>(_ => 
            {
                var options = new DbContextOptionsBuilder<ChatDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;
                return new ChatDbContext(options);
            });

            var provider = services.BuildServiceProvider();

            // Act & Assert
            using var scope = provider.CreateScope();
            var mediator = scope.ServiceProvider.GetService<IMediator>();
            Assert.NotNull(mediator);

            var saveHandler = scope.ServiceProvider.GetService<IRequestHandler<SaveChatMessageCommand, AgentCoreSystem.Shared.Kernel.Primitives.Result<ChatMessageDto>>>();
            Assert.NotNull(saveHandler);

            var getHandler = scope.ServiceProvider.GetService<IRequestHandler<GetChannelMessagesQuery, AgentCoreSystem.Shared.Kernel.Primitives.Result<IReadOnlyList<ChatMessageDto>>>>();
            Assert.NotNull(getHandler);
        }
    }
}
