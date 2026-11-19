// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Domain.Constants;
using AgentCoreSystem.Module.System.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace AgentCoreSystem.UnitTests
{
    public class DummyRequirement : IAuthorizationRequirement
    {
    }

    public class SuperAdminBypassTests
    {
        [Fact]
        public async Task SuperAdmin_Should_Bypass_All_Pending_Requirements()
        {
            // Arrange
            var handler = new SuperAdminBypassAuthorizationHandler();
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "SuperAdminUser"),
                new(ClaimTypes.Role, SystemRoles.SuperAdmin)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var requirement = new DummyRequirement();
            var context = new AuthorizationHandlerContext([requirement], principal, null);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
            Assert.Empty(context.PendingRequirements);
        }

        [Fact]
        public async Task Non_SuperAdmin_Should_Not_Bypass_Requirements()
        {
            // Arrange
            var handler = new SuperAdminBypassAuthorizationHandler();
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "EmployeeUser"),
                new(ClaimTypes.Role, SystemRoles.Employee)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var requirement = new DummyRequirement();
            var context = new AuthorizationHandlerContext([requirement], principal, null);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.Single(context.PendingRequirements);
        }
    }
}

