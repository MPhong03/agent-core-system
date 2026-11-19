// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.Commands.CreateAgentProfile;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.Queries.GetAgentProfiles;
using AgentCoreSystem.Module.Organization.Application.Members.Commands.AssignMemberToChannel;
using AgentCoreSystem.Module.Organization.Application.Members.Queries.GetChannelMembers;
using AgentCoreSystem.Module.Organization.Domain.Entities;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Module.Organization.Infrastructure.Persistence;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgentCoreSystem.UnitTests.Organization
{
    /// <summary>
    /// Kiểm thử các Command &amp; Query Handler cho phân hệ Hồ sơ Nhân sự AI Agent và Phân bổ Thành viên.
    /// </summary>
    public class AgentProfileCommandHandlerTests
    {
        private static OrganizationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<OrganizationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new OrganizationDbContext(options);
        }

        [Fact]
        public async Task CreateAgentProfile_ShouldSucceed_AndSaveToDatabase()
        {
            using var db = CreateDbContext();
            var handler = new CreateAgentProfileCommandHandler(db);
            var command = new CreateAgentProfileCommand(
                Name: "Tech Researcher",
                AvatarUrl: "https://example.com/bot.png",
                Provider: ModelProviderType.Gemini,
                ModelName: "gemini-2.5-flash",
                SystemPrompt: "You are an expert researcher.",
                Temperature: 0.5m,
                PrivateMemoryNotes: "Prefer concise summary.");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotEqual(Guid.Empty, result.Value);

            var saved = await db.AgentProfiles.FirstOrDefaultAsync(a => a.Id == result.Value);
            Assert.NotNull(saved);
            Assert.Equal("Tech Researcher", saved.Name);
            Assert.Equal(ModelProviderType.Gemini, saved.Provider);
            Assert.Equal("gemini-2.5-flash", saved.ModelName);
            Assert.Equal("You are an expert researcher.", saved.SystemPrompt);
            Assert.Equal(0.5m, saved.Temperature);
            Assert.Equal("Prefer concise summary.", saved.PrivateMemoryNotes);
        }

        [Fact]
        public async Task GetAgentProfiles_ShouldReturnAllActiveProfiles()
        {
            using var db = CreateDbContext();
            db.AgentProfiles.Add(new AgentProfile("Bot 1", null, ModelProviderType.OpenAI, "gpt-4o", "Prompt 1"));
            db.AgentProfiles.Add(new AgentProfile("Bot 2", null, ModelProviderType.Claude, "claude-3-7-sonnet", "Prompt 2"));
            await db.SaveChangesAsync();

            var handler = new GetAgentProfilesQueryHandler(db);
            var query = new GetAgentProfilesQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
        }

        [Fact]
        public async Task AssignMemberToChannel_ShouldSucceed_ForHumanMember()
        {
            using var db = CreateDbContext();
            var dept = new Department("Engineering");
            db.Departments.Add(dept);
            await db.SaveChangesAsync();

            var channel = new TeamChannel(dept.Id, "general");
            db.Channels.Add(channel);
            await db.SaveChangesAsync();

            var userId = Guid.CreateVersion7();
            var handler = new AssignMemberToChannelCommandHandler(db);
            var command = new AssignMemberToChannelCommand(
                ChannelId: channel.Id,
                Type: MemberType.Human,
                UserId: userId,
                AgentProfileId: null,
                DisplayName: "Phong Minh Dang");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotEqual(Guid.Empty, result.Value);

            var member = await db.Members.FirstOrDefaultAsync(m => m.Id == result.Value);
            Assert.NotNull(member);
            Assert.Equal(MemberType.Human, member.Type);
            Assert.Equal(userId, member.UserId);
            Assert.Null(member.AgentProfileId);
            Assert.Equal("Phong Minh Dang", member.DisplayName);
        }

        [Fact]
        public async Task AssignMemberToChannel_ShouldSucceed_ForAgentMember()
        {
            using var db = CreateDbContext();
            var dept = new Department("Engineering");
            db.Departments.Add(dept);
            await db.SaveChangesAsync();

            var channel = new TeamChannel(dept.Id, "general");
            db.Channels.Add(channel);
            await db.SaveChangesAsync();

            var agent = new AgentProfile("Code Reviewer", null, ModelProviderType.Gemini, "gemini-2.5-flash", "System prompt");
            db.AgentProfiles.Add(agent);
            await db.SaveChangesAsync();

            var handler = new AssignMemberToChannelCommandHandler(db);
            var command = new AssignMemberToChannelCommand(
                ChannelId: channel.Id,
                Type: MemberType.Agent,
                UserId: null,
                AgentProfileId: agent.Id,
                DisplayName: "Bot Code Reviewer");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotEqual(Guid.Empty, result.Value);

            var member = await db.Members.FirstOrDefaultAsync(m => m.Id == result.Value);
            Assert.NotNull(member);
            Assert.Equal(MemberType.Agent, member.Type);
            Assert.Equal(agent.Id, member.AgentProfileId);
            Assert.Null(member.UserId);
            Assert.Equal("Bot Code Reviewer", member.DisplayName);
        }

        [Fact]
        public async Task AssignMemberToChannel_ShouldReturnNotFound_WhenChannelDoesNotExist()
        {
            using var db = CreateDbContext();
            var handler = new AssignMemberToChannelCommandHandler(db);
            var command = new AssignMemberToChannelCommand(
                ChannelId: Guid.CreateVersion7(),
                Type: MemberType.Human,
                UserId: Guid.CreateVersion7(),
                AgentProfileId: null,
                DisplayName: "User");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.Error.Type);
            Assert.Equal("Channel.NotFound", result.Error.Code);
        }

        [Fact]
        public async Task AssignMemberToChannel_ShouldReturnNotFound_WhenAgentProfileDoesNotExist()
        {
            using var db = CreateDbContext();
            var dept = new Department("Engineering");
            db.Departments.Add(dept);
            await db.SaveChangesAsync();

            var channel = new TeamChannel(dept.Id, "general");
            db.Channels.Add(channel);
            await db.SaveChangesAsync();

            var handler = new AssignMemberToChannelCommandHandler(db);
            var command = new AssignMemberToChannelCommand(
                ChannelId: channel.Id,
                Type: MemberType.Agent,
                UserId: null,
                AgentProfileId: Guid.CreateVersion7(),
                DisplayName: "NonExistent Agent");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.Error.Type);
            Assert.Equal("AgentProfile.NotFound", result.Error.Code);
        }

        [Fact]
        public async Task AssignMemberToChannel_ShouldReturnConflict_WhenDisplayNameAlreadyExistsInChannel()
        {
            using var db = CreateDbContext();
            var dept = new Department("Engineering");
            db.Departments.Add(dept);
            await db.SaveChangesAsync();

            var channel = new TeamChannel(dept.Id, "general");
            db.Channels.Add(channel);
            await db.SaveChangesAsync();

            db.Members.Add(Member.CreateHuman(channel.Id, Guid.CreateVersion7(), "Alice"));
            await db.SaveChangesAsync();

            var handler = new AssignMemberToChannelCommandHandler(db);
            var command = new AssignMemberToChannelCommand(
                ChannelId: channel.Id,
                Type: MemberType.Human,
                UserId: Guid.CreateVersion7(),
                AgentProfileId: null,
                DisplayName: "Alice");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.Conflict, result.Error.Type);
            Assert.Equal("Member.DisplayNameConflict", result.Error.Code);
        }

        [Fact]
        public async Task GetChannelMembers_ShouldReturnMembers_WhenChannelExists()
        {
            using var db = CreateDbContext();
            var dept = new Department("Engineering");
            db.Departments.Add(dept);
            await db.SaveChangesAsync();

            var channel = new TeamChannel(dept.Id, "general");
            db.Channels.Add(channel);
            await db.SaveChangesAsync();

            db.Members.Add(Member.CreateHuman(channel.Id, Guid.CreateVersion7(), "Alice"));
            await db.SaveChangesAsync();

            var handler = new GetChannelMembersQueryHandler(db);
            var query = new GetChannelMembersQuery(channel.Id);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal("Alice", result.Value[0].DisplayName);
        }

        [Fact]
        public async Task GetChannelMembers_ShouldReturnNotFound_WhenChannelDoesNotExist()
        {
            using var db = CreateDbContext();
            var handler = new GetChannelMembersQueryHandler(db);
            var query = new GetChannelMembersQuery(Guid.CreateVersion7());

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.Error.Type);
            Assert.Equal("Channel.NotFound", result.Error.Code);
        }
    }
}
