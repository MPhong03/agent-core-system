// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Organization.Domain.Entities
{
    /// <summary>
    /// Thực thể đại diện cho Hồ sơ định danh Nhân sự AI (Agent Profile).
    /// </summary>
    /// <remarks>
    /// <strong>What:</strong> Quản lý danh tính, mô hình LLM, System Prompt và các thiết lập tính cách cốt lõi của Agent.
    /// <strong>Why:</strong> Cho phép định hình năng lực và vai trò chuyên môn hóa độc lập cho từng Agent trong tổ chức.
    /// </remarks>
    public class AgentProfile : BaseEntity
    {
        // ── Constructors ──

        /// <summary>
        /// Khởi tạo một hồ sơ Agent mới.
        /// </summary>
        /// <param name="name">Tên định danh của Agent.</param>
        /// <param name="avatarUrl">Đường dẫn ảnh đại diện.</param>
        /// <param name="provider">Nhà cung cấp LLM (Gemini, OpenAI, Claude, Ollama).</param>
        /// <param name="modelName">Tên model cụ thể (vd: gemini-2.5-flash, gpt-4o).</param>
        /// <param name="systemPrompt">Chỉ dẫn hệ thống cốt lõi định hình vai trò.</param>
        /// <param name="temperature">Mức độ sáng tạo của mô hình (0.0 đến 2.0).</param>
        /// <param name="privateMemoryNotes">Ghi chú bộ nhớ riêng ban đầu.</param>
        /// <param name="toolExecutionMode">Chế độ thực thi công cụ (mặc định SemiAuto).</param>
        public AgentProfile(
            string name,
            string? avatarUrl,
            ModelProviderType provider,
            string modelName,
            string systemPrompt,
            decimal temperature = 0.7m,
            string? privateMemoryNotes = null,
            ToolExecutionMode toolExecutionMode = ToolExecutionMode.SemiAuto) : base()
        {
            Name = name;
            AvatarUrl = avatarUrl;
            Provider = provider;
            ModelName = modelName;
            SystemPrompt = systemPrompt;
            Temperature = temperature;
            PrivateMemoryNotes = privateMemoryNotes;
            ToolExecutionMode = toolExecutionMode;
            Members = new List<Member>();
        }

        /// <summary>
        /// Constructor phục vụ Entity Framework Core deserialization.
        /// </summary>
        protected AgentProfile() : base()
        {
            Name = string.Empty;
            ModelName = string.Empty;
            SystemPrompt = string.Empty;
            Members = new List<Member>();
        }

        // ── Core Identity ──

        /// <summary>
        /// Tên định danh của Agent (vd: Tech Researcher, Email Scraper).
        /// </summary>
        /// <remarks>🔴 <strong>Ràng buộc:</strong> Tối đa 150 ký tự, không được để trống.</remarks>
        public string Name { get; set; }

        /// <summary>
        /// Đường dẫn ảnh đại diện của Agent.
        /// </summary>
        /// <remarks>Tối đa 500 ký tự. Có thể là URL nội bộ hoặc CDN đám mây.</remarks>
        public string? AvatarUrl { get; set; }

        // ── LLM Configuration ──

        /// <summary>
        /// Nhà cung cấp mô hình AI.
        /// </summary>
        /// <remarks>
        /// <para>Xác định bộ adapter LLM tương ứng trong hệ thống.</para>
        /// <para>⇒ Thay đổi provider kéo theo cấu hình API Key và Endpoint tương ứng trong cấu hình hạ tầng.</para>
        /// </remarks>
        public ModelProviderType Provider { get; set; }

        /// <summary>
        /// Tên định danh kỹ thuật của model (vd: gemini-2.5-flash, claude-3-7-sonnet).
        /// </summary>
        /// <remarks>🔴 <strong>Lưu ý:</strong> Phải khớp chính xác chuỗi định danh mô hình mà Provider hỗ trợ.</remarks>
        public string ModelName { get; set; }

        /// <summary>
        /// Chỉ dẫn hệ thống định hình tính cách và năng lực chuyên môn.
        /// </summary>
        /// <remarks>
        /// <para>Chỉ dẫn cốt lõi luôn được đưa vào System Role trong mỗi ngữ cảnh hội thoại.</para>
        /// <para>⛔ <strong>Cấm:</strong> Không đưa bí mật hệ thống hoặc token xác thực vào System Prompt.</para>
        /// </remarks>
        public string SystemPrompt { get; set; }

        /// <summary>
        /// Độ sáng tạo của model (thường từ 0.0 đến 2.0).
        /// </summary>
        /// <remarks>
        /// <para>🔴 <strong>Lưu ý:</strong> Giá trị nằm trong khoảng từ 0.0 đến 2.0.</para>
        /// <para>⇒ Giá trị càng cao thì phản hồi càng đa dạng nhưng nguy cơ hallucination tăng.</para>
        /// </remarks>
        public decimal Temperature { get; set; } = 0.7m;

        // ── Memory & Context ──

        /// <summary>
        /// Ghi chú bộ nhớ riêng tư lưu thói quen và kinh nghiệm làm việc của Agent.
        /// </summary>
        /// <remarks>
        /// <para>AI-NOTE: Vùng đệm ghi nhớ ngắn hạn và trung hạn riêng của Agent trước khi đồng bộ sang hệ thống bộ nhớ phân tán.</para>
        /// </remarks>
        public string? PrivateMemoryNotes { get; set; }

        // ── Tool Calling & Autonomy ──

        /// <summary>
        /// Chế độ thực thi công cụ và mức độ tự chủ của Agent.
        /// </summary>
        /// <remarks>
        /// <para>Mặc định là <see cref="ToolExecutionMode.SemiAuto"/> (bán tự động).</para>
        /// </remarks>
        public ToolExecutionMode ToolExecutionMode { get; set; } = ToolExecutionMode.SemiAuto;

        // ── Navigation ──

        /// <summary>
        /// Danh sách thành viên trong các kênh mà hồ sơ Agent này đang đảm nhiệm.
        /// </summary>
        /// <remarks>
        /// <para>Quan hệ 1-N: Một hồ sơ Agent có thể được phân bổ làm thành viên tại nhiều kênh khác nhau.</para>
        /// </remarks>
        public virtual ICollection<Member> Members { get; set; }
    }
}
