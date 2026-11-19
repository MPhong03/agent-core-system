// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Shared.Kernel.Localization
{
    /// <summary>
    /// Dummy class dùng làm Marker Type cho IStringLocalizer trong toàn hệ thống.
    /// </summary>
    /// <remarks>
    /// <strong>Why:</strong> Cho phép inject IStringLocalizer&lt;SharedResources&gt; tại bất kỳ service hoặc validator nào để đọc thông điệp đa ngôn ngữ.
    /// </remarks>
    public class SharedResources
    {
    }
}

