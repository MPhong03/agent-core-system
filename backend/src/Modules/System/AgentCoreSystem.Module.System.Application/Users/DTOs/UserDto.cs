// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace AgentCoreSystem.Module.System.Application.Users.DTOs
{
    public record UserDto(
        Guid Id,
        string Email,
        string FullName,
        string? PhoneNumber,
        string? AvatarUrl,
        bool IsActive,
        IReadOnlyList<string> Roles,
        DateTimeOffset CreatedDate);
}

