// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using AgentCoreSystem.Module.System.Application.Common;
using AgentCoreSystem.Module.System.Domain.Constants;
using AgentCoreSystem.Module.System.Domain.Entities;
using AgentCoreSystem.Module.System.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace AgentCoreSystem.UnitTests
{
    public class JwtTokenGeneratorTests
    {
        [Fact]
        public void GenerateToken_Should_Produce_Valid_Jwt_With_Expected_Claims()
        {
            // Arrange
            var jwtSettings = new JwtSettings
            {
                Secret = "AgentCoreSystemModularMonolithSecuritySuperKey2026!@#$%^&*()_+LongEnoughSecret256Bits",
                Issuer = "AgentCoreSystem.Api",
                Audience = "AgentCoreSystem.Client",
                ExpiryMinutes = 60
            };
            var options = Options.Create(jwtSettings);
            var generator = new JwtTokenGenerator(options);

            var user = new ApplicationUser
            {
                Email = "testuser@mvp.vn",
                UserName = "testuser@mvp.vn",
                FullName = "Test User"
            };
            var roles = new[] { SystemRoles.Admin, SystemRoles.Employee };

            // Act
            var tokenString = generator.GenerateToken(user, roles);

            // Assert
            Assert.NotNull(tokenString);
            Assert.NotEmpty(tokenString);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenString);

            Assert.Equal(jwtSettings.Issuer, jwtToken.Issuer);
            Assert.Contains(jwtSettings.Audience, jwtToken.Audiences);

            var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            Assert.Equal(user.Id.ToString(), subClaim);

            var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
            Assert.Equal(user.Email, emailClaim);

            var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").Select(c => c.Value).ToList();
            Assert.Contains(SystemRoles.Admin, roleClaims);
            Assert.Contains(SystemRoles.Employee, roleClaims);
        }

        [Fact]
        public void GenerateRefreshToken_Should_Return_NonEmpty_Base64_String()
        {
            // Arrange
            var options = Options.Create(new JwtSettings());
            var generator = new JwtTokenGenerator(options);

            // Act
            var refreshToken1 = generator.GenerateRefreshToken();
            var refreshToken2 = generator.GenerateRefreshToken();

            // Assert
            Assert.NotEmpty(refreshToken1);
            Assert.NotEmpty(refreshToken2);
            Assert.NotEqual(refreshToken1, refreshToken2);
        }

        [Fact]
        public void JwtSettings_Should_Not_Have_Hardcoded_Default_Secret_And_Be_Empty_ByDefault()
        {
            // Arrange & Act (Đảm bảo tuân thủ nguyên tắc Zero Hardcoded Secrets & Fail-Fast)
            var settings = new JwtSettings();

            // Assert
            Assert.True(string.IsNullOrEmpty(settings.Secret));
            Assert.True(string.IsNullOrEmpty(settings.Issuer));
            Assert.True(string.IsNullOrEmpty(settings.Audience));
            Assert.Equal(0, settings.ExpiryMinutes);
        }
    }
}

