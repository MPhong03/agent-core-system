// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.Channels.Commands.CreateTeamChannel;
using AgentCoreSystem.Module.Organization.Application.Channels.Queries.GetChannelsByDepartment;
using AgentCoreSystem.Module.Organization.Application.Departments.Commands.CreateDepartment;
using AgentCoreSystem.Module.Organization.Application.Departments.Queries.GetDepartments;
using AgentCoreSystem.Module.Organization.Domain.Entities;
using AgentCoreSystem.Module.Organization.Infrastructure.Persistence;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgentCoreSystem.UnitTests.Organization
{
    /// <summary>
    /// Kiểm thử các Command &amp; Query Handler cho phân hệ Quản lý Phòng ban &amp; Kênh nhóm.
    /// </summary>
    public class DepartmentCommandHandlerTests
    {
        private static OrganizationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<OrganizationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new OrganizationDbContext(options);
        }

        [Fact]
        public async Task CreateDepartment_ShouldSucceed_WhenNameIsUnique()
        {
            using var db = CreateDbContext();
            var handler = new CreateDepartmentCommandHandler(db);
            var command = new CreateDepartmentCommand("Engineering", "Software Engineering Department");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotEqual(Guid.Empty, result.Value);

            var saved = await db.Departments.FirstOrDefaultAsync(d => d.Id == result.Value);
            Assert.NotNull(saved);
            Assert.Equal("Engineering", saved.Name);
            Assert.Equal("Software Engineering Department", saved.Description);
        }

        [Fact]
        public async Task CreateDepartment_ShouldReturnConflict_WhenNameAlreadyExists()
        {
            using var db = CreateDbContext();
            db.Departments.Add(new Department("Engineering", "Existing Department"));
            await db.SaveChangesAsync();

            var handler = new CreateDepartmentCommandHandler(db);
            var command = new CreateDepartmentCommand("Engineering", "Duplicate Department");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.Conflict, result.Error.Type);
            Assert.Equal("Department.NameConflict", result.Error.Code);
        }

        [Fact]
        public async Task GetDepartments_ShouldReturnDepartments_WithChannelCount()
        {
            using var db = CreateDbContext();
            var dept1 = new Department("Engineering", "R&D");
            var dept2 = new Department("Marketing", "Growth");
            db.Departments.AddRange(dept1, dept2);
            await db.SaveChangesAsync();

            db.Channels.Add(new TeamChannel(dept1.Id, "backend", "Backend channel"));
            db.Channels.Add(new TeamChannel(dept1.Id, "frontend", "Frontend channel"));
            await db.SaveChangesAsync();

            var handler = new GetDepartmentsQueryHandler(db);
            var query = new GetDepartmentsQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            var eng = result.Value.First(d => d.Id == dept1.Id);
            Assert.Equal(2, eng.ChannelCount);
            var mkt = result.Value.First(d => d.Id == dept2.Id);
            Assert.Equal(0, mkt.ChannelCount);
        }

        [Fact]
        public async Task CreateTeamChannel_ShouldSucceed_WhenDepartmentExistsAndNameIsUnique()
        {
            using var db = CreateDbContext();
            var dept = new Department("Engineering", "R&D");
            db.Departments.Add(dept);
            await db.SaveChangesAsync();

            var handler = new CreateTeamChannelCommandHandler(db);
            var command = new CreateTeamChannelCommand(dept.Id, "tech-research", "Research emerging tech");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotEqual(Guid.Empty, result.Value);

            var saved = await db.Channels.FirstOrDefaultAsync(c => c.Id == result.Value);
            Assert.NotNull(saved);
            Assert.Equal("tech-research", saved.Name);
            Assert.Equal(dept.Id, saved.DepartmentId);
        }

        [Fact]
        public async Task CreateTeamChannel_ShouldReturnNotFound_WhenDepartmentDoesNotExist()
        {
            using var db = CreateDbContext();
            var handler = new CreateTeamChannelCommandHandler(db);
            var nonExistentDeptId = Guid.CreateVersion7();
            var command = new CreateTeamChannelCommand(nonExistentDeptId, "general", "General channel");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.Error.Type);
            Assert.Equal("Department.NotFound", result.Error.Code);
        }

        [Fact]
        public async Task CreateTeamChannel_ShouldReturnConflict_WhenChannelNameAlreadyExistsInDepartment()
        {
            using var db = CreateDbContext();
            var dept = new Department("Engineering", "R&D");
            db.Departments.Add(dept);
            await db.SaveChangesAsync();

            db.Channels.Add(new TeamChannel(dept.Id, "tech-research", "Existing channel"));
            await db.SaveChangesAsync();

            var handler = new CreateTeamChannelCommandHandler(db);
            var command = new CreateTeamChannelCommand(dept.Id, "tech-research", "Duplicate channel");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.Conflict, result.Error.Type);
            Assert.Equal("Channel.NameConflict", result.Error.Code);
        }

        [Fact]
        public async Task GetChannelsByDepartment_ShouldReturnChannels_WhenDepartmentExists()
        {
            using var db = CreateDbContext();
            var dept = new Department("Engineering", "R&D");
            db.Departments.Add(dept);
            await db.SaveChangesAsync();

            var channel = new TeamChannel(dept.Id, "dev-chat", "Dev discussions");
            db.Channels.Add(channel);
            await db.SaveChangesAsync();

            db.Members.Add(Member.CreateHuman(channel.Id, Guid.CreateVersion7(), "Phong Dang"));
            await db.SaveChangesAsync();

            var handler = new GetChannelsByDepartmentQueryHandler(db);
            var query = new GetChannelsByDepartmentQuery(dept.Id);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal("dev-chat", result.Value[0].Name);
            Assert.Equal(1, result.Value[0].MemberCount);
        }

        [Fact]
        public async Task GetChannelsByDepartment_ShouldReturnNotFound_WhenDepartmentDoesNotExist()
        {
            using var db = CreateDbContext();
            var handler = new GetChannelsByDepartmentQueryHandler(db);
            var query = new GetChannelsByDepartmentQuery(Guid.CreateVersion7());

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.Error.Type);
            Assert.Equal("Department.NotFound", result.Error.Code);
        }
    }
}
