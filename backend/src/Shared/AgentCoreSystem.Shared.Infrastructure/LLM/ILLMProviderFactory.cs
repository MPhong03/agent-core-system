// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Shared.Infrastructure.LLM
{
    /// <summary>
    /// Factory for resolving named or default LLM clients.
    /// </summary>
    /// <remarks>
    /// Decouples caller modules from provider configuration details and enables dynamic routing.
    /// </remarks>
    public interface ILLMProviderFactory
    {
        /// <summary>
        /// Retrieves an <see cref="ILLMClient"/> instance configured for the specified provider.
        /// </summary>
        /// <param name="providerName">Optional provider name. If null, the default provider is resolved.</param>
        /// <returns>Configured LLM client instance.</returns>
        ILLMClient GetClient(string? providerName = null);
    }
}
