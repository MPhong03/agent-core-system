// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Organization.Domain.Entities;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using Xunit;

namespace AgentCoreSystem.UnitTests.Organization
{
    public class OrganizationDomainTests
    {
        [Fact]
        public void Department_Should_Initialize_With_UUIDv7_And_Properties()
        {
            // Act
            var department = new Department("Engineering", "R&D Department");

            // Assert
            Assert.NotEqual(Guid.Empty, department.Id);
            Assert.Equal(7, department.Id.Version);
            Assert.Equal("Engineering", department.Name);
            Assert.Equal("R&D Department", department.Description);
            Assert.False(department.IsDeleted);
            Assert.NotNull(department.Channels);
            Assert.Empty(department.Channels);
        }

        [Fact]
        public void TeamChannel_Should_Initialize_With_UUIDv7_And_Properties()
        {
            // Arrange
            var deptId = Guid.CreateVersion7();

            // Act
            var channel = new TeamChannel(deptId, "tech-research", "Research new tech stacks");

            // Assert
            Assert.NotEqual(Guid.Empty, channel.Id);
            Assert.Equal(7, channel.Id.Version);
            Assert.Equal(deptId, channel.DepartmentId);
            Assert.Equal("tech-research", channel.Name);
            Assert.Equal("Research new tech stacks", channel.Description);
            Assert.False(channel.IsDeleted);
            Assert.NotNull(channel.Members);
            Assert.Empty(channel.Members);
        }

        [Fact]
        public void Member_Should_Support_Human_And_Agent_Types()
        {
            // Arrange
            var channelId = Guid.CreateVersion7();
            var userId = Guid.CreateVersion7();
            var agentProfileId = Guid.CreateVersion7();

            // Act
            var humanMember = Member.CreateHuman(channelId, userId, "Phong Dang");
            var agentMember = Member.CreateAgent(channelId, agentProfileId, "Research Agent");

            // Assert
            Assert.NotEqual(Guid.Empty, humanMember.Id);
            Assert.Equal(7, humanMember.Id.Version);
            Assert.Equal(MemberType.Human, humanMember.Type);
            Assert.Equal(userId, humanMember.UserId);
            Assert.Null(humanMember.AgentProfileId);
            Assert.Equal("Phong Dang", humanMember.DisplayName);

            Assert.NotEqual(Guid.Empty, agentMember.Id);
            Assert.Equal(7, agentMember.Id.Version);
            Assert.Equal(MemberType.Agent, agentMember.Type);
            Assert.Equal(agentProfileId, agentMember.AgentProfileId);
            Assert.Null(agentMember.UserId);
            Assert.Equal("Research Agent", agentMember.DisplayName);
        }

        [Fact]
        public void AgentProfile_Should_Initialize_With_Correct_Attributes()
        {
            // Act
            var agent = new AgentProfile(
                "Researcher",
                "https://cdn.example.com/bot.png",
                ModelProviderType.Gemini,
                "gemini-2.5-flash",
                "You are an expert tech researcher.",
                0.7m,
                "Always format output as markdown tables.");

            // Assert
            Assert.NotEqual(Guid.Empty, agent.Id);
            Assert.Equal(7, agent.Id.Version);
            Assert.Equal("Researcher", agent.Name);
            Assert.Equal("https://cdn.example.com/bot.png", agent.AvatarUrl);
            Assert.Equal(ModelProviderType.Gemini, agent.Provider);
            Assert.Equal("gemini-2.5-flash", agent.ModelName);
            Assert.Equal("You are an expert tech researcher.", agent.SystemPrompt);
            Assert.Equal(0.7m, agent.Temperature);
            Assert.Equal("Always format output as markdown tables.", agent.PrivateMemoryNotes);
            Assert.False(agent.IsDeleted);
        }
    }
}
