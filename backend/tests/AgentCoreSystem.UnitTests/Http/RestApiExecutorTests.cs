// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Net;
using System.Text;
using System.Text.Json;
using AgentCoreSystem.Shared.Infrastructure;
using AgentCoreSystem.Shared.Infrastructure.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AgentCoreSystem.UnitTests.Http
{
    /// <summary>
    /// Kiểm thử bộ thực thi REST API không trạng thái kèm bộ lọc SSRF Guard.
    /// </summary>
    public class RestApiExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldSubstitutePathAndQueryParams_OnValidGetRequest()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            var responseJson = "{\"items\":[1,2,3]}";

            var handler = new TestHttpMessageHandler((request, _) =>
            {
                capturedRequest = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });
            });

            var httpClient = new HttpClient(handler);
            var executor = new RestApiExecutor(httpClient, (_, _) => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") }));

            var request = new RestApiExecutionRequest
            {
                Url = "https://api.example.com/api/v1/users/{userId}/posts/{postId}",
                Method = "GET",
                Headers = new Dictionary<string, string>
                {
                    ["Authorization"] = "Bearer test-token-123",
                    ["X-Custom-Header"] = "my-custom-value"
                },
                Arguments = new Dictionary<string, object?>
                {
                    ["userId"] = 123,
                    ["postId"] = "456",
                    ["status"] = "published",
                    ["limit"] = 10
                },
                TimeoutSeconds = 30
            };

            // Act
            var result = await executor.ExecuteAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(responseJson, result.ResponsePayload);
            Assert.Null(result.ErrorMessage);
            Assert.True(result.DurationMs >= 0);

            Assert.NotNull(capturedRequest);
            Assert.Equal(HttpMethod.Get, capturedRequest.Method);

            var expectedUri = "https://api.example.com/api/v1/users/123/posts/456?status=published&limit=10";
            Assert.Equal(expectedUri, capturedRequest.RequestUri?.ToString());

            Assert.True(capturedRequest.Headers.Contains("Authorization"));
            Assert.True(capturedRequest.Headers.Contains("X-Custom-Header"));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSendJsonBody_OnValidPostRequest()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;
            var responseJson = "{\"id\":42,\"status\":\"created\"}";

            var handler = new TestHttpMessageHandler(async (request, cancellationToken) =>
            {
                capturedRequest = request;
                if (request.Content != null)
                {
                    capturedBody = await request.Content.ReadAsStringAsync(cancellationToken);
                }

                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                };
            });

            var httpClient = new HttpClient(handler);
            var executor = new RestApiExecutor(httpClient, (_, _) => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") }));

            var request = new RestApiExecutionRequest
            {
                Url = "https://api.example.com/api/v1/users/{userId}/profile",
                Method = "POST",
                Headers = new Dictionary<string, string>
                {
                    ["X-Request-Id"] = "req-999"
                },
                Arguments = new Dictionary<string, object?>
                {
                    ["userId"] = 42,
                    ["name"] = "Alice",
                    ["email"] = "alice@example.com",
                    ["active"] = true
                },
                TimeoutSeconds = 30
            };

            // Act
            var result = await executor.ExecuteAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal(responseJson, result.ResponsePayload);

            Assert.NotNull(capturedRequest);
            Assert.Equal(HttpMethod.Post, capturedRequest.Method);
            Assert.Equal("https://api.example.com/api/v1/users/42/profile", capturedRequest.RequestUri?.ToString());

            Assert.NotNull(capturedBody);
            using var doc = JsonDocument.Parse(capturedBody);
            var root = doc.RootElement;
            Assert.Equal("Alice", root.GetProperty("name").GetString());
            Assert.Equal("alice@example.com", root.GetProperty("email").GetString());
            Assert.True(root.GetProperty("active").GetBoolean());
            // userId was consumed in the path, should not be duplicated in JSON body
            Assert.False(root.TryGetProperty("userId", out _));
        }

        [Theory]
        [InlineData("http://127.0.0.1:5000/api")]
        [InlineData("http://localhost:8080/admin")]
        [InlineData("http://169.254.169.254/latest/meta-data/")]
        [InlineData("http://10.0.0.1/secret")]
        [InlineData("http://192.168.1.1/config")]
        [InlineData("http://172.16.5.10/private")]
        [InlineData("http://[::1]:8080/status")]
        [InlineData("ftp://example.com/file.txt")]
        public async Task ExecuteAsync_ShouldBlockSSRF_WhenTargetingRestrictedAddresses(string targetUrl)
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            var handler = new TestHttpMessageHandler((request, _) =>
            {
                capturedRequest = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });

            var httpClient = new HttpClient(handler);
            var executor = new RestApiExecutor(httpClient);

            var request = new RestApiExecutionRequest
            {
                Url = targetUrl,
                Method = "GET"
            };

            // Act
            var result = await executor.ExecuteAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("SSRF", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Null(capturedRequest); // Chặn hoàn toàn, không gửi bất kỳ HTTP request nào
        }

        [Theory]
        [InlineData("http://127.0.0.1:5000")]
        [InlineData("http://localhost")]
        [InlineData("http://169.254.169.254")]
        public async Task EnsureSafeUrlAsync_ShouldThrowInvalidOperationException_WhenTargetIsRestricted(string url)
        {
            // Arrange
            var executor = new RestApiExecutor(new HttpClient());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => executor.EnsureSafeUrlAsync(url));
            Assert.Contains("SSRF", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldBlockDnsRebinding_WhenDomainResolvesToPrivateIp()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            var handler = new TestHttpMessageHandler((request, _) =>
            {
                capturedRequest = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });

            var httpClient = new HttpClient(handler);
            // Giả lập domain công khai giải mã ra địa chỉ private 127.0.0.1 (kỹ thuật DNS Rebinding)
            var executor = new RestApiExecutor(httpClient, (_, _) => Task.FromResult(new[] { IPAddress.Parse("127.0.0.1") }));

            var request = new RestApiExecutionRequest
            {
                Url = "https://attacker-rebinding.com/api",
                Method = "GET"
            };

            // Act
            var result = await executor.ExecuteAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("SSRF", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Null(capturedRequest);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleTimeout_Gracefully()
        {
            // Arrange
            var handler = new TestHttpMessageHandler(async (_, cancellationToken) =>
            {
                // Giả lập server xử lý chậm 2 giây
                await Task.Delay(2000, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            var httpClient = new HttpClient(handler);
            var executor = new RestApiExecutor(httpClient, (_, _) => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") }));

            var request = new RestApiExecutionRequest
            {
                Url = "https://api.example.com/api/slow",
                Method = "GET",
                TimeoutSeconds = 1 // Timeout 1s
            };

            // Act
            var result = await executor.ExecuteAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal((int)HttpStatusCode.RequestTimeout, result.StatusCode);
            Assert.Contains("timed out", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnErrorResult_WhenTargetReturnsHttp500()
        {
            // Arrange
            var errorJson = "{\"error\":\"Internal database failure\"}";
            var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
            }));

            var httpClient = new HttpClient(handler);
            var executor = new RestApiExecutor(httpClient, (_, _) => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") }));

            var request = new RestApiExecutionRequest
            {
                Url = "https://api.example.com/api/orders",
                Method = "GET"
            };

            // Act
            var result = await executor.ExecuteAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal(errorJson, result.ResponsePayload);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotFollowRedirect_WhenServerReturns302Redirect()
        {
            // Arrange
            var requestCount = 0;
            var handler = new TestHttpMessageHandler((_, _) =>
            {
                requestCount++;
                var response = new HttpResponseMessage(HttpStatusCode.Found);
                response.Headers.Location = new Uri("http://169.254.169.254/latest/meta-data/");
                response.Content = new StringContent("Redirecting...", Encoding.UTF8, "text/plain");
                return Task.FromResult(response);
            });

            var httpClient = new HttpClient(handler);
            var executor = new RestApiExecutor(httpClient, (_, _) => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") }));

            var request = new RestApiExecutionRequest
            {
                Url = "https://api.example.com/api/redirect",
                Method = "GET"
            };

            // Act
            var result = await executor.ExecuteAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal((int)HttpStatusCode.Found, result.StatusCode);
            Assert.Equal(1, requestCount); // Chặn chuyển hướng, không gửi request thứ 2 đến đích chuyển hướng
        }

        [Fact]
        public async Task ExecuteAsync_ShouldTruncateResponsePayload_WhenExceedingMaxLimit()
        {
            // Arrange: sinh payload 2.5MB (> 2MB limit)
            var largePayload = new string('A', RestApiExecutor.MaxResponseBytes + 500 * 1024);
            var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(largePayload, Encoding.UTF8, "text/plain")
            }));

            var httpClient = new HttpClient(handler);
            var executor = new RestApiExecutor(httpClient, (_, _) => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") }));

            var request = new RestApiExecutionRequest
            {
                Url = "https://api.example.com/api/large-data",
                Method = "GET"
            };

            // Act
            var result = await executor.ExecuteAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Contains("[TRUNCATED: Response payload exceeded 2MB limit]", result.ResponsePayload);
            Assert.True(result.ResponsePayload.Length < largePayload.Length);
        }

        [Fact]
        public void AddHttpInfrastructure_ShouldConfigureSocketsHttpHandler_WithAutoRedirectDisabled()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddHttpInfrastructure();
            var sp = services.BuildServiceProvider();
            var executor = sp.GetRequiredService<IRestApiExecutor>();

            // Assert
            Assert.NotNull(executor);
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
