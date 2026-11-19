// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace AgentCoreSystem.Module.System.Domain.Constants
{
    /// <summary>
    /// Định nghĩa các vai trò (Roles) mặc định của hệ thống.
    /// </summary>
    /// <remarks>
    /// <strong>Why:</strong> Tập trung định danh các Role cốt lõi để áp dụng nhất quán trên các Authorization Policies và Seed Data.
    /// </remarks>
    public static class SystemRoles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string Employee = "Employee";

        public static readonly IReadOnlyList<string> All = [SuperAdmin, Admin, Employee];
    }
}

