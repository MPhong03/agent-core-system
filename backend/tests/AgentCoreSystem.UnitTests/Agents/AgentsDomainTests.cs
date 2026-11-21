// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using AgentCoreSystem.Module.Agents.Domain.Enums;
using AgentCoreSystem.Module.Chat.Domain.Entities;
using AgentCoreSystem.Module.Chat.Domain.Enums;
using AgentCoreSystem.Module.Organization.Domain.Entities;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Xunit;

namespace AgentCoreSystem.UnitTests.Agents
{
    public class AgentsDomainTests
    {
        [Fact]
        public void Create_WithValidParameters_ShouldReturnSuccess()
        {
            // Act
            var result = ToolDefinition.Create(
                name: "search_orders",
                displayName: "Tìm kiếm đơn hàng",
                description: "Tra cứu danh sách đơn hàng theo từ khóa",
                type: ToolType.RestApi,
                endpointUrl: "https://api.example.com/v1/orders/search",
                httpMethod: "POST",
                headersConfig: "{\"Authorization\": \"Bearer token\"}",
                parametersSchema: "{\"type\": \"object\", \"properties\": {\"query\": {\"type\": \"string\"}}}",
                requiresConfirmation: true,
                timeoutSeconds: 45);

            // Assert
            Assert.True(result.IsSuccess);
            var tool = result.Value;
            Assert.NotEqual(Guid.Empty, tool.Id);
            Assert.Equal(7, tool.Id.Version);
            Assert.Equal("search_orders", tool.Name);
            Assert.Equal("Tìm kiếm đơn hàng", tool.DisplayName);
            Assert.Equal("Tra cứu danh sách đơn hàng theo từ khóa", tool.Description);
            Assert.Equal(ToolType.RestApi, tool.Type);
            Assert.Equal("https://api.example.com/v1/orders/search", tool.EndpointUrl);
            Assert.Equal("POST", tool.HttpMethod);
            Assert.Equal("{\"Authorization\": \"Bearer token\"}", tool.HeadersConfig);
            Assert.Equal("{\"type\": \"object\", \"properties\": {\"query\": {\"type\": \"string\"}}}", tool.ParametersSchema);
            Assert.True(tool.RequiresConfirmation);
            Assert.Equal(45, tool.TimeoutSeconds);
            Assert.True(tool.IsActive);
            Assert.False(tool.IsDeleted);
        }

        [Theory]
        [InlineData("invalid name with spaces")]
        [InlineData("tool@special!")]
        [InlineData("name#hash")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_WithInvalidNameRegex_ShouldFailValidation(string? invalidName)
        {
            // Act
            var result = ToolDefinition.Create(
                name: invalidName!,
                displayName: "Tool",
                description: "Description",
                type: ToolType.RestApi,
                endpointUrl: "https://api.example.com/tool",
                httpMethod: "GET");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Error.Type);
            Assert.Equal("ToolDefinition.InvalidName", result.Error.Code);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_WithEmptyEndpointUrl_ShouldFailValidation(string? emptyEndpoint)
        {
            // Act
            var result = ToolDefinition.Create(
                name: "valid_tool",
                displayName: "Tool",
                description: "Description",
                type: ToolType.RestApi,
                endpointUrl: emptyEndpoint!,
                httpMethod: "GET");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Error.Type);
            Assert.Equal("ToolDefinition.EndpointRequired", result.Error.Code);
        }

        [Theory]
        [InlineData("INVALID")]
        [InlineData("FETCH")]
        [InlineData("HEAD")]
        [InlineData("")]
        public void Create_WithInvalidHttpMethod_ShouldFailValidation(string invalidMethod)
        {
            // Act
            var result = ToolDefinition.Create(
                name: "valid_tool",
                displayName: "Tool",
                description: "Description",
                type: ToolType.RestApi,
                endpointUrl: "https://api.example.com/tool",
                httpMethod: invalidMethod);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Error.Type);
            Assert.Equal("ToolDefinition.InvalidHttpMethod", result.Error.Code);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Create_WithInvalidTimeout_ShouldFailValidation(int invalidTimeout)
        {
            // Act
            var result = ToolDefinition.Create(
                name: "valid_tool",
                displayName: "Tool",
                description: "Description",
                type: ToolType.RestApi,
                endpointUrl: "https://api.example.com/tool",
                httpMethod: "GET",
                timeoutSeconds: invalidTimeout);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Error.Type);
            Assert.Equal("ToolDefinition.InvalidTimeout", result.Error.Code);
        }

        [Fact]
        public void AgentProfile_ToolExecutionMode_ShouldDefaultToSemiAuto()
        {
            // Act
            var agent = new AgentProfile(
                "Researcher",
                "https://cdn.example.com/bot.png",
                ModelProviderType.Gemini,
                "gemini-2.5-flash",
                "System prompt");

            // Assert
            Assert.Equal(ToolExecutionMode.SemiAuto, agent.ToolExecutionMode);
        }

        [Fact]
        public void AgentProfile_ToolExecutionMode_CanBeSetExplicitly()
        {
            // Act
            var agentAuto = new AgentProfile(
                "AutoBot",
                null,
                ModelProviderType.OpenAI,
                "gpt-4o",
                "System prompt",
                0.5m,
                null,
                ToolExecutionMode.Auto);

            var agentNeedApprove = new AgentProfile(
                "SecureBot",
                null,
                ModelProviderType.Claude,
                "claude-3-7-sonnet",
                "System prompt",
                0.2m,
                null,
                ToolExecutionMode.NeedApprove);

            // Assert
            Assert.Equal(ToolExecutionMode.Auto, agentAuto.ToolExecutionMode);
            Assert.Equal(ToolExecutionMode.NeedApprove, agentNeedApprove.ToolExecutionMode);
        }

