// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Services.Python;
using AgentCoreSystem.Module.Agents.Application.Services.Tools;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;
using AgentCoreSystem.Module.Agents.Application.Tools.Commands;
using AgentCoreSystem.Module.Agents.Application.Workspace.Commands;
using AgentCoreSystem.Module.Agents.Application.Workspace.Queries;
using AgentCoreSystem.Shared.Kernel.Primitives;
using MediatR;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests.Tools
{
    public class SystemBuiltInToolsTests
    {
        private readonly ISender _sender = Substitute.For<ISender>();
        private readonly Guid _agentProfileId = Guid.CreateVersion7();

        [Fact]
        public void SystemBuiltInTools_GetAll_ShouldIncludePythonTools()
        {
            // Act
            var tools = SystemBuiltInTools.GetAll();

            // Assert
            Assert.Contains(tools, t => t.Name == SystemBuiltInTools.ExecutePythonCodeToolName);
            Assert.Contains(tools, t => t.Name == SystemBuiltInTools.InstallPythonPackagesToolName);
            Assert.Contains(tools, t => t.Name == SystemBuiltInTools.ListWorkspaceFilesToolName);

            var execTool = SystemBuiltInTools.GetByName(SystemBuiltInTools.ExecutePythonCodeToolName);
            Assert.NotNull(execTool);
            Assert.True(execTool.RequiresConfirmation);
        }

        [Fact]
        public async Task ExecuteSystemToolCommandHandler_ExecutePythonCode_ShouldCallExecuteScriptCommand()
        {
            // Arrange
            var execResult = new PythonExecutionResult(
                IsSuccess: true,
                ExitCode: 0,
                Stdout: "calc result: 100\n",
                Stderr: string.Empty,
                DurationMs: 50,
                IsTimeout: false,
                GeneratedArtifacts: new List<ArtifactFileDto>
                {
                    new("plot.png", 1024, "artifacts/plot.png")
                }
            );

            _sender.Send(Arg.Any<ExecuteScriptCommand>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Success(execResult)));

            var handler = new ExecuteSystemToolCommandHandler(_sender);
            var args = JsonSerializer.Serialize(new
            {
                code = "print('calc result: 100')",
                script_name = "test.py",
                timeout_seconds = 30
            });

            var command = new ExecuteSystemToolCommand(
                AgentProfileId: _agentProfileId,
                ToolName: SystemBuiltInTools.ExecutePythonCodeToolName,
                ArgumentsJson: args,
                CallerType: "Chat",
                ExecutionSourceId: Guid.CreateVersion7()
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("calc result: 100", result.Value);
            Assert.Contains("plot.png", result.Value);
        }

        [Fact]
        public async Task ExecuteSystemToolCommandHandler_InstallPackages_ShouldCallInstallPackagesCommand()
        {
            // Arrange
            _sender.Send(Arg.Any<InstallPackagesCommand>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Success("Successfully installed pandas")));

            var handler = new ExecuteSystemToolCommandHandler(_sender);
            var args = JsonSerializer.Serialize(new
            {
                packages = new[] { "pandas" }
            });

            var command = new ExecuteSystemToolCommand(
                AgentProfileId: _agentProfileId,
                ToolName: SystemBuiltInTools.InstallPythonPackagesToolName,
                ArgumentsJson: args,
                CallerType: "Chat",
                ExecutionSourceId: Guid.CreateVersion7()
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("Successfully installed pandas", result.Value);
        }
    }
}
