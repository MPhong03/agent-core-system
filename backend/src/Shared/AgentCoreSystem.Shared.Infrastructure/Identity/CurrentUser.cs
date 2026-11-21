// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Security.Claims;
using AgentCoreSystem.Shared.Application.Contracts;
using Microsoft.AspNetCore.Http;

namespace AgentCoreSystem.Shared.Infrastructure.Identity
{
    /// <summary>
    /// Triển khai dịch vụ xác định người dùng hiện tại dựa trên HttpContext.
    /// </summary>
    public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        /// <inheritdoc />
        public Guid? UserId
        {
            get
            {
                var user = httpContextAccessor.HttpContext?.User;
                var sub = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user?.FindFirst("sub")?.Value;

                return Guid.TryParse(sub, out var id) ? id : null;
            }
        }

        /// <inheritdoc />
        public bool IsAuthenticated => UserId.HasValue && UserId.Value != Guid.Empty;
    }
}
