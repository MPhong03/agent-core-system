// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace AgentCoreSystem.Module.Organization.Application.Channels.DTOs
{
    /// <summary>
    /// Đối tượng truyền tải dữ liệu đại diện cho Kênh nhóm làm việc.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Chứa thông tin kênh trực thuộc phòng ban kèm số lượng thành viên (Human &amp; Agent).</para>
    /// <para><strong>When:</strong> Được trả về khi truy vấn danh sách kênh theo phòng ban.</para>
    /// </remarks>
    /// <param name="Id">Mã định danh kênh nhóm.</param>
    /// <param name="DepartmentId">Mã định danh phòng ban trực thuộc.</param>
    /// <param name="Name">Tên kênh làm việc.</param>
    /// <param name="Description">Mô tả mục tiêu của kênh.</param>
    /// <param name="CreatedDate">Thời điểm khởi tạo.</param>
    /// <param name="MemberCount">Số lượng thành viên đang tham gia kênh.</param>
    public record TeamChannelDto(
        Guid Id,
        Guid DepartmentId,
        string Name,
        string? Description,
        DateTimeOffset CreatedDate,
        int MemberCount);
}
