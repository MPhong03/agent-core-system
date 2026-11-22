// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Application.Services.Python;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;
using AgentCoreSystem.Module.Agents.Application.Workspace.Commands;
using AgentCoreSystem.Module.Agents.Application.Workspace.DTOs;
using AgentCoreSystem.Module.Agents.Application.Workspace.Queries;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using AgentCoreSystem.Module.Agents.Infrastructure.Persistence;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests.Workspace
{
    public class WorkspaceCqrsTests : IDisposable
    {
        private readonly AgentsDbContext _dbContext;
        private readonly IWorkspaceManager _workspaceManager;
        private readonly IPythonSandboxRunner _pythonRunner;
        private readonly Guid _agentProfileId = Guid.CreateVersion7();

        public WorkspaceCqrsTests()
        {
            var options = new DbContextOptionsBuilder<AgentsDbContext>()
                .UseInMemoryDatabase("AgentsCqrsTests_" + Guid.NewGuid().ToString("N"))
                .Options;

            _dbContext = new AgentsDbContext(options);
            _workspaceManager = Substitute.For<IWorkspaceManager>();
            _pythonRunner = Substitute.For<IPythonSandboxRunner>();
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetAgentWorkspaceQuery_WhenConfigNotExists_ShouldCreateDefaultAndReturn()
        {
            // Arrange
            _workspaceManager.GetWorkspaceStatsAsync(_agentProfileId, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Success(new WorkspaceStatsDto(
                    _agentProfileId,
                    "C:/Workspaces/1",
                    VenvExists: false,
                    TotalSizeBytes: 1024,
                    TotalFilesCount: 2,
                    ScriptsCount: 1,
                    ArtifactsCount: 1
                ))));

            var handler = new GetAgentWorkspaceQueryHandler(_dbContext, _workspaceManager);
            var query = new GetAgentWorkspaceQuery(_agentProfileId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(_agentProfileId, result.Value.AgentProfileId);
            Assert.Equal(1024, result.Value.StorageUsedBytes);
            Assert.False(result.Value.IsVenvReady);
        }

        [Fact]
        public async Task ExecuteScriptCommand_ShouldRunScriptAndSaveHistory()
        {
            // Arrange
            var config = AgentWorkspaceConfig.Create(_agentProfileId).Value;
            _dbContext.AgentWorkspaceConfigs.Add(config);
            await _dbContext.SaveChangesAsync();

            var expectedResult = new PythonExecutionResult(
                IsSuccess: true,
                ExitCode: 0,
                Stdout: "result: 42\n",
                Stderr: string.Empty,
                DurationMs: 120,
                IsTimeout: false,
                GeneratedArtifacts: new List<ArtifactFileDto>
                {
                    new("chart.png", 5000, "artifacts/chart.png")
                }
            );

            _pythonRunner.ExecuteScriptAsync(Arg.Any<PythonExecutionRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Success(expectedResult)));

            _workspaceManager.GetWorkspaceStatsAsync(_agentProfileId, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Success(new WorkspaceStatsDto(_agentProfileId, "path", true, 5000, 1, 0, 1))));

            var handler = new ExecuteScriptCommandHandler(_dbContext, _pythonRunner, _workspaceManager);
            var command = new ExecuteScriptCommand(
                AgentProfileId: _agentProfileId,
                ScriptContent: "print('result: 42')",
                ScriptName: "test.py",
                TimeoutSeconds: 30,
                CallerType: "Chat",
                ExecutionSourceId: Guid.CreateVersion7()
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.IsSuccess);
            Assert.Contains("result: 42", result.Value.Stdout);
            Assert.Single(result.Value.GeneratedArtifacts);

            // Verify history persisted in database
            var historyRecord = await _dbContext.ScriptExecutionHistories.FirstOrDefaultAsync(h => h.AgentProfileId == _agentProfileId);
            Assert.NotNull(historyRecord);
            Assert.Equal("Chat", historyRecord.CallerType);
            Assert.Equal("test.py", historyRecord.ScriptName);
            Assert.Equal(0, historyRecord.ExitCode);
        }

        [Fact]
        public async Task InitializeVenvCommand_WhenSuccessful_ShouldUpdateConfig()
        {
            // Arrange
            var config = AgentWorkspaceConfig.Create(_agentProfileId).Value;
            _dbContext.AgentWorkspaceConfigs.Add(config);
            await _dbContext.SaveChangesAsync();

            _pythonRunner.EnsureVenvInitializedAsync(_agentProfileId, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Success("C:/Workspaces/1/.venv")));

            var handler = new InitializeVenvCommandHandler(_dbContext, _pythonRunner);
            var command = new InitializeVenvCommand(_agentProfileId);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            var updatedConfig = await _dbContext.AgentWorkspaceConfigs.FirstAsync(c => c.AgentProfileId == _agentProfileId);
            Assert.True(updatedConfig.IsVenvReady);
        }
    }
}
