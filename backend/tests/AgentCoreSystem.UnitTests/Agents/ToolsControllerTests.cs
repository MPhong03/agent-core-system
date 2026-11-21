// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Tools.Commands;
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Module.Agents.Application.Tools.Queries;
using AgentCoreSystem.Module.Agents.Domain.Enums;
using AgentCoreSystem.Module.Agents.Presentation.Contracts;
using AgentCoreSystem.Module.Agents.Presentation.Controllers.v1;
using AgentCoreSystem.Shared.Kernel.Primitives;
using AgentCoreSystem.Shared.Kernel.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests.Agents
{
    public class ToolsControllerTests
    {
        private readonly ISender _mediator = Substitute.For<ISender>();
        private readonly ToolsController _controller;
        private readonly AgentToolsController _agentToolsController;

        public ToolsControllerTests()
        {
            _controller = new ToolsController(_mediator)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            _agentToolsController = new AgentToolsController(_mediator)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task GetToolDefinitions_ShouldReturnOkWithMaskedHeaders()
        {
            // Arrange
            var tools = new List<ToolDefinitionDto>
            {
                new(
                    Guid.NewGuid(),
                    "weather_api",
                    "Weather API",
                    "Get weather details",
                    ToolType.RestApi,
                    "https://api.weather.com/v1",
                    "GET",
                    "{\"Authorization\":\"••••••••\"}",
                    "{}",
                    false,
                    30,
                    true,
                    DateTimeOffset.UtcNow,
                    null)
            };

            _mediator.Send(Arg.Any<GetToolDefinitionsQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success(tools));

            // Act
            var actionResult = await _controller.GetToolDefinitions(CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<List<ToolDefinitionDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Single(response.Data);
            Assert.Equal("weather_api", response.Data[0].Name);
            Assert.Contains("••••••••", response.Data[0].HeadersConfig);
        }

        [Fact]
        public async Task GetToolDefinitionById_WhenFound_ShouldReturnOk()
        {
            // Arrange
            var toolId = Guid.NewGuid();
            var toolDto = new ToolDefinitionDto(
                toolId,
                "calculator",
                "Calculator Tool",
                "Performs arithmetic",
                ToolType.RestApi,
                "https://api.calc.local/run",
                "POST",
                "{}",
                "{}",
                false,
                10,
                true,
                DateTimeOffset.UtcNow,
                null);

            _mediator.Send(Arg.Is<GetToolDefinitionByIdQuery>(q => q.Id == toolId), Arg.Any<CancellationToken>())
                .Returns(Result.Success(toolDto));

            // Act
            var actionResult = await _controller.GetToolDefinitionById(toolId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<ToolDefinitionDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(toolId, response.Data!.Id);
        }

        [Fact]
        public async Task GetToolDefinitionById_WhenNotFound_ShouldReturnNotFoundProblemDetails()
        {
            // Arrange
            var toolId = Guid.NewGuid();
            _mediator.Send(Arg.Is<GetToolDefinitionByIdQuery>(q => q.Id == toolId), Arg.Any<CancellationToken>())
                .Returns(Result.Failure<ToolDefinitionDto>(Error.NotFound("ToolDefinition.NotFound", "Không tìm thấy")));

            // Act
            var actionResult = await _controller.GetToolDefinitionById(toolId, CancellationToken.None);

            // Assert
            var problemResult = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status404NotFound, problemResult.StatusCode);
        }

        [Fact]
        public async Task CreateToolDefinition_WhenValid_ShouldReturnCreated()
        {
            // Arrange
            var request = new CreateToolRequest(
                "send_slack_message",
                "Send Slack Message",
                "Sends a notification to a Slack channel",
                ToolType.RestApi,
                "https://slack.com/api/chat.postMessage",
                "POST",
                "{\"Authorization\":\"Bearer secret_token\"}",
                "{}",
                false,
                30);

            var createdDto = new ToolDefinitionDto(
                Guid.NewGuid(),
                request.Name,
                request.DisplayName,
                request.Description,
                request.Type,
                request.EndpointUrl,
                request.HttpMethod,
                "{\"Authorization\":\"••••••••\"}",
                "{}",
                false,
                30,
                true,
                DateTimeOffset.UtcNow,
                null);

            _mediator.Send(Arg.Is<CreateToolDefinitionCommand>(c => c.Name == request.Name), Arg.Any<CancellationToken>())
                .Returns(Result.Success(createdDto));

            // Act
            var actionResult = await _controller.CreateToolDefinition(request, CancellationToken.None);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
            var response = Assert.IsType<ApiResponse<ToolDefinitionDto>>(createdResult.Value);
            Assert.True(response.Success);
            Assert.Equal(createdDto.Id, response.Data!.Id);
            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        }

        [Fact]
        public async Task CreateToolDefinition_WhenConflict_ShouldReturnConflictProblemDetails()
        {
            // Arrange
            var request = new CreateToolRequest(
                "duplicate_tool",
                "Duplicate",
                "Desc",
                ToolType.RestApi,
                "https://api.test.com",
                "POST");

            _mediator.Send(Arg.Any<CreateToolDefinitionCommand>(), Arg.Any<CancellationToken>())
                .Returns(Result.Failure<ToolDefinitionDto>(Error.Conflict("ToolDefinition.DuplicateName", "Tên đã tồn tại")));

            // Act
            var actionResult = await _controller.CreateToolDefinition(request, CancellationToken.None);

            // Assert
            var problemResult = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status409Conflict, problemResult.StatusCode);
        }

        [Fact]
        public async Task UpdateToolDefinition_WhenValid_ShouldReturnOk()
        {
            // Arrange
            var toolId = Guid.NewGuid();
            var request = new UpdateToolRequest(
                "Updated Tool",
                "Updated Desc",
                "https://api.updated.com/v2",
                "PUT",
                "{\"Authorization\":\"••••••••\"}",
                "{}",
                true,
                45);

            var updatedDto = new ToolDefinitionDto(
                toolId,
                "my_tool",
                request.DisplayName,
                request.Description,
                ToolType.RestApi,
                request.EndpointUrl,
                request.HttpMethod,
                "{\"Authorization\":\"••••••••\"}",
                request.ParametersSchema!,
                request.RequiresConfirmation,
                request.TimeoutSeconds,
                true,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow);

            _mediator.Send(Arg.Is<UpdateToolDefinitionCommand>(c => c.Id == toolId), Arg.Any<CancellationToken>())
                .Returns(Result.Success(updatedDto));

            // Act
            var actionResult = await _controller.UpdateToolDefinition(toolId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<ToolDefinitionDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Updated Tool", response.Data!.DisplayName);
        }

        [Fact]
        public async Task DeleteToolDefinition_WhenValid_ShouldReturnOk()
        {
            // Arrange
            var toolId = Guid.NewGuid();
            _mediator.Send(Arg.Is<DeleteToolDefinitionCommand>(c => c.Id == toolId), Arg.Any<CancellationToken>())
                .Returns(Result.Success());

            // Act
            var actionResult = await _controller.DeleteToolDefinition(toolId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task TestToolDefinitionWithId_ShouldReturnExecutionResults()
        {
            // Arrange
            var toolId = Guid.NewGuid();
            var request = new TestToolRequest(
                HeadersConfig: null,
                Arguments: new Dictionary<string, object?> { ["city"] = "Hanoi" },
                ArgumentsJson: null,
                TimeoutSeconds: 15);

            var testResultDto = new ToolTestResultDto(
                IsSuccess: true,
                StatusCode: 200,
                ResponsePayload: "{\"temp\":28}",
                DurationMs: 120,
                ErrorMessage: null);

            _mediator.Send(Arg.Is<TestToolDefinitionCommand>(c => c.ToolDefinitionId == toolId), Arg.Any<CancellationToken>())
                .Returns(Result.Success(testResultDto));

            // Act
            var actionResult = await _controller.TestToolDefinition(toolId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<ToolTestResultDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.True(response.Data!.IsSuccess);
            Assert.Equal(200, response.Data.StatusCode);
            Assert.Contains("temp", response.Data.ResponsePayload);
        }

        [Fact]
        public async Task TestToolDefinitionAdHoc_ShouldReturnExecutionResults()
        {
            // Arrange
            var request = new TestAdHocToolRequest(
                EndpointUrl: "https://api.github.com/zen",
                HttpMethod: "GET",
                HeadersConfig: "{\"User-Agent\":\"AgentCoreSystem\"}",
                Arguments: null,
                ArgumentsJson: null,
                TimeoutSeconds: 10);

            var testResultDto = new ToolTestResultDto(
                IsSuccess: true,
                StatusCode: 200,
                ResponsePayload: "Practicality beats purity.",
                DurationMs: 250,
                ErrorMessage: null);

            _mediator.Send(Arg.Is<TestToolDefinitionCommand>(c => c.ToolDefinitionId == null && c.EndpointUrl == request.EndpointUrl), Arg.Any<CancellationToken>())
                .Returns(Result.Success(testResultDto));

            // Act
            var actionResult = await _controller.TestAdHocTool(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<ToolTestResultDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(200, response.Data!.StatusCode);
        }

        [Fact]
        public async Task GetAgentTools_ShouldReturnOkWithTools()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var tools = new List<ToolDefinitionDto>
            {
                new(
                    Guid.NewGuid(),
                    "search_tool",
                    "Search Tool",
                    "Performs web search",
                    ToolType.RestApi,
                    "https://api.search.com",
                    "GET",
                    "{}",
                    "{}",
                    false,
                    30,
                    true,
                    DateTimeOffset.UtcNow,
                    null)
            };

            _mediator.Send(Arg.Is<GetAgentToolsQuery>(q => q.AgentProfileId == agentId), Arg.Any<CancellationToken>())
                .Returns(Result.Success(tools));

            // Act
            var actionResult = await _agentToolsController.GetAgentTools(agentId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<List<ToolDefinitionDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Single(response.Data!);
        }

        [Fact]
        public async Task AssignAgentTools_WhenValid_ShouldReturnOk()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var request = new AssignAgentToolsRequest(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

            _mediator.Send(Arg.Is<AssignAgentToolsCommand>(c => c.AgentProfileId == agentId && c.ToolDefinitionIds.Count == 2), Arg.Any<CancellationToken>())
                .Returns(Result.Success());

            // Act
            var actionResult = await _agentToolsController.AssignAgentTools(agentId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            Assert.True(response.Success);
        }
    }
}
