// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Net;
using System.Text;
using System.Text.Json;
using AgentCoreSystem.Shared.Infrastructure.LLM;

namespace AgentCoreSystem.UnitTests.LLM
{
    /// <summary>
    /// Kiểm thử tính năng gọi công cụ (OpenAI Function/Tool Calling) của ILLMClient và OpenAiCompatibleClient.
    /// </summary>
    public class LlmToolCallingTests
    {
        [Fact]
        public async Task ChatWithToolsAsync_ShouldSerializeToolsPayloadToOpenAiFormat()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;

            var responseJson = """
            {
                "id": "chatcmpl-test",
                "object": "chat.completion",
                "choices": [
                    {
                        "index": 0,
                        "message": {
                            "role": "assistant",
                            "content": "Tôi sẽ tra cứu sản phẩm cho bạn."
                        },
                        "finish_reason": "stop"
                    }
                ]
            }
            """;

            var handler = new TestHttpMessageHandler(async (request, ct) =>
            {
                capturedRequest = request;
                capturedBody = await request.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                };
            });

            var httpClient = new HttpClient(handler);
            var config = new LlmProviderConfig
            {
                ProviderType = "OpenAiCompatible",
                BaseUrl = "http://test-provider.local/v1",
                DefaultModel = "test-model"
            };

            var client = new OpenAiCompatibleClient(httpClient, config);

            var messages = new List<ChatMessagePayload>
            {
                new("user", "Tìm kiếm laptop giá dưới 20 triệu")
            };

            var tools = new List<LlmToolDefinitionPayload>
            {
                new(
                    Name: "search_products",
                    Description: "Tra cứu danh sách sản phẩm trong kho theo từ khóa và giá",
                    Parameters: new
                    {
                        type = "object",
                        properties = new
                        {
                            query = new { type = "string", description = "Từ khóa tìm kiếm" },
                            maxPrice = new { type = "number", description = "Giá tối đa" }
                        },
                        required = new[] { "query" }
                    })
            };

            // Act
            var result = await client.ChatWithToolsAsync("test-model", messages, tools);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.NotNull(capturedBody);

            using var doc = JsonDocument.Parse(capturedBody);
            var root = doc.RootElement;

            Assert.Equal("test-model", root.GetProperty("model").GetString());
            Assert.False(root.GetProperty("stream").GetBoolean());

            // Kiểm tra schema OpenAI tools: [{"type": "function", "function": {"name": ..., "description": ..., "parameters": ...}}]
            Assert.True(root.TryGetProperty("tools", out var toolsElem));
            Assert.Equal(JsonValueKind.Array, toolsElem.ValueKind);
            Assert.Equal(1, toolsElem.GetArrayLength());

            var firstTool = toolsElem[0];
            Assert.Equal("function", firstTool.GetProperty("type").GetString());

