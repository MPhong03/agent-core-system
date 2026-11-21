// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Shared.Infrastructure.LLM
{
    /// <summary>
    /// Dữ liệu đại diện cho một tin nhắn trong phiên hội thoại gửi tới LLM.
    /// </summary>
    /// <param name="Role">Vai trò của tin nhắn (system, user, assistant, tool).</param>
    /// <param name="Content">Nội dung văn bản tin nhắn (có thể null nếu trợ lý chỉ kích hoạt tool calls).</param>
    /// <param name="ToolCallId">Mã định danh tool call tương ứng khi vai trò là tool (role: "tool").</param>
    /// <param name="ToolCalls">Danh sách yêu cầu gọi tool khi vai trò là trợ lý (role: "assistant").</param>
    public sealed record ChatMessagePayload(
        string Role,
        string? Content,
        string? ToolCallId = null,
        IReadOnlyList<LlmToolCallPayload>? ToolCalls = null);

    /// <summary>
    /// Trừu tượng hóa giao tiếp với nhà cung cấp dịch vụ LLM (OpenAI-compatible).
    /// </summary>
    /// <remarks>
    /// Được hiện thực hóa bởi các adapter trung lập (như <see cref="OpenAiCompatibleClient"/>)
    /// hỗ trợ cả cơ chế streaming SSE và cơ chế gọi công cụ (Tool / Function Calling).
    /// </remarks>
    public interface ILLMClient
    {
        /// <summary>
        /// Truyền trực tiếp luồng token văn bản của phản hồi chat completion qua IAsyncEnumerable.
        /// </summary>
        /// <param name="model">Tên mô hình LLM cần sử dụng.</param>
        /// <param name="messages">Danh sách tin nhắn lịch sử trò chuyện.</param>
        /// <param name="temperature">Tham số nhiệt độ ngẫu nhiên.</param>
        /// <param name="ct">Token hủy tác vụ.</param>
        /// <returns>Luồng chuỗi delta token văn bản bất đồng bộ.</returns>
        IAsyncEnumerable<string> StreamChatAsync(
            string model,
            IEnumerable<ChatMessagePayload> messages,
            decimal? temperature = null,
            CancellationToken ct = default);

        /// <summary>
        /// Gửi yêu cầu chat completion kèm danh sách công cụ (Function Calling) và nhận phản hồi toàn diện từ LLM.
        /// </summary>
        /// <param name="model">Tên mô hình LLM cần sử dụng.</param>
        /// <param name="messages">Danh sách tin nhắn lịch sử trò chuyện bao gồm user, assistant và tool responses.</param>
        /// <param name="tools">Danh sách định nghĩa công cụ LLM có quyền gọi.</param>
        /// <param name="temperature">Tham số nhiệt độ ngẫu nhiên.</param>
        /// <param name="ct">Token hủy tác vụ.</param>
        /// <returns>Đối tượng phản hồi chứa nội dung văn bản hoặc danh sách yêu cầu gọi tool.</returns>
        Task<LlmChatResponse> ChatWithToolsAsync(
            string model,
            IEnumerable<ChatMessagePayload> messages,
            IEnumerable<LlmToolDefinitionPayload>? tools = null,
            decimal? temperature = null,
            CancellationToken ct = default);
    }
}
