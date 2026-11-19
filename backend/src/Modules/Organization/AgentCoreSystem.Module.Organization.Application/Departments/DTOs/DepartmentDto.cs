// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace AgentCoreSystem.Module.Organization.Application.Departments.DTOs
{
    /// <summary>
    /// Đối tượng truyền tải dữ liệu đại diện cho Phòng ban / Khối chức năng.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Chứa thông tin tổng quan của phòng ban kèm số lượng kênh trực thuộc.</para>
    /// <para><strong>When:</strong> Được trả về từ các truy vấn danh sách phòng ban.</para>
    /// </remarks>
    /// <param name="Id">Mã định danh phòng ban.</param>
    /// <param name="Name">Tên phòng ban.</param>
    /// <param name="Description">Mô tả chức năng nhiệm vụ.</param>
    /// <param name="CreatedDate">Thời điểm khởi tạo.</param>
    /// <param name="ChannelCount">Số lượng kênh làm việc trực thuộc.</param>
    public record DepartmentDto(
        Guid Id,
        string Name,
        string? Description,
        DateTimeOffset CreatedDate,
        int ChannelCount);
}
