// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Net;
using System.Text;
using AgentCoreSystem.Shared.Infrastructure.LLM;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AgentCoreSystem.UnitTests.LLM
{
    public class LlmClientTests
    {
        [Fact]
        public void LlmOptions_ShouldNotHaveHardcodedEndpointsOrNames_ByDefault()
        {
            var options = new LlmOptions();
            var config = new LlmProviderConfig();

            Assert.DoesNotContain("localhost:20128", options.DefaultProvider, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("9router", options.DefaultProvider, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("localhost:20128", config.BaseUrl, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("9router", config.BaseUrl, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task StreamChatAsync_ShouldYieldChunks_WhenSseStreamIsValid()
        {
            var sseContent = 
                "data: {\"choices\":[{\"delta\":{\"content\":\"Hello\"}}]}\n\n" +
                "data: {\"choices\":[{\"delta\":{\"content\":\" World\"}}]}\n\n" +
                "data: [DONE]\n\n";

            HttpRequestMessage? capturedRequest = null;
            var handler = new TestHttpMessageHandler(async (request, cancellationToken) =>
            {
                capturedRequest = request;
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream")
                };
                return await Task.FromResult(response);
            });

            var httpClient = new HttpClient(handler);
            var config = new LlmProviderConfig
            {
                ProviderType = "OpenAiCompatible",
                BaseUrl = "http://test-provider.local/v1",
                DefaultModel = "test-model",
                TimeoutSeconds = 30
            };

            var client = new OpenAiCompatibleClient(httpClient, config);
            var messages = new List<ChatMessagePayload>
            {
                new("user", "Hi there")
            };

            var tokens = new List<string>();
            await foreach (var token in client.StreamChatAsync("test-model", messages))
            {
                tokens.Add(token);
            }

            Assert.Equal(2, tokens.Count);
            Assert.Equal("Hello", tokens[0]);
            Assert.Equal(" World", tokens[1]);
            Assert.NotNull(capturedRequest);
            Assert.Equal(HttpMethod.Post, capturedRequest.Method);
            Assert.Equal("http://test-provider.local/v1/chat/completions", capturedRequest.RequestUri?.ToString());
        }

        [Fact]
        public async Task StreamChatAsync_ShouldIncludeBearerAuthorization_WhenApiKeyIsProvided()
        {
            var sseContent = "data: {\"choices\":[{\"delta\":{\"content\":\"OK\"}}]}\n\ndata: [DONE]\n\n";
            HttpRequestMessage? capturedRequest = null;

            var handler = new TestHttpMessageHandler((request, _) =>
            {
                capturedRequest = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream")
                });
            });

            var httpClient = new HttpClient(handler);
            var config = new LlmProviderConfig
            {
                ProviderType = "OpenAiCompatible",
                BaseUrl = "http://test-provider.local/v1",
                ApiKey = "super-secret-key",
                DefaultModel = "test-model"
            };

            var client = new OpenAiCompatibleClient(httpClient, config);
            await foreach (var _ in client.StreamChatAsync("test-model", [new("user", "Hello")]))
            {
            }

            Assert.NotNull(capturedRequest);
            Assert.NotNull(capturedRequest.Headers.Authorization);
            Assert.Equal("Bearer", capturedRequest.Headers.Authorization.Scheme);
            Assert.Equal("super-secret-key", capturedRequest.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task StreamChatAsync_ShouldThrowHttpRequestException_WhenResponseStatusIsNotSuccess()
        {
            var handler = new TestHttpMessageHandler((_, _) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("{\"error\":\"Internal model crash\"}", Encoding.UTF8, "application/json")
                });
            });

            var httpClient = new HttpClient(handler);
            var config = new LlmProviderConfig
            {
                BaseUrl = "http://test-provider.local/v1",
                DefaultModel = "test-model"
            };

            var client = new OpenAiCompatibleClient(httpClient, config);

            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await foreach (var _ in client.StreamChatAsync("test-model", [new("user", "Hello")]))
                {
                }
            });
        }

        [Fact]
        public void LlmProviderFactory_ShouldReturnClient_WhenProviderIsConfigured()
        {
            var options = Options.Create(new LlmOptions
            {
                DefaultProvider = "Primary",
                Providers = new Dictionary<string, LlmProviderConfig>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Primary"] = new()
                    {
                        BaseUrl = "http://primary-gateway.local/v1",
                        DefaultModel = "primary-model"
                    }
                }
            });

            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient("Primary").Returns(new HttpClient(new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))));

            var factory = new LlmProviderFactory(httpClientFactory, options);
            var client = factory.GetClient();

            Assert.NotNull(client);
            Assert.IsType<OpenAiCompatibleClient>(client);
        }

        [Fact]
        public void LlmProviderFactory_ShouldThrowInvalidOperationException_WhenProviderNotFound()
        {
            var options = Options.Create(new LlmOptions
            {
                DefaultProvider = "NonExistent",
                Providers = []
            });

            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var factory = new LlmProviderFactory(httpClientFactory, options);

            var ex = Assert.Throws<InvalidOperationException>(() => factory.GetClient());
            Assert.Contains("NonExistent", ex.Message);
        }

        [Fact]
        public void LlmProviderFactory_ShouldThrowInvalidOperationException_WhenBaseUrlIsMissing()
        {
            var options = Options.Create(new LlmOptions
            {
                DefaultProvider = "EmptyUrl",
                Providers = new Dictionary<string, LlmProviderConfig>(StringComparer.OrdinalIgnoreCase)
                {
                    ["EmptyUrl"] = new()
                    {
                        BaseUrl = "",
                        DefaultModel = "model"
                    }
                }
            });

            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var factory = new LlmProviderFactory(httpClientFactory, options);

            var ex = Assert.Throws<InvalidOperationException>(() => factory.GetClient());
            Assert.Contains("BaseUrl", ex.Message);
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
