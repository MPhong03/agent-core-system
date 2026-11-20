// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Module.Organization.Domain.Enums
{
    /// <summary>
    /// Định danh phân loại thành viên trong kênh nhóm.
    /// </summary>
    /// <remarks>
    /// <para><strong>Why:</strong> Cho phép hệ thống trừu tượng hóa cả người thật (Human) lẫn nhân viên số (Agent) vào chung một kênh làm việc, tạo điều kiện thuận lợi cho việc giao tiếp và giao việc tự động.</para>
    /// <para>⇒ Human kéo theo liên kết logic tới UserId từ module System.</para>
    /// <para>⇒ Agent kéo theo liên kết khóa ngoại nội bộ tới AgentProfileId.</para>
    /// </remarks>
    public enum MemberType
    {
        /// <summary>
        /// Thành viên là người dùng thực tế trong hệ thống.
        /// </summary>
        /// <remarks>⇒ Yêu cầu UserId hợp lệ.</remarks>
        Human = 1,

        /// <summary>
        /// Thành viên là nhân viên AI tự chủ.
        /// </summary>
        /// <remarks>⇒ Yêu cầu AgentProfileId hợp lệ để lấy System Prompt và cấu hình LLM.</remarks>
        Agent = 2
    }
}
