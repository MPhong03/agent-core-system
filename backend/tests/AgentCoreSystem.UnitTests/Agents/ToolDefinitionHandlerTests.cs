// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Common;
using AgentCoreSystem.Module.Agents.Application.Tools.Commands;
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Module.Agents.Application.Tools.Queries;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using AgentCoreSystem.Module.Agents.Domain.Enums;
using AgentCoreSystem.Module.Agents.Infrastructure.Persistence;
using AgentCoreSystem.Shared.Infrastructure.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests.Agents
{
    /// <summary>
    /// Bộ kiểm thử đơn vị cho các CQRS Handlers quản lý Tool Definition và Secret Masking.
    /// </summary>
    public class ToolDefinitionHandlerTests
    {
        private static DbContextOptions<AgentsDbContext> CreateInMemoryOptions()
        {
            return new DbContextOptionsBuilder<AgentsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        // ── 1. Create Tool Tests ──

        [Fact]
        public async Task CreateToolDefinitionCommand_WithValidJsonSchema_ShouldSucceedAndMaskHeadersInDto()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            await using var context = new AgentsDbContext(options);
            var handler = new CreateToolDefinitionCommandHandler(context);

            var headersJson = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer real_secret_token_123",
                ["X-Api-Key"] = "api_key_xyz",
                ["Content-Type"] = "application/json"
            });

            var schemaJson = JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    city = new { type = "string", description = "Tên thành phố" }
                },
                required = new[] { "city" }
            });

            var command = new CreateToolDefinitionCommand(
                Name: "get_weather",
                DisplayName: "Tra cứu thời tiết",
                Description: "Lấy dự báo thời tiết theo thành phố",
                Type: ToolType.RestApi,
                EndpointUrl: "https://api.weather.com/v1/forecast",
                HttpMethod: "GET",
                HeadersConfig: headersJson,
                ParametersSchema: schemaJson,
                RequiresConfirmation: false,
                TimeoutSeconds: 25);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("get_weather", result.Value.Name);
            Assert.Equal("https://api.weather.com/v1/forecast", result.Value.EndpointUrl);
            Assert.Equal("GET", result.Value.HttpMethod);

            // Check masked headers in DTO
            var dtoHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(result.Value.HeadersConfig);
            Assert.NotNull(dtoHeaders);
            Assert.Equal(SecretMaskingHelper.MaskedSecret, dtoHeaders["Authorization"]);
            Assert.Equal(SecretMaskingHelper.MaskedSecret, dtoHeaders["X-Api-Key"]);
            Assert.Equal("application/json", dtoHeaders["Content-Type"]);

            // Verify stored in DB with actual secrets
            var savedTool = await context.Tools.FirstOrDefaultAsync(t => t.Id == result.Value.Id);
            Assert.NotNull(savedTool);
            var dbHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(savedTool.HeadersConfig);
            Assert.NotNull(dbHeaders);
            Assert.Equal("Bearer real_secret_token_123", dbHeaders["Authorization"]);
            Assert.Equal("api_key_xyz", dbHeaders["X-Api-Key"]);
        }

        [Fact]
        public async Task CreateToolDefinitionCommand_WithInvalidParametersSchema_ShouldReturnFailure()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            await using var context = new AgentsDbContext(options);
            var handler = new CreateToolDefinitionCommandHandler(context);

            var command = new CreateToolDefinitionCommand(
                Name: "invalid_tool",
                DisplayName: "Invalid Tool",
                Description: "Test invalid JSON",
                Type: ToolType.RestApi,
                EndpointUrl: "https://api.example.com/test",
                HttpMethod: "POST",
                HeadersConfig: "{}",
                ParametersSchema: "{ invalid json format }",
                RequiresConfirmation: false,
                TimeoutSeconds: 30);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Schema", result.Error.Code, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateToolDefinitionCommand_WithDuplicateName_ShouldReturnConflictError()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var existingTool = ToolDefinition.Create("duplicate_tool", "Display", "Desc", ToolType.RestApi, "https://api.example.com", "GET").Value;
            await using (var context = new AgentsDbContext(options))
            {
                context.Tools.Add(existingTool);
                await context.SaveChangesAsync();
            }

            await using (var context = new AgentsDbContext(options))
            {
                var handler = new CreateToolDefinitionCommandHandler(context);
                var command = new CreateToolDefinitionCommand(
                    Name: "duplicate_tool",
                    DisplayName: "Duplicate",
                    Description: "Desc",
                    Type: ToolType.RestApi,
                    EndpointUrl: "https://api.example.com",
                    HttpMethod: "GET");

                // Act
                var result = await handler.Handle(command, CancellationToken.None);

                // Assert
                Assert.True(result.IsFailure);
                Assert.Contains("DuplicateName", result.Error.Code, StringComparison.OrdinalIgnoreCase);
            }
        }

        // ── 2. Update Tool Tests ──

        [Fact]
        public async Task UpdateToolDefinitionCommand_PreservingExistingSecretHeaders_WhenMaskedValuePassed()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var existingTool = ToolDefinition.Create(
                name: "search_orders",
                displayName: "Tìm kiếm đơn hàng",
                description: "Tìm kiếm đơn hàng qua API",
                type: ToolType.RestApi,
                endpointUrl: "https://api.store.com/orders",
                httpMethod: "GET",
                headersConfig: JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["Authorization"] = "Bearer super_secure_token_999",
                    ["Custom-Header"] = "old-value"
                }),
                parametersSchema: "{}",
                requiresConfirmation: true,
                timeoutSeconds: 30).Value;

            await using (var context = new AgentsDbContext(options))
            {
                context.Tools.Add(existingTool);
                await context.SaveChangesAsync();
            }

            await using (var context = new AgentsDbContext(options))
            {
                var handler = new UpdateToolDefinitionCommandHandler(context);

                // Gửi bản cập nhật với Authorization bị ẩn dạng ••••••••
                var updateHeadersJson = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["Authorization"] = SecretMaskingHelper.MaskedSecret,
                    ["Custom-Header"] = "new-value"
                });

                var updateCommand = new UpdateToolDefinitionCommand(
                    Id: existingTool.Id,
                    DisplayName: "Tìm kiếm đơn hàng v2",
                    Description: "Mô tả mới",
                    EndpointUrl: "https://api.store.com/orders/v2",
                    HttpMethod: "GET",
                    HeadersConfig: updateHeadersJson,
                    ParametersSchema: "{}",
                    RequiresConfirmation: false,
                    TimeoutSeconds: 45);

                // Act
                var result = await handler.Handle(updateCommand, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal("Tìm kiếm đơn hàng v2", result.Value.DisplayName);

                // DTO vẫn hiển thị masked
                var dtoHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(result.Value.HeadersConfig);
                Assert.NotNull(dtoHeaders);
                Assert.Equal(SecretMaskingHelper.MaskedSecret, dtoHeaders["Authorization"]);
                Assert.Equal("new-value", dtoHeaders["Custom-Header"]);

                // Kiểm tra DB: Phải giữ nguyên secret ban đầu, không bị ghi đè bởi chuỗi mask
                var updatedInDb = await context.Tools.FirstAsync(t => t.Id == existingTool.Id);
                var dbHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(updatedInDb.HeadersConfig);
                Assert.NotNull(dbHeaders);
                Assert.Equal("Bearer super_secure_token_999", dbHeaders["Authorization"]);
                Assert.Equal("new-value", dbHeaders["Custom-Header"]);
            }
        }

        [Fact]
        public async Task UpdateToolDefinitionCommand_OverwritingSecret_WhenNewSecretPassed()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var existingTool = ToolDefinition.Create(
                name: "api_service",
                displayName: "API Service",
                description: "Test API",
                type: ToolType.RestApi,
                endpointUrl: "https://api.store.com/api",
                httpMethod: "POST",
                headersConfig: "{\"ApiKey\":\"old_secret_123\"}").Value;

            await using (var context = new AgentsDbContext(options))
            {
                context.Tools.Add(existingTool);
                await context.SaveChangesAsync();
            }

            await using (var context = new AgentsDbContext(options))
            {
                var handler = new UpdateToolDefinitionCommandHandler(context);
                var updateCommand = new UpdateToolDefinitionCommand(
                    Id: existingTool.Id,
                    DisplayName: "API Service Updated",
                    Description: "Updated desc",
                    EndpointUrl: "https://api.store.com/api",
                    HttpMethod: "POST",
                    HeadersConfig: "{\"ApiKey\":\"brand_new_secret_456\"}",
                    ParametersSchema: "{}",
                    RequiresConfirmation: false,
                    TimeoutSeconds: 30);

                // Act
                var result = await handler.Handle(updateCommand, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                var updatedInDb = await context.Tools.FirstAsync(t => t.Id == existingTool.Id);
                var dbHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(updatedInDb.HeadersConfig);
                Assert.NotNull(dbHeaders);
                Assert.Equal("brand_new_secret_456", dbHeaders["ApiKey"]);
            }
        }

        [Fact]
        public async Task UpdateToolDefinitionCommand_WhenNotFound_ShouldReturnNotFoundError()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            await using var context = new AgentsDbContext(options);
            var handler = new UpdateToolDefinitionCommandHandler(context);
            var command = new UpdateToolDefinitionCommand(
                Id: Guid.NewGuid(),
                DisplayName: "Not Found",
                Description: "Desc",
                EndpointUrl: "https://api.example.com",
                HttpMethod: "GET");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("NotFound", result.Error.Code, StringComparison.OrdinalIgnoreCase);
        }

        // ── 3. Delete Tool Tests ──

        [Fact]
        public async Task DeleteToolDefinitionCommand_WhenNotFound_ShouldReturnNotFoundError()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            await using var context = new AgentsDbContext(options);
            var handler = new DeleteToolDefinitionCommandHandler(context);
            var command = new DeleteToolDefinitionCommand(Guid.NewGuid());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("NotFound", result.Error.Code, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteToolDefinitionCommand_ShouldSoftDeleteTool()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var tool = ToolDefinition.Create(
                name: "tool_to_delete",
                displayName: "Delete Me",
                description: "Will be deleted",
                type: ToolType.RestApi,
                endpointUrl: "https://api.example.com",
                httpMethod: "DELETE").Value;

            await using (var context = new AgentsDbContext(options))
            {
                context.Tools.Add(tool);
                await context.SaveChangesAsync();
            }

            await using (var context = new AgentsDbContext(options))
            {
                var handler = new DeleteToolDefinitionCommandHandler(context);
                var command = new DeleteToolDefinitionCommand(tool.Id);

                // Act
                var result = await handler.Handle(command, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                var deletedInDb = await context.Tools.IgnoreQueryFilters().FirstAsync(t => t.Id == tool.Id);
                Assert.False(deletedInDb.IsActive);
                Assert.True(deletedInDb.IsDeleted);
                Assert.NotNull(deletedInDb.DeletedDate);
            }
        }

        // ── 4. Test Tool Execution Tests ──

        [Fact]
        public async Task TestToolDefinitionCommand_ShouldCallRestApiExecutorWithUnmaskedSecrets()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var tool = ToolDefinition.Create(
                name: "test_call_tool",
                displayName: "Test Call",
                description: "Execute test API call",
                type: ToolType.RestApi,
                endpointUrl: "https://api.example.com/data/{id}",
                httpMethod: "GET",
                headersConfig: "{\"Authorization\":\"Bearer secret_key_abc\"}",
                timeoutSeconds: 15).Value;

            await using (var context = new AgentsDbContext(options))
            {
                context.Tools.Add(tool);
                await context.SaveChangesAsync();
            }

            var mockExecutor = Substitute.For<IRestApiExecutor>();
            mockExecutor.ExecuteAsync(Arg.Any<RestApiExecutionRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(RestApiExecutionResult.Success(200, "{\"success\":true,\"value\":42}", 120)));

            await using (var context = new AgentsDbContext(options))
            {
                var handler = new TestToolDefinitionCommandHandler(context, mockExecutor);
                var command = new TestToolDefinitionCommand(
                    ToolDefinitionId: tool.Id,
                    Arguments: new Dictionary<string, object?> { ["id"] = "item_123" });

                // Act
                var result = await handler.Handle(command, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.True(result.Value.IsSuccess);
                Assert.Equal(200, result.Value.StatusCode);
                Assert.Equal("{\"success\":true,\"value\":42}", result.Value.ResponsePayload);
                Assert.Equal(120, result.Value.DurationMs);

                // Verify executor called with real unmasked token
                await mockExecutor.Received(1).ExecuteAsync(
                    Arg.Is<RestApiExecutionRequest>(r =>
                        r.Url == "https://api.example.com/data/{id}" &&
                        r.Method == "GET" &&
                        r.Headers != null &&
                        r.Headers["Authorization"] == "Bearer secret_key_abc" &&
                        r.Arguments != null &&
                        r.Arguments["id"]!.ToString() == "item_123" &&
                        r.TimeoutSeconds == 15),
                    Arg.Any<CancellationToken>());
            }
        }

        [Fact]
        public async Task TestToolDefinitionCommand_WhenToolNotFound_ShouldReturnNotFoundError()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            await using var context = new AgentsDbContext(options);
            var mockExecutor = Substitute.For<IRestApiExecutor>();
            var handler = new TestToolDefinitionCommandHandler(context, mockExecutor);

            var command = new TestToolDefinitionCommand(ToolDefinitionId: Guid.NewGuid());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("NotFound", result.Error.Code, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task TestToolDefinitionCommand_AdHoc_WithoutSavedTool_ShouldCallExecutor()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            await using var context = new AgentsDbContext(options);
            var mockExecutor = Substitute.For<IRestApiExecutor>();
            mockExecutor.ExecuteAsync(Arg.Any<RestApiExecutionRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(RestApiExecutionResult.Success(201, "{\"created\":true}", 80)));

            var handler = new TestToolDefinitionCommandHandler(context, mockExecutor);
            var command = new TestToolDefinitionCommand(
                EndpointUrl: "https://api.external.com/items",
                HttpMethod: "POST",
                HeadersConfig: "{\"X-Api-Key\":\"secret_123\"}",
                ArgumentsJson: "{\"name\":\"Widget\"}",
                TimeoutSeconds: 20);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.Value.StatusCode);
            Assert.Equal(80, result.Value.DurationMs);

            await mockExecutor.Received(1).ExecuteAsync(
                Arg.Is<RestApiExecutionRequest>(r =>
                    r.Url == "https://api.external.com/items" &&
                    r.Method == "POST" &&
                    r.Headers != null &&
                    r.Headers["X-Api-Key"] == "secret_123" &&
                    r.TimeoutSeconds == 20),
                Arg.Any<CancellationToken>());
        }

        // ── 5. Assign Agent Tools Tests ──

        [Fact]
        public async Task AssignAgentToolsCommand_ShouldSynchronizeAgentToolPermissions()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var tool1 = ToolDefinition.Create("tool_1", "Tool 1", "Desc", ToolType.RestApi, "https://api.example.com/1", "GET").Value;
            var tool2 = ToolDefinition.Create("tool_2", "Tool 2", "Desc", ToolType.RestApi, "https://api.example.com/2", "GET").Value;
            var tool3 = ToolDefinition.Create("tool_3", "Tool 3", "Desc", ToolType.RestApi, "https://api.example.com/3", "GET").Value;

            var agentProfileId = Guid.NewGuid();
            var existingAccess = new AgentToolAccess(agentProfileId, tool1.Id, isActive: true);

            await using (var context = new AgentsDbContext(options))
            {
                context.Tools.AddRange(tool1, tool2, tool3);
                context.AgentToolAccesses.Add(existingAccess);
                await context.SaveChangesAsync();
            }

            await using (var context = new AgentsDbContext(options))
            {
                var handler = new AssignAgentToolsCommandHandler(context);

                // Gán tool 2 và tool 3, bỏ tool 1
                var command = new AssignAgentToolsCommand(agentProfileId, [tool2.Id, tool3.Id]);

                // Act
                var result = await handler.Handle(command, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);

                var accesses = await context.AgentToolAccesses
                    .Where(a => a.AgentProfileId == agentProfileId)
                    .ToListAsync();

                var activeAccesses = accesses.Where(a => a.IsActive).ToList();
                Assert.Equal(2, activeAccesses.Count);
                Assert.Contains(activeAccesses, a => a.ToolDefinitionId == tool2.Id);
                Assert.Contains(activeAccesses, a => a.ToolDefinitionId == tool3.Id);

                // tool 1 phải bị vô hiệu hóa
                var tool1Access = accesses.First(a => a.ToolDefinitionId == tool1.Id);
                Assert.False(tool1Access.IsActive);
            }
        }

        // ── 6. Query Tests ──

        [Fact]
        public async Task GetToolDefinitionsQuery_ShouldReturnToolsWithMaskedHeaders()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var tool1 = ToolDefinition.Create(
                name: "tool_alpha",
                displayName: "Alpha",
                description: "Desc Alpha",
                type: ToolType.RestApi,
                endpointUrl: "https://api.alpha.com",
                httpMethod: "GET",
                headersConfig: "{\"Authorization\":\"Bearer alpha_secret\",\"Custom\":\"keep\"}").Value;

            var tool2 = ToolDefinition.Create(
                name: "tool_beta",
                displayName: "Beta",
                description: "Desc Beta",
                type: ToolType.RestApi,
                endpointUrl: "https://api.beta.com",
                httpMethod: "POST",
                headersConfig: "{\"X-Api-Key\":\"beta_secret\"}").Value;

            await using (var context = new AgentsDbContext(options))
            {
                context.Tools.AddRange(tool1, tool2);
                await context.SaveChangesAsync();
            }

            await using (var context = new AgentsDbContext(options))
            {
                var handler = new GetToolDefinitionsQueryHandler(context);
                var query = new GetToolDefinitionsQuery();

                // Act
                var result = await handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(2, result.Value.Count);

                var alphaDto = result.Value.First(t => t.Name == "tool_alpha");
                var alphaHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(alphaDto.HeadersConfig);
                Assert.NotNull(alphaHeaders);
                Assert.Equal(SecretMaskingHelper.MaskedSecret, alphaHeaders["Authorization"]);
                Assert.Equal("keep", alphaHeaders["Custom"]);

                var betaDto = result.Value.First(t => t.Name == "tool_beta");
                var betaHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(betaDto.HeadersConfig);
                Assert.NotNull(betaHeaders);
                Assert.Equal(SecretMaskingHelper.MaskedSecret, betaHeaders["X-Api-Key"]);
            }
        }

        [Fact]
        public async Task GetToolDefinitionByIdQuery_ShouldReturnToolWithMaskedHeaders()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var tool = ToolDefinition.Create(
                name: "single_tool",
                displayName: "Single",
                description: "Single Desc",
                type: ToolType.RestApi,
                endpointUrl: "https://api.example.com",
                httpMethod: "GET",
                headersConfig: "{\"Token\":\"sensitive_token_value\"}").Value;

            await using (var context = new AgentsDbContext(options))
            {
                context.Tools.Add(tool);
                await context.SaveChangesAsync();
            }

            await using (var context = new AgentsDbContext(options))
            {
                var handler = new GetToolDefinitionByIdQueryHandler(context);
                var query = new GetToolDefinitionByIdQuery(tool.Id);

                // Act
                var result = await handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal("single_tool", result.Value.Name);
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(result.Value.HeadersConfig);
                Assert.NotNull(headers);
                Assert.Equal(SecretMaskingHelper.MaskedSecret, headers["Token"]);
            }
        }

        [Fact]
        public async Task GetToolDefinitionByIdQuery_WhenNotFound_ShouldReturnNotFoundError()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            await using var context = new AgentsDbContext(options);
            var handler = new GetToolDefinitionByIdQueryHandler(context);
            var query = new GetToolDefinitionByIdQuery(Guid.NewGuid());

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("NotFound", result.Error.Code, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetAgentToolsQuery_ShouldReturnAssignedActiveToolsForAgent()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            var tool1 = ToolDefinition.Create("assigned_tool", "Assigned", "Desc", ToolType.RestApi, "https://api.example.com/1", "GET").Value;
            var tool2 = ToolDefinition.Create("unassigned_tool", "Unassigned", "Desc", ToolType.RestApi, "https://api.example.com/2", "GET").Value;
            var tool3 = ToolDefinition.Create("inactive_tool", "Inactive", "Desc", ToolType.RestApi, "https://api.example.com/3", "GET").Value;
            tool3.Deactivate();

            var agentProfileId = Guid.NewGuid();
            var access1 = new AgentToolAccess(agentProfileId, tool1.Id, isActive: true);
            var access3 = new AgentToolAccess(agentProfileId, tool3.Id, isActive: true);

            await using (var context = new AgentsDbContext(options))
            {
                context.Tools.AddRange(tool1, tool2, tool3);
                context.AgentToolAccesses.AddRange(access1, access3);
                await context.SaveChangesAsync();
            }

            await using (var context = new AgentsDbContext(options))
            {
                var handler = new GetAgentToolsQueryHandler(context);
                var query = new GetAgentToolsQuery(agentProfileId);

                // Act
                var result = await handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Single(result.Value);
                Assert.Equal("assigned_tool", result.Value[0].Name);
            }
        }

        // ── 7. Secret Masking Helper Tests ──

        [Theory]
        [InlineData("Authorization")]
        [InlineData("authorization")]
        [InlineData("AUTHORIZATION")]
        [InlineData("ApiKey")]
        [InlineData("apikey")]
        [InlineData("X-Api-Key")]
        [InlineData("x-api-key")]
        [InlineData("Token")]
        [InlineData("Secret")]
        [InlineData("X-Auth-Token")]
        public void SecretMaskingHelper_IsSensitiveHeader_ShouldDetectSensitiveNames(string headerName)
        {
            Assert.True(SecretMaskingHelper.IsSensitiveHeader(headerName));
        }

        [Theory]
        [InlineData("Content-Type")]
        [InlineData("Accept")]
        [InlineData("User-Agent")]
        [InlineData("Cache-Control")]
        public void SecretMaskingHelper_IsSensitiveHeader_ShouldNotDetectNormalHeaders(string headerName)
        {
            Assert.False(SecretMaskingHelper.IsSensitiveHeader(headerName));
        }

        [Fact]
        public void SecretMaskingHelper_MergeHeaders_ShouldRetainOriginalSecrets()
        {
            var existingHeadersJson = "{\"Authorization\":\"Bearer secret_key\",\"Accept\":\"application/json\"}";
            var incomingHeadersJson = "{\"Authorization\":\"••••••••\",\"Accept\":\"text/plain\",\"X-New\":\"added\"}";

            var mergedJson = SecretMaskingHelper.MergeHeadersJson(existingHeadersJson, incomingHeadersJson);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(mergedJson);

            Assert.NotNull(dict);
            Assert.Equal("Bearer secret_key", dict["Authorization"]);
            Assert.Equal("text/plain", dict["Accept"]);
            Assert.Equal("added", dict["X-New"]);
        }
    }
}
