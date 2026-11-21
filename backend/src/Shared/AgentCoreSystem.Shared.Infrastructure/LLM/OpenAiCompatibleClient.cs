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
    /// LLM client adapter giao tiếp với các API tương thích chuẩn OpenAI (OpenAI, Ollama, LocalAI, v.v.).
    /// </summary>
    /// <remarks>
    /// Kết nối qua HTTP POST tới endpoint <c>/chat/completions</c>.
    /// Hỗ trợ cả hai chế độ: Streaming Server-Sent Events (SSE) và Function / Tool Calling đồng bộ.
    /// Hoàn toàn độc lập với các thư viện SDK vendor bên thứ ba.
    /// </remarks>
    public class OpenAiCompatibleClient(HttpClient httpClient, LlmProviderConfig config) : ILLMClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <inheritdoc />
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

            var requestUrl = BuildRequestUrl();

            var payload = new Dictionary<string, object?>
            {
                ["model"] = modelToUse,
                ["messages"] = messages.Select(FormatMessageForOpenAi).ToList(),
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

        /// <inheritdoc />
        public async Task<LlmChatResponse> ChatWithToolsAsync(
            string model,
            IEnumerable<ChatMessagePayload> messages,
            IEnumerable<LlmToolDefinitionPayload>? tools = null,
            decimal? temperature = null,
            CancellationToken ct = default)
        {
            var modelToUse = !string.IsNullOrWhiteSpace(model) ? model : config.DefaultModel;
            if (string.IsNullOrWhiteSpace(modelToUse))
            {
                throw new ArgumentException("Model name must be specified or configured as default.", nameof(model));
            }

            var requestUrl = BuildRequestUrl();

            var payload = new Dictionary<string, object?>
            {
                ["model"] = modelToUse,
                ["messages"] = messages.Select(FormatMessageForOpenAi).ToList(),
                ["stream"] = false
            };

            if (temperature.HasValue)
            {
                payload["temperature"] = temperature.Value;
            }

            if (tools != null)
            {
                var toolsList = tools.Select(t => t.ToOpenAiFormat()).ToList();
                if (toolsList.Count > 0)
                {
                    payload["tools"] = toolsList;
                }
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(config.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            }

            using var response = await httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"OpenAI-compatible LLM request failed with status code {response.StatusCode} ({(int)response.StatusCode}): {errorContent}",
                    null,
                    response.StatusCode);
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            using var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                return new LlmChatResponse(null, null, null);
            }

            var firstChoice = choices[0];
            string? finishReason = null;
            if (firstChoice.TryGetProperty("finish_reason", out var finishReasonProp) &&
                finishReasonProp.ValueKind == JsonValueKind.String)
            {
                finishReason = finishReasonProp.GetString();
            }

            string? content = null;
            List<LlmToolCallPayload>? toolCalls = null;

            if (firstChoice.TryGetProperty("message", out var messageProp))
            {
                if (messageProp.TryGetProperty("content", out var contentProp) &&
                    contentProp.ValueKind == JsonValueKind.String)
                {
                    content = contentProp.GetString();
                }

                if (messageProp.TryGetProperty("tool_calls", out var toolCallsProp) &&
                    toolCallsProp.ValueKind == JsonValueKind.Array)
                {
                    toolCalls = new List<LlmToolCallPayload>();
                    foreach (var tcElem in toolCallsProp.EnumerateArray())
                    {
                        var id = tcElem.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String
                            ? idProp.GetString() ?? string.Empty
                            : string.Empty;

                        var type = tcElem.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String
                            ? typeProp.GetString() ?? "function"
                            : "function";

                        var name = string.Empty;
                        var arguments = string.Empty;

                        if (tcElem.TryGetProperty("function", out var funcProp))
                        {
                            if (funcProp.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                            {
                                name = nameProp.GetString() ?? string.Empty;
                            }

                            if (funcProp.TryGetProperty("arguments", out var argsProp))
                            {
                                arguments = argsProp.ValueKind == JsonValueKind.String
                                    ? argsProp.GetString() ?? string.Empty
                                    : argsProp.GetRawText();
                            }
                        }

                        toolCalls.Add(new LlmToolCallPayload(id, name, arguments, type));
                    }
                }
            }

            return new LlmChatResponse(content, toolCalls, finishReason);
        }

        private string BuildRequestUrl()
        {
            var baseUrl = config.BaseUrl.TrimEnd('/');
            return baseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
                ? baseUrl
                : $"{baseUrl}/chat/completions";
        }

        private static object FormatMessageForOpenAi(ChatMessagePayload m)
        {
            var msg = new Dictionary<string, object?>
            {
                ["role"] = m.Role,
                ["content"] = m.Content
            };

            if (!string.IsNullOrEmpty(m.ToolCallId))
            {
                msg["tool_call_id"] = m.ToolCallId;
            }

            if (m.ToolCalls != null && m.ToolCalls.Count > 0)
            {
                msg["tool_calls"] = m.ToolCalls.Select(tc => new
                {
                    id = tc.Id,
                    type = string.IsNullOrWhiteSpace(tc.Type) ? "function" : tc.Type,
                    function = new
                    {
                        name = tc.Name,
                        arguments = tc.Arguments
                    }
                }).ToList();
            }

            return msg;
        }
    }
}
