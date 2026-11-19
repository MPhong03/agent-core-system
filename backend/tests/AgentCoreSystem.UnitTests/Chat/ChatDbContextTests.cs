// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Chat.Application.Contracts;
using AgentCoreSystem.Module.Chat.Domain.Entities;
using AgentCoreSystem.Module.Chat.Infrastructure;
using AgentCoreSystem.Module.Chat.Infrastructure.Persistence;
using AgentCoreSystem.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AgentCoreSystem.UnitTests.Chat
{
    public class ChatDbContextTests
    {
        [Fact]
        public void ChatDbContext_ShouldDefineCorrectSchemaConstant()
        {
            // Assert
            Assert.Equal("chat", ChatDbContext.SchemaName);
        }

        [Fact]
        public async Task ChatDbContext_ShouldPersistChatMessageAndMentions_WithInMemory()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var channelId = Guid.NewGuid();
            var senderId = Guid.NewGuid();
            var agentId = Guid.NewGuid();

            await using var context = new ChatDbContext(options);

            var message = new ChatMessage(
                channelId: channelId,
                senderMemberId: senderId,
                senderDisplayName: "Human User",
                isAgent: false,
                content: "Xin chào @SalesAgent, hãy phân tích xu hướng!",
                callDepth: 0);

            message.AddMention(agentId);

            context.Messages.Add(message);
            await context.SaveChangesAsync();

            // Act
            await using var queryContext = new ChatDbContext(options);
            var savedMessage = await queryContext.Messages
                .Include(m => m.Mentions)
                .FirstOrDefaultAsync(m => m.Id == message.Id);

            // Assert
            Assert.NotNull(savedMessage);
            Assert.Equal("Human User", savedMessage.SenderDisplayName);
            Assert.Equal(channelId, savedMessage.ChannelId);
            Assert.Single(savedMessage.Mentions);
            Assert.Equal(agentId, savedMessage.Mentions.First().MentionedMemberId);
        }

        [Fact]
        public async Task ChatDbContext_SoftDelete_ShouldFilterOutDeletedMessages()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            await using var context = new ChatDbContext(options);
            var message = new ChatMessage(
                channelId: Guid.NewGuid(),
                senderMemberId: Guid.NewGuid(),
                senderDisplayName: "User",
                isAgent: false,
                content: "Tin nhắn sắp bị xóa");

            message.IsDeleted = true;
            message.DeletedDate = DateTimeOffset.UtcNow;
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            // Act
            await using var queryContext = new ChatDbContext(options);
            var activeMessages = await queryContext.Messages.ToListAsync();
            var allMessages = await queryContext.Messages.IgnoreQueryFilters().ToListAsync();

            // Assert
            Assert.Empty(activeMessages);
            Assert.Single(allMessages);
        }

        [Fact]
        public void AddChatInfrastructure_ShouldRegisterDbContextAndInterface()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSharedPersistence();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string?>(
                        "ConnectionStrings:DefaultConnection",
                        "Server=localhost;Database=TestDb;Trusted_Connection=True;")
                })
                .Build();

            // Act
            services.AddChatInfrastructure(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<ChatDbContext>();
            var interfaceContext = scope.ServiceProvider.GetService<IChatDbContext>();

            Assert.NotNull(dbContext);
            Assert.NotNull(interfaceContext);
            Assert.Same(dbContext, interfaceContext);
        }
    }
}
