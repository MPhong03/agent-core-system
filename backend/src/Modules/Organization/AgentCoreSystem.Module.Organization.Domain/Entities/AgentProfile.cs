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
        public AgentProfile(
            string name,
            string? avatarUrl,
            ModelProviderType provider,
            string modelName,
            string systemPrompt,
            decimal temperature = 0.7m,
            string? privateMemoryNotes = null) : base()
        {
            Name = name;
            AvatarUrl = avatarUrl;
            Provider = provider;
            ModelName = modelName;
            SystemPrompt = systemPrompt;
            Temperature = temperature;
            PrivateMemoryNotes = privateMemoryNotes;
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

        /// <summary>
        /// Tên định danh của Agent (vd: Tech Researcher, Email Scraper).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Đường dẫn ảnh đại diện của Agent.
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Nhà cung cấp mô hình AI.
        /// </summary>
        public ModelProviderType Provider { get; set; }

        /// <summary>
        /// Tên định danh kỹ thuật của model (vd: gemini-2.5-flash, claude-3-7-sonnet).
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Chỉ dẫn hệ thống định hình tính cách và năng lực chuyên môn.
        /// </summary>
        public string SystemPrompt { get; set; }

        /// <summary>
        /// Độ sáng tạo của model (thường từ 0.0 đến 1.0).
        /// </summary>
        public decimal Temperature { get; set; } = 0.7m;

        /// <summary>
        /// Ghi chú bộ nhớ riêng tư lưu thói quen và kinh nghiệm làm việc của Agent.
        /// </summary>
        public string? PrivateMemoryNotes { get; set; }

        /// <summary>
        /// Danh sách thành viên trong các kênh mà hồ sơ Agent này đang đảm nhiệm.
        /// </summary>
        public virtual ICollection<Member> Members { get; set; }
    }
}
