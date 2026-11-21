// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace AgentCoreSystem.Shared.Application.Contracts
{
    /// <summary>
    /// Giao diện cung cấp thông tin người dùng đang thực hiện yêu cầu hiện tại từ Claims Principal.
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// Mã định danh người dùng hiện tại (nếu đã xác thực danh tính).
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// Cờ xác định yêu cầu có kèm thông tin định danh hợp lệ hay không.
        /// </summary>
        bool IsAuthenticated { get; }
    }
}
