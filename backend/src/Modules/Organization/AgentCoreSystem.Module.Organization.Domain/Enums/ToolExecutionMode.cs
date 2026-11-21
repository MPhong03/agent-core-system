// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Module.Organization.Domain.Enums
{
    /// <summary>
    /// Chế độ thực thi công cụ (Tool Execution Mode) dành cho Agent Profile.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Xác định mức độ tự chủ của Agent khi gọi các công cụ (Function Calling / Tools).</para>
    /// <para><strong>Why:</strong> Cho phép kiểm soát an toàn bảo mật và quy trình phê duyệt (Human-in-the-loop) đối với các hành động quan trọng.</para>
    /// </remarks>
    public enum ToolExecutionMode
    {
        /// <summary>
        /// Tự động thực thi toàn bộ công cụ mà không cần phê duyệt.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Bán tự động: Các công cụ nhạy cảm hoặc yêu cầu xác nhận sẽ chờ người dùng phê duyệt, các công cụ còn lại tự động chạy.
        /// </summary>
        SemiAuto = 1,

        /// <summary>
        /// Bắt buộc phê duyệt: Mọi lời gọi công cụ đều yêu cầu xác nhận từ con người trước khi thực thi.
        /// </summary>
        NeedApprove = 2
    }
}