        [Fact]
        public void PendingToolCall_ShouldInitializeAndTransitionStatusCorrectly()
        {
            // Arrange
            var channelId = Guid.CreateVersion7();
            var messageId = Guid.CreateVersion7();
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

            // Act
            var pendingCall = new PendingToolCall(
                channelId,
                messageId,
                "call_abc123",
                "search_orders",
                "{\"query\": \"laptop\"}",
                expiresAt);

            // Assert
            Assert.NotEqual(Guid.Empty, pendingCall.Id);
            Assert.Equal(7, pendingCall.Id.Version);
            Assert.Equal(channelId, pendingCall.ChannelId);
            Assert.Equal(messageId, pendingCall.ChatMessageId);
            Assert.Equal("call_abc123", pendingCall.ToolCallId);
            Assert.Equal("search_orders", pendingCall.ToolName);
            Assert.Equal("{\"query\": \"laptop\"}", pendingCall.ArgumentsJson);
            Assert.Equal(PendingToolCallStatus.Pending, pendingCall.Status);
            Assert.Equal(expiresAt, pendingCall.ExpiresAt);

            // Transitions
            pendingCall.Confirm();
            Assert.Equal(PendingToolCallStatus.Confirmed, pendingCall.Status);

            pendingCall.Reject();
            Assert.Equal(PendingToolCallStatus.Rejected, pendingCall.Status);

            pendingCall.Expire();
            Assert.Equal(PendingToolCallStatus.Expired, pendingCall.Status);
        }

        [Fact]
        public void AgentToolAccess_ShouldInitializeWithCorrectPropertiesAndSupportToggle()
        {
            // Arrange
            var agentProfileId = Guid.CreateVersion7();
            var toolDefinitionId = Guid.CreateVersion7();

            // Act
            var access = new AgentToolAccess(agentProfileId, toolDefinitionId, isActive: true);

            // Assert
            Assert.NotEqual(Guid.Empty, access.Id);
            Assert.Equal(7, access.Id.Version);
            Assert.Equal(agentProfileId, access.AgentProfileId);
            Assert.Equal(toolDefinitionId, access.ToolDefinitionId);
            Assert.True(access.IsActive);

            access.Deactivate();
            Assert.False(access.IsActive);

            access.Activate();
            Assert.True(access.IsActive);
        }

        [Fact]
        public void ToolExecutionLog_ShouldInitializeWithCorrectProperties()
        {
            // Arrange
            var toolDefId = Guid.CreateVersion7();
            var agentId = Guid.CreateVersion7();
            var sourceId = Guid.CreateVersion7();

            // Act
            var log = new ToolExecutionLog(
                toolDefinitionId: toolDefId,
                agentProfileId: agentId,
                callerType: ToolCallerType.Chat,
                executionSourceId: sourceId,
                inputArguments: "{\"q\":\"test\"}",
                responsePayload: "{\"status\":\"ok\"}",
                statusCode: 200,
                durationMs: 150,
                isSuccess: true,
                errorMessage: null);

            // Assert
            Assert.NotEqual(Guid.Empty, log.Id);
            Assert.Equal(7, log.Id.Version);
            Assert.Equal(toolDefId, log.ToolDefinitionId);
            Assert.Equal(agentId, log.AgentProfileId);
            Assert.Equal(ToolCallerType.Chat, log.CallerType);
            Assert.Equal(sourceId, log.ExecutionSourceId);
            Assert.Equal("{\"q\":\"test\"}", log.InputArguments);
            Assert.Equal("{\"status\":\"ok\"}", log.ResponsePayload);
            Assert.Equal(200, log.StatusCode);
            Assert.Equal(150, log.DurationMs);
            Assert.True(log.IsSuccess);
            Assert.Null(log.ErrorMessage);
        }

        [Fact]
        public void ToolDefinition_UpdateAndToggle_ShouldModifyProperties()
        {
            // Arrange
            var tool = ToolDefinition.Create(
                name: "get_weather",
                displayName: "Get Weather",
                description: "Fetch weather info",
                type: ToolType.RestApi,
                endpointUrl: "https://weather.example.com",
                httpMethod: "GET").Value;

            // Act
            var updateResult = tool.Update(
                displayName: "Get Weather V2",
                description: "Updated description",
                endpointUrl: "https://weather.example.com/v2",
                httpMethod: "POST",
                headersConfig: "{\"X-Api-Key\": \"key\"}",
                parametersSchema: "{\"city\": \"string\"}",
                requiresConfirmation: true,
                timeoutSeconds: 60);

            // Assert
            Assert.True(updateResult.IsSuccess);
            Assert.Equal("Get Weather V2", tool.DisplayName);
            Assert.Equal("Updated description", tool.Description);
            Assert.Equal("https://weather.example.com/v2", tool.EndpointUrl);
            Assert.Equal("POST", tool.HttpMethod);
            Assert.True(tool.RequiresConfirmation);
            Assert.Equal(60, tool.TimeoutSeconds);

            tool.Deactivate();
            Assert.False(tool.IsActive);
            tool.Activate();
            Assert.True(tool.IsActive);
        }
    }
}
