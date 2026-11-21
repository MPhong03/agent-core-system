// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace AgentCoreSystem.Shared.Infrastructure.LLM
{
    /// <summary>
    /// Định nghĩa cấu hình công cụ (Tool / Function Calling) gửi đến LLM theo đặc tả OpenAI.
    /// </summary>
    /// <param name="Name">Tên định danh của hàm/công cụ.</param>
    /// <param name="Description">Mô tả mục đích và ngữ cảnh sử dụng để LLM quyết định kích hoạt.</param>
    /// <param name="Parameters">JSON Schema định nghĩa tham số đầu vào (Dictionary, object hoặc chuỗi JSON thô).</param>
    /// <param name="Type">Loại công cụ, mặc định là "function".</param>
    /// <remarks>
    /// Tuân thủ định dạng OpenAI Tools schema: <c>[{"type": "function", "function": {"name": ..., "description": ..., "parameters": ...}}]</c>.
    /// </remarks>
    public sealed record LlmToolDefinitionPayload(
        string Name,
        string Description,
        object? Parameters = null,
        string Type = "function")
    {
        /// <summary>
        /// Chuyển đổi đối tượng sang wire format JSON tương thích với OpenAI /chat/completions payload.
        /// </summary>
        /// <returns>Đối tượng cấu trúc function tool cho serializer.</returns>
        public object ToOpenAiFormat()
        {
            object? formattedParams = Parameters;
            if (Parameters is string jsonStr && !string.IsNullOrWhiteSpace(jsonStr))
            {
                try
                {
                    formattedParams = JsonDocument.Parse(jsonStr).RootElement.Clone();
                }
                catch (JsonException)
                {
                    formattedParams = Parameters;
                }
            }

            return new
            {
                type = string.IsNullOrWhiteSpace(Type) ? "function" : Type,
                function = new
                {
                    name = Name,
                    description = Description,
                    parameters = formattedParams ?? new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>()
                    }
                }
            };
        }
    }

    /// <summary>
    /// Dữ liệu yêu cầu gọi công cụ do mô hình LLM chỉ định trong phản hồi.
    /// </summary>
    /// <param name="Id">Mã định danh duy nhất của lượt gọi tool (do LLM sinh ra, ví dụ: call_abc123).</param>
    /// <param name="Name">Tên của hàm cần gọi.</param>
    /// <param name="Arguments">Chuỗi JSON chứa các tham số do LLM điền.</param>
    /// <param name="Type">Loại tool call, mặc định "function".</param>
    /// <remarks>
    /// AI-NOTE: Id này bắt buộc phải gửi ngược lại trong message có <c>role: "tool"</c> và <c>tool_call_id: Id</c>.
    /// </remarks>
    public sealed record LlmToolCallPayload(
        string Id,
        string Name,
        string Arguments,
        string Type = "function");

    /// <summary>
    /// Kết quả phản hồi hoàn chỉnh từ LLM sau khi gửi kèm danh sách công cụ hoặc yêu cầu thông thường.
    /// </summary>
    /// <param name="Content">Nội dung văn bản phản hồi của trợ lý, hoặc null nếu chỉ yêu cầu gọi tool.</param>
    /// <param name="ToolCalls">Danh sách các yêu cầu gọi công cụ nếu LLM quyết định gọi hàm.</param>
    /// <param name="FinishReason">Lý do kết thúc của mô hình (ví dụ: "stop", "tool_calls", "length").</param>
    public sealed record LlmChatResponse(
        string? Content,
        IReadOnlyList<LlmToolCallPayload>? ToolCalls = null,
        string? FinishReason = null)
    {
        /// <summary>
        /// Kiểm tra xem phản hồi có chứa yêu cầu gọi tool nào từ LLM hay không.
        /// </summary>
        public bool HasToolCalls => ToolCalls is { Count: > 0 };
    }
}
