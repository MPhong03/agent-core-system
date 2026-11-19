// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Organization.Domain.Enums;

namespace AgentCoreSystem.Module.Organization.Application.Members.DTOs
{
    /// <summary>
    /// Đối tượng truyền tải dữ liệu thành viên trong kênh làm việc.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Chứa thông tin thành viên kênh nhóm, hỗ trợ cả Human lẫn Agent.</para>
    /// <para><strong>When:</strong> Được trả về khi truy vấn danh sách thành viên kênh.</para>
    /// </remarks>
    /// <param name="Id">Mã định danh thành viên.</param>
    /// <param name="ChannelId">Mã kênh làm việc trực thuộc.</param>
    /// <param name="Type">Loại thành viên (Human hoặc Agent).</param>
    /// <param name="UserId">Mã người dùng (nếu là Human).</param>
    /// <param name="AgentProfileId">Mã hồ sơ Agent (nếu là Agent).</param>
    /// <param name="DisplayName">Tên hiển thị trong kênh.</param>
    /// <param name="CreatedDate">Thời điểm tham gia kênh.</param>
    public record MemberDto(
        Guid Id,
        Guid ChannelId,
        MemberType Type,
        Guid? UserId,
        Guid? AgentProfileId,
        string DisplayName,
        DateTimeOffset CreatedDate);
}
