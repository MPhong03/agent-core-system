// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace AgentCoreSystem.Module.System.Infrastructure.Security
{
    /// <summary>
    /// Handler phân quyền đặc quyền cho vai trò SuperAdmin.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Tự động bypass toàn bộ các chính sách phân quyền cho tài khoản SuperAdmin.</para>
    /// <para><strong>Why:</strong> Cho phép SuperAdmin truy cập mọi endpoint được bảo vệ bởi [Authorize(Policy = "...")] mà không cần cấp quyền chi tiết.</para>
    /// </remarks>
    public class SuperAdminBypassAuthorizationHandler : IAuthorizationHandler
    {
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            if (context.User.IsInRole(SystemRoles.SuperAdmin) ||
                context.User.HasClaim(ClaimTypes.Role, SystemRoles.SuperAdmin))
            {
                foreach (var requirement in context.PendingRequirements.ToList())
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}

