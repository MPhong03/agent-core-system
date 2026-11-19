// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Shared.Infrastructure.LLM
{
    /// <summary>
    /// Configuration options for LLM providers.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="ILLMProviderFactory"/> to resolve named or default LLM client configurations.
    /// Never contains hardcoded endpoints or credentials.
    /// </remarks>
    public sealed class LlmOptions
    {
        public const string SectionName = "Llm";

        public string DefaultProvider { get; set; } = string.Empty;

        public Dictionary<string, LlmProviderConfig> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Configuration details for a single LLM provider endpoint.
    /// </summary>
    /// <remarks>
    /// Provides connection and model defaults for OpenAI-compatible gateways or direct providers.
    /// </remarks>
    public sealed class LlmProviderConfig
    {
        public string ProviderType { get; set; } = "OpenAiCompatible";

        public string BaseUrl { get; set; } = string.Empty;

        public string? ApiKey { get; set; }

        public string DefaultModel { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; } = 60;
    }
}
