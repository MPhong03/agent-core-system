// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.Agents.Presentation.Controllers
{
    /// <summary>
    /// Controller cơ sở cho toàn bộ các API Controller thuộc phân hệ Agents.
    /// </summary>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Lấy mã định danh người dùng hiện tại từ Claims Principal (ClaimTypes.NameIdentifier hoặc 'sub').
        /// </summary>
        /// <returns>Mã định danh UUID của người dùng, hoặc Guid.Empty nếu không xác định được.</returns>
        protected Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
