// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Module.Organization.Presentation.Requests
{
    /// <summary>
    /// Dữ liệu yêu cầu tạo mới phòng ban / khối chức năng.
    /// </summary>
    /// <param name="Name">Tên phòng ban duy nhất.</param>
    /// <param name="Description">Mô tả chức năng nhiệm vụ của phòng ban.</param>
    public record CreateDepartmentRequest(string Name, string? Description = null);

    /// <summary>
    /// Dữ liệu yêu cầu tạo mới kênh làm việc trong phòng ban.
    /// </summary>
    /// <param name="Name">Tên kênh làm việc định danh.</param>
    /// <param name="Description">Mô tả mục tiêu của kênh.</param>
    public record CreateChannelRequest(string Name, string? Description = null);
}
