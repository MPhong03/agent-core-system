// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Module.Organization.Domain.Enums
{
    /// <summary>
    /// Các nhà cung cấp mô hình trí tuệ nhân tạo (LLM Providers) được hỗ trợ.
    /// </summary>
    /// <remarks>
    /// <strong>Why:</strong> Cho phép từng Agent linh hoạt lựa chọn "bộ não" phù hợp nhất với chi phí và độ phức tạp của bài toán nghiệp vụ.
    /// </remarks>
    public enum ModelProviderType
    {
        /// <summary>
        /// Google Gemini API (Flash, Pro).
        /// </summary>
        Gemini = 1,

        /// <summary>
        /// OpenAI API (GPT-4o, GPT-4o-mini).
        /// </summary>
        OpenAI = 2,

        /// <summary>
        /// Anthropic Claude API (Sonnet, Haiku).
        /// </summary>
        Claude = 3,

        /// <summary>
        /// Mô hình Local mã nguồn mở chạy qua Ollama.
        /// </summary>
        Ollama = 4
    }
}