            var functionElem = firstTool.GetProperty("function");
            Assert.Equal("search_products", functionElem.GetProperty("name").GetString());
            Assert.Equal("Tra cứu danh sách sản phẩm trong kho theo từ khóa và giá", functionElem.GetProperty("description").GetString());
            Assert.True(functionElem.TryGetProperty("parameters", out var paramsElem));
            Assert.Equal("object", paramsElem.GetProperty("type").GetString());
        }

        [Fact]
        public async Task ChatWithToolsAsync_ShouldDeserializeToolCallsResponse_WhenLlmRequestsToolExecution()
        {
            // Arrange
            var responseJson = """
            {
                "id": "chatcmpl-tool-test",
                "object": "chat.completion",
                "choices": [
                    {
                        "index": 0,
                        "message": {
                            "role": "assistant",
                            "content": null,
                            "tool_calls": [
                                {
                                    "id": "call_abc123",
                                    "type": "function",
                                    "function": {
                                        "name": "search_products",
                                        "arguments": "{\"query\":\"laptop\",\"maxPrice\":20000000}"
                                    }
                                }
                            ]
                        },
                        "finish_reason": "tool_calls"
                    }
                ]
            }
            """;

            var handler = new TestHttpMessageHandler((_, _) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });
            });

            var httpClient = new HttpClient(handler);
            var config = new LlmProviderConfig
            {
                ProviderType = "OpenAiCompatible",
                BaseUrl = "http://test-provider.local/v1",
                DefaultModel = "test-model"
            };

            var client = new OpenAiCompatibleClient(httpClient, config);
            var messages = new List<ChatMessagePayload>
            {
                new("user", "Tìm laptop 20tr")
            };

            // Act
            var result = await client.ChatWithToolsAsync("test-model", messages);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.HasToolCalls);
            Assert.Equal("tool_calls", result.FinishReason);
            Assert.NotNull(result.ToolCalls);
            Assert.Single(result.ToolCalls);

            var toolCall = result.ToolCalls[0];
            Assert.Equal("call_abc123", toolCall.Id);
            Assert.Equal("function", toolCall.Type);
            Assert.Equal("search_products", toolCall.Name);
            Assert.Equal("{\"query\":\"laptop\",\"maxPrice\":20000000}", toolCall.Arguments);
        }

        [Fact]
        public async Task ChatWithToolsAsync_ShouldReturnTextContent_WhenNoToolsAreCalled()
        {
            // Arrange
            var responseJson = """
            {
                "id": "chatcmpl-text-only",
                "object": "chat.completion",
                "choices": [
                    {
                        "index": 0,
                        "message": {
                            "role": "assistant",
                            "content": "Xin chào! Tôi có thể giúp gì cho bạn?"
                        },
                        "finish_reason": "stop"
                    }
                ]
            }
            """;

            var handler = new TestHttpMessageHandler((_, _) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });
            });

            var httpClient = new HttpClient(handler);
            var config = new LlmProviderConfig
            {
                ProviderType = "OpenAiCompatible",
                BaseUrl = "http://test-provider.local/v1",
                DefaultModel = "test-model"
            };

            var client = new OpenAiCompatibleClient(httpClient, config);
            var messages = new List<ChatMessagePayload>
            {
                new("user", "Xin chào")
            };

            // Act
            var result = await client.ChatWithToolsAsync("test-model", messages);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.HasToolCalls);
            Assert.Null(result.ToolCalls);
            Assert.Equal("Xin chào! Tôi có thể giúp gì cho bạn?", result.Content);
            Assert.Equal("stop", result.FinishReason);
        }

        [Fact]
        public async Task ChatWithToolsAsync_ShouldSerializeToolResponseMessage_WhenToolRoleAndToolCallIdAreProvided()
        {
            // Arrange
            string? capturedBody = null;
            var responseJson = """
            {
                "choices": [
                    {
                        "message": {
                            "role": "assistant",
                            "content": "Đã tìm thấy 2 sản phẩm phù hợp."
                        },
                        "finish_reason": "stop"
                    }
                ]
            }
            """;

            var handler = new TestHttpMessageHandler(async (request, ct) =>
            {
                capturedBody = await request.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                };
            });

            var httpClient = new HttpClient(handler);
            var config = new LlmProviderConfig
            {
                BaseUrl = "http://test-provider.local/v1",
                DefaultModel = "test-model"
            };

            var client = new OpenAiCompatibleClient(httpClient, config);

            var messages = new List<ChatMessagePayload>
            {
                new("user", "Tìm laptop"),
                new("assistant", null, ToolCalls: [new("call_abc123", "search_products", "{\"query\":\"laptop\"}")]),
                new("tool", "[{\"name\":\"Dell Inspiron\"},{\"name\":\"HP Pavilion\"}]", ToolCallId: "call_abc123")
            };

            // Act
            var result = await client.ChatWithToolsAsync("test-model", messages);

            // Assert
            Assert.NotNull(capturedBody);
            using var doc = JsonDocument.Parse(capturedBody);
            var messagesElem = doc.RootElement.GetProperty("messages");
            Assert.Equal(3, messagesElem.GetArrayLength());

            var assistantMsg = messagesElem[1];
            Assert.Equal("assistant", assistantMsg.GetProperty("role").GetString());
            Assert.True(assistantMsg.TryGetProperty("tool_calls", out var toolCallsElem));
            Assert.Equal(1, toolCallsElem.GetArrayLength());
            Assert.Equal("call_abc123", toolCallsElem[0].GetProperty("id").GetString());

            var toolMsg = messagesElem[2];
            Assert.Equal("tool", toolMsg.GetProperty("role").GetString());
            Assert.Equal("call_abc123", toolMsg.GetProperty("tool_call_id").GetString());
            Assert.Equal("[{\"name\":\"Dell Inspiron\"},{\"name\":\"HP Pavilion\"}]", toolMsg.GetProperty("content").GetString());
        }

        [Fact]
        public async Task ChatWithToolsAsync_ShouldThrowHttpRequestException_WhenEndpointReturnsError()
        {
            // Arrange
            var handler = new TestHttpMessageHandler((_, _) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"Invalid tool schema\"}", Encoding.UTF8, "application/json")
                });
            });

            var httpClient = new HttpClient(handler);
            var config = new LlmProviderConfig
            {
                BaseUrl = "http://test-provider.local/v1",
                DefaultModel = "test-model"
            };

            var client = new OpenAiCompatibleClient(httpClient, config);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await client.ChatWithToolsAsync("test-model", [new("user", "Hello")]);
            });
        }

        private sealed class TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
            : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return handlerFunc(request, cancellationToken);
            }
        }
    }
}
