// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Services.Python;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;
using AgentCoreSystem.Module.Agents.Application.Workspace.Commands;
using AgentCoreSystem.Module.Agents.Application.Workspace.DTOs;
using AgentCoreSystem.Module.Agents.Application.Workspace.Queries;
using AgentCoreSystem.Module.Agents.Presentation.Contracts;
using AgentCoreSystem.Module.Agents.Presentation.Controllers.v1;
using AgentCoreSystem.Shared.Kernel.Primitives;
using AgentCoreSystem.Shared.Kernel.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests.Controllers
{
    public class AgentWorkspaceControllerTests
    {
        private readonly ISender _mediator = Substitute.For<ISender>();
        private readonly IWorkspaceManager _workspaceManager = Substitute.For<IWorkspaceManager>();
        private readonly IPythonSandboxRunner _pythonRunner = Substitute.For<IPythonSandboxRunner>();
        private readonly AgentWorkspaceController _controller;
        private readonly Guid _agentId = Guid.CreateVersion7();

        public AgentWorkspaceControllerTests()
        {
            _controller = new AgentWorkspaceController(_mediator, _workspaceManager, _pythonRunner)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task GetWorkspace_WhenSuccessful_ShouldReturnOkWithData()
        {
            // Arrange
            var dto = new AgentWorkspaceDetailDto(
                Guid.CreateVersion7(),
                _agentId,
                "C:/Workspaces/1",
                "3.12.0",
                60,
                true,
                true,
                2048,
                3,
                1,
                2,
                DateTime.UtcNow
            );

            _mediator.Send(Arg.Any<GetAgentWorkspaceQuery>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Success(dto)));

            // Act
            var actionResult = await _controller.GetWorkspace(_agentId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<AgentWorkspaceDetailDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(_agentId, response.Data.AgentProfileId);
        }

        [Fact]
        public async Task ExecuteScript_WhenSuccessful_ShouldReturnExecutionResult()
        {
            // Arrange
            var execResult = new PythonExecutionResult(
                IsSuccess: true,
                ExitCode: 0,
                Stdout: "output\n",
                Stderr: string.Empty,
                DurationMs: 80,
                IsTimeout: false,
                GeneratedArtifacts: Array.Empty<ArtifactFileDto>()
            );

            _mediator.Send(Arg.Any<ExecuteScriptCommand>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Success(execResult)));

            var request = new ExecuteScriptApiRequest("print('output')", "test.py", 30);

            // Act
            var actionResult = await _controller.ExecuteScript(_agentId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<PythonExecutionResult>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(0, response.Data.ExitCode);
        }

        [Fact]
        public async Task DownloadFile_WhenNotFound_ShouldReturnProblemDetails()
        {
            // Arrange
            _workspaceManager.GetFileStreamAsync(_agentId, "missing.csv", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Failure<Stream>(Error.NotFound("Workspace.FileNotFound", "File not found."))));

            // Act
            var actionResult = await _controller.DownloadFile(_agentId, "missing.csv", CancellationToken.None);

            // Assert
            var problemResult = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status404NotFound, problemResult.StatusCode);
        }
    }
}
