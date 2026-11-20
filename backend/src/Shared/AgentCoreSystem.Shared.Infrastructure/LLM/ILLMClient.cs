// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Shared.Infrastructure.LLM
{
    /// <summary>
    /// Payload representing a single message in a chat completion conversation.
    /// </summary>
    /// <param name="Role">Role of the sender (e.g. system, user, assistant).</param>
    /// <param name="Content">Text content of the message.</param>
    public sealed record ChatMessagePayload(string Role, string Content);

    /// <summary>
    /// Abstraction for streaming chat completions from an LLM provider.
    /// </summary>
    /// <remarks>
    /// Implemented by vendor-neutral adapters (e.g. <see cref="OpenAiCompatibleClient"/>)
    /// to stream tokens asynchronously back to callers such as SignalR hubs.
    /// </remarks>
    public interface ILLMClient
    {
        /// <summary>
        /// Streams text tokens for the given chat conversation.
        /// </summary>
        /// <param name="model">Target model identifier.</param>
        /// <param name="messages">Sequence of conversation history messages.</param>
        /// <param name="temperature">Optional sampling temperature.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Asynchronous stream of response delta text chunks.</returns>
        IAsyncEnumerable<string> StreamChatAsync(
            string model,
            IEnumerable<ChatMessagePayload> messages,
            decimal? temperature = null,
            CancellationToken ct = default);
    }
}
