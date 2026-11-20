// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace AgentCoreSystem.Shared.Infrastructure.LLM
{
    /// <summary>
    /// LLM client implementation communicating with OpenAI-compatible chat completion endpoints.
    /// </summary>
    /// <remarks>
    /// Connects via HTTP POST to /chat/completions and reads Server-Sent Events (SSE) tokens asynchronously.
    /// Does not depend on vendor-specific SDKs.
    /// </remarks>
    public class OpenAiCompatibleClient(HttpClient httpClient, LlmProviderConfig config) : ILLMClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async IAsyncEnumerable<string> StreamChatAsync(
            string model,
            IEnumerable<ChatMessagePayload> messages,
            decimal? temperature = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var modelToUse = !string.IsNullOrWhiteSpace(model) ? model : config.DefaultModel;
            if (string.IsNullOrWhiteSpace(modelToUse))
            {
                throw new ArgumentException("Model name must be specified or configured as default.", nameof(model));
            }

            var baseUrl = config.BaseUrl.TrimEnd('/');
            var requestUrl = baseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
                ? baseUrl
                : $"{baseUrl}/chat/completions";

            var payload = new Dictionary<string, object?>
            {
                ["model"] = modelToUse,
                ["messages"] = messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                ["stream"] = true
            };

            if (temperature.HasValue)
            {
                payload["temperature"] = temperature.Value;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            if (!string.IsNullOrWhiteSpace(config.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            }

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"OpenAI-compatible LLM request failed with status code {response.StatusCode} ({(int)response.StatusCode}): {errorContent}",
                    null,
                    response.StatusCode);
            }

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(':'))
                {
                    continue;
                }

                if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var data = line["data:".Length..].Trim();
                if (string.Equals(data, "[DONE]", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                string? deltaContent = null;
                try
                {
                    using var jsonDoc = JsonDocument.Parse(data);
                    var root = jsonDoc.RootElement;
                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("content", out var contentElement) &&
                            contentElement.ValueKind == JsonValueKind.String)
                        {
                            deltaContent = contentElement.GetString();
                        }
                    }
                }
                catch (JsonException)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(deltaContent))
                {
                    yield return deltaContent;
                }
            }
        }
    }
}
