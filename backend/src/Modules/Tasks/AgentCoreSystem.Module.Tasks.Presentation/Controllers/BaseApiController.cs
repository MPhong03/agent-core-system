// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.Tasks.Presentation.Controllers
{
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
