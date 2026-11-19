// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Api.Extensions;
using AgentCoreSystem.Module.Chat.Application.Contracts;
using AgentCoreSystem.Module.Chat.Application.Messages.DTOs;
using AgentCoreSystem.Module.Chat.Application.Messages.Queries;
using AgentCoreSystem.Module.Chat.Presentation;
using AgentCoreSystem.Module.Chat.Presentation.Controllers.v1;
using AgentCoreSystem.Shared.Kernel.Primitives;
using AgentCoreSystem.Shared.Kernel.Responses;
using AgentCoreSystem.Shared.Persistence.Interceptors;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests.Chat
{
    public class ChatControllerTests
    {
        private readonly ISender _mediator = Substitute.For<ISender>();
        private readonly ChatController _controller;

        public ChatControllerTests()
        {
            _controller = new ChatController(_mediator)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task GetChannelMessages_WhenQuerySucceeds_ShouldReturnOkWithMessages()
        {
            // Arrange
            var channelId = Guid.NewGuid();
            var messageId = Guid.NewGuid();
            var expectedDtos = new List<ChatMessageDto>
            {
                new(
                    messageId,
                    channelId,
                    Guid.NewGuid(),
                    "Test User",
                    false,
                    "Hello team!",
                    0,
                    null,
                    Array.Empty<Guid>(),
                    DateTimeOffset.UtcNow)
            };

            _mediator.Send(
                Arg.Is<GetChannelMessagesQuery>(q => q.ChannelId == channelId && q.Limit == 50 && q.BeforeMessageId == null),
                Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<ChatMessageDto>>(expectedDtos));

            // Act
            var actionResult = await _controller.GetChannelMessages(channelId, 50, null, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var apiResponse = Assert.IsType<ApiResponse<IReadOnlyList<ChatMessageDto>>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Single(apiResponse.Data);
            Assert.Equal(messageId, apiResponse.Data[0].Id);
            Assert.Equal("Hello team!", apiResponse.Data[0].Content);
        }

        [Fact]
        public async Task GetChannelMessages_WhenNotFound_ShouldReturnProblemDetailsWithNotFound()
        {
            // Arrange
            var channelId = Guid.NewGuid();
            var beforeId = Guid.NewGuid();

            _mediator.Send(
                Arg.Is<GetChannelMessagesQuery>(q => q.ChannelId == channelId && q.BeforeMessageId == beforeId),
                Arg.Any<CancellationToken>())
                .Returns(Result.Failure<IReadOnlyList<ChatMessageDto>>(
                    Error.NotFound("ChatMessage.NotFound", "Không tìm thấy tin nhắn mốc.")));

            // Act
            var actionResult = await _controller.GetChannelMessages(channelId, 20, beforeId, CancellationToken.None);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);

            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("Not Found", problemDetails.Title);
            Assert.Equal("Không tìm thấy tin nhắn mốc.", problemDetails.Detail);
            Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        }

        [Fact]
        public async Task GetChannelMessages_WhenValidationFails_ShouldReturnProblemDetailsWithBadRequest()
        {
            // Arrange
            var channelId = Guid.NewGuid();

            _mediator.Send(
                Arg.Any<GetChannelMessagesQuery>(),
                Arg.Any<CancellationToken>())
                .Returns(Result.Failure<IReadOnlyList<ChatMessageDto>>(
                    Error.Validation("ChatMessage.InvalidLimit", "Số lượng tin nhắn không hợp lệ.")));

            // Act
            var actionResult = await _controller.GetChannelMessages(channelId, -1, null, CancellationToken.None);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);

            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("Bad Request - Validation Error", problemDetails.Title);
            Assert.Equal("Số lượng tin nhắn không hợp lệ.", problemDetails.Detail);
            Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        }

        [Fact]
        public void AddChatPresentation_ShouldRegisterPresentationServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddChatPresentation();

            // Assert
            Assert.Contains(services, d => d.ServiceType.Name.Contains("IActionDescriptorCollectionProvider") || d.ServiceType.Name.Contains("ApplicationPartManager") || d.ServiceType.Name.Contains("Controller"));
        }

        [Fact]
        public void AddChatModule_ShouldRegisterApplicationInfrastructureAndPresentation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(new AuditableEntityInterceptor());

            var configDict = new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=TestDb;Trusted_Connection=True;TrustServerCertificate=True;" }
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            // Act
            services.AddChatModule(configuration);

            // Assert
            Assert.Contains(services, d => d.ServiceType == typeof(IChatDbContext));
            Assert.Contains(services, d => d.ServiceType == typeof(IMessageDispatcherService));
        }

        [Fact]
        public void MapChatHub_And_MapChatEndpoints_ShouldReturnEndpointRouteBuilder()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddRouting();
            services.AddSignalR();
            services.AddSingleton(Substitute.For<Microsoft.Extensions.Hosting.IHostApplicationLifetime>());
            var serviceProvider = services.BuildServiceProvider();

            var builder = Substitute.For<IEndpointRouteBuilder>();
            var dataSources = new List<EndpointDataSource>();
            builder.DataSources.Returns(dataSources);
            builder.ServiceProvider.Returns(serviceProvider);

            // Act
            var resultHub = builder.MapChatHub();
            var resultEndpoints = builder.MapChatEndpoints();

            // Assert
            Assert.NotNull(resultHub);
            Assert.NotNull(resultEndpoints);
            Assert.NotEmpty(builder.DataSources);
        }
    }
}
