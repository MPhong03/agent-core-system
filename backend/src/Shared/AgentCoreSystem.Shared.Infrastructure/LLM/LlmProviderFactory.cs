// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace AgentCoreSystem.Shared.Infrastructure.LLM
{
    /// <summary>
    /// Factory implementation resolving and instantiating LLM clients based on configuration.
    /// </summary>
    /// <remarks>
    /// Enforces fail-fast configuration checks (e.g. valid BaseUrl) before instantiating clients.
    /// </remarks>
    public class LlmProviderFactory(
        IHttpClientFactory httpClientFactory,
        IOptions<LlmOptions> options) : ILLMProviderFactory
    {
        private readonly LlmOptions _options = options.Value;

        public ILLMClient GetClient(string? providerName = null)
        {
            var targetProvider = !string.IsNullOrWhiteSpace(providerName)
                ? providerName
                : _options.DefaultProvider;

            if (string.IsNullOrWhiteSpace(targetProvider))
            {
                throw new InvalidOperationException("Default LLM provider name is not configured in LlmOptions.");
            }

            if (!_options.Providers.TryGetValue(targetProvider, out var config))
            {
                throw new InvalidOperationException($"LLM provider '{targetProvider}' is not configured in LlmOptions.");
            }

            if (string.IsNullOrWhiteSpace(config.BaseUrl))
            {
                throw new InvalidOperationException($"BaseUrl for LLM provider '{targetProvider}' is not configured.");
            }

            if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException($"BaseUrl '{config.BaseUrl}' for LLM provider '{targetProvider}' is not a valid absolute URI.");
            }

            var httpClient = httpClientFactory.CreateClient(targetProvider);
            if (config.TimeoutSeconds > 0)
            {
                httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            }

            return new OpenAiCompatibleClient(httpClient, config);
        }
    }
}
