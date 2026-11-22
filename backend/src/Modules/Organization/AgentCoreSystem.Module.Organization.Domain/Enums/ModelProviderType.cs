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
    /// <para><strong>Why:</strong> Cho phép từng Agent linh hoạt lựa chọn "bộ não" phù hợp nhất với chi phí và độ phức tạp của bài toán nghiệp vụ.</para>
    /// <para>🔴 <strong>Lưu ý:</strong> Mỗi Provider yêu cầu cấu hình API Key và Gateway tương ứng trong cấu hình hạ tầng.</para>
    /// </remarks>
    public enum ModelProviderType
    {
        /// <summary>
        /// Google Gemini API (Flash, Pro).
        /// </summary>
        /// <remarks>Mặc định ưu tiên gemini-2.5-flash cho tác vụ thường nhật và gemini-2.5-pro cho phân tích chuyên sâu.</remarks>
        Gemini = 1,

        /// <summary>
        /// OpenAI API (GPT-4o, GPT-4o-mini).
        /// </summary>
        /// <remarks>Dùng cho các tác vụ suy luận tổng quát và sinh code.</remarks>
        OpenAI = 2,

        /// <summary>
        /// Anthropic Claude API (Sonnet, Haiku).
        /// </summary>
        /// <remarks>Dùng cho tác vụ đọc hiểu tài liệu lớn và lập luận phức tạp.</remarks>
        Claude = 3,

        /// <summary>
        /// Mô hình Local mã nguồn mở chạy qua Ollama.
        /// </summary>
        /// <remarks>⇒ Không tốn chi phí token bên ngoài, bảo mật dữ liệu tuyệt đối trong mạng nội bộ.</remarks>
        Ollama = 4,

        /// <summary>
        /// Cổng định tuyến cục bộ 9router (OpenAI-compatible đa mô hình).
        /// </summary>
        /// <remarks>⇒ Kết nối tới 9router gateway cục bộ tại localhost.</remarks>
        NineRouter = 5
    }
}
