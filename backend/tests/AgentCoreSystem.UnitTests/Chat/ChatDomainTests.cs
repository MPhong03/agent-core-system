// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Chat.Domain.Entities;
using Xunit;

namespace AgentCoreSystem.UnitTests.Chat
{
    public class ChatDomainTests
    {
        [Fact]
        public void ChatMessage_Should_Initialize_With_UUIDv7_And_Default_Values()
        {
            // Arrange
            var channelId = Guid.CreateVersion7();
            var senderMemberId = Guid.CreateVersion7();
            var displayName = "Phong Dang";
            var content = "Hello world";

            // Act
            var message = new ChatMessage(channelId, senderMemberId, displayName, isAgent: false, content);

            // Assert
            Assert.NotEqual(Guid.Empty, message.Id);
            Assert.Equal(7, message.Id.Version);
            Assert.Equal(channelId, message.ChannelId);
            Assert.Equal(senderMemberId, message.SenderMemberId);
            Assert.Equal(displayName, message.SenderDisplayName);
            Assert.False(message.IsAgent);
            Assert.Equal(content, message.Content);
            Assert.Equal(0, message.CallDepth);
            Assert.Null(message.ParentMessageId);
            Assert.NotNull(message.Mentions);
            Assert.Empty(message.Mentions);
            Assert.False(message.IsDeleted);
        }

        [Fact]
        public void ChatMessage_Should_Support_Custom_CallDepth_And_ParentMessageId()
        {
            // Arrange
            var channelId = Guid.CreateVersion7();
            var senderMemberId = Guid.CreateVersion7();
            var parentMessageId = Guid.CreateVersion7();

            // Act
            var message = new ChatMessage(
                channelId,
                senderMemberId,
                "Agent Smith",
                isAgent: true,
                "Responding to task",
                callDepth: 2,
                parentMessageId: parentMessageId);

            // Assert
            Assert.NotEqual(Guid.Empty, message.Id);
            Assert.Equal(7, message.Id.Version);
            Assert.True(message.IsAgent);
            Assert.Equal(2, message.CallDepth);
            Assert.Equal(parentMessageId, message.ParentMessageId);
        }

        [Fact]
        public void ChatMessage_Should_Add_Mention_Successfully()
        {
            // Arrange
            var channelId = Guid.CreateVersion7();
            var senderMemberId = Guid.CreateVersion7();
            var mentionedMemberId = Guid.CreateVersion7();
            var message = new ChatMessage(channelId, senderMemberId, "Phong Dang", isAgent: false, "Hey @Agent!");

            // Act
            var mention = message.AddMention(mentionedMemberId);

            // Assert
            Assert.NotEqual(Guid.Empty, mention.Id);
            Assert.Equal(7, mention.Id.Version);
            Assert.Equal(message.Id, mention.MessageId);
            Assert.Equal(mentionedMemberId, mention.MentionedMemberId);
            Assert.Contains(mention, message.Mentions);
            Assert.Single(message.Mentions);
        }

        [Fact]
        public void MessageMention_Should_Initialize_With_UUIDv7_And_Properties()
        {
            // Arrange
            var messageId = Guid.CreateVersion7();
            var mentionedMemberId = Guid.CreateVersion7();

            // Act
            var mention = new MessageMention(messageId, mentionedMemberId);

            // Assert
            Assert.NotEqual(Guid.Empty, mention.Id);
            Assert.Equal(7, mention.Id.Version);
            Assert.Equal(messageId, mention.MessageId);
            Assert.Equal(mentionedMemberId, mention.MentionedMemberId);
            Assert.False(mention.IsDeleted);
        }
    }
}
