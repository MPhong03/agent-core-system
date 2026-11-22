// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using Xunit;

namespace AgentCoreSystem.UnitTests.Entities
{
    public class AgentWorkspaceEntitiesTests
    {
        [Fact]
        public void AgentWorkspaceConfig_Create_ValidParameters_ShouldReturnSuccess()
        {
            // Arrange
            var agentProfileId = Guid.CreateVersion7();

            // Act
            var result = AgentWorkspaceConfig.Create(agentProfileId, maxTimeoutSeconds: 90, pythonVersion: "3.12.0");

            // Assert
            Assert.True(result.IsSuccess);
            var config = result.Value;
            Assert.NotEqual(Guid.Empty, config.Id);
            Assert.Equal(7, config.Id.Version);
            Assert.Equal(agentProfileId, config.AgentProfileId);
            Assert.Equal(90, config.MaxTimeoutSeconds);
            Assert.Equal("3.12.0", config.PythonVersion);
            Assert.False(config.IsVenvReady);
            Assert.True(config.IsActive);
        }

        [Fact]
        public void AgentWorkspaceConfig_MarkVenvReady_ShouldUpdateState()
        {
            // Arrange
            var agentProfileId = Guid.CreateVersion7();
            var config = AgentWorkspaceConfig.Create(agentProfileId).Value;

            // Act
            config.MarkVenvReady("3.12.1");

            // Assert
            Assert.True(config.IsVenvReady);
            Assert.Equal("3.12.1", config.PythonVersion);
            Assert.NotNull(config.LastActiveAt);
        }

        [Fact]
        public void ScriptExecutionHistory_Create_ValidParameters_ShouldReturnSuccess()
        {
            // Arrange
            var agentProfileId = Guid.CreateVersion7();
            var sourceId = Guid.CreateVersion7();

            // Act
            var result = ScriptExecutionHistory.Create(
                agentProfileId: agentProfileId,
                callerType: "Chat",
                executionSourceId: sourceId,
                scriptName: "calc.py",
                scriptContent: "print(1+1)",
                exitCode: 0,
                stdout: "2\n",
                stderr: null,
                durationMs: 150,
                isTimeout: false,
                artifactsJson: "[]"
            );

            // Assert
            Assert.True(result.IsSuccess);
            var history = result.Value;
            Assert.NotEqual(Guid.Empty, history.Id);
            Assert.Equal(7, history.Id.Version);
            Assert.Equal(agentProfileId, history.AgentProfileId);
            Assert.Equal("Chat", history.CallerType);
            Assert.Equal("calc.py", history.ScriptName);
            Assert.Equal(0, history.ExitCode);
            Assert.Equal(150, history.DurationMs);
            Assert.False(history.IsTimeout);
        }
    }
}
