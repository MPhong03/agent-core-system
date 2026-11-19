// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Application.Common;
using AgentCoreSystem.Module.System.Domain.Constants;
using AgentCoreSystem.Module.System.Domain.Entities;
using AgentCoreSystem.Module.System.Infrastructure.Persistence.Seeds;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AgentCoreSystem.UnitTests
{
    /// <summary>
    /// Kiểm thử dịch vụ khởi tạo dữ liệu mẫu (SystemDataSeeder) và kiểm tra bảo mật cấu hình.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Đảm bảo dữ liệu khởi tạo tuân thủ nguyên tắc Zero Hardcoded Secrets & Fail-Fast Configuration.</para>
    /// <para><strong>Why:</strong> Ngăn chặn việc lọt mật khẩu mặc định hoặc chạy ứng dụng với cấu hình seed không an toàn.</para>
    /// </remarks>
    public class SystemDataSeederTests
    {
        private readonly IRoleStore<ApplicationRole> _roleStore = Substitute.For<IRoleStore<ApplicationRole>>();
        private readonly IUserStore<ApplicationUser> _userStore = Substitute.For<IUserStore<ApplicationUser>>();
        private readonly ILogger<SystemDataSeeder> _logger = Substitute.For<ILogger<SystemDataSeeder>>();

        private RoleManager<ApplicationRole> CreateRoleManager() =>
            Substitute.For<RoleManager<ApplicationRole>>(_roleStore, null, null, null, null);

        private UserManager<ApplicationUser> CreateUserManager() =>
            Substitute.For<UserManager<ApplicationUser>>(_userStore, null, null, null, null, null, null, null, null);

        [Fact]
        public void SystemSeedSettings_DefaultValues_ShouldBeEmpty_And_Disabled()
        {
            // Arrange & Act
            var settings = new SystemSeedSettings();

            // Assert (Đảm bảo không hardcode credentials trong class)
            Assert.False(settings.EnableSeeding);
            Assert.NotNull(settings.SuperAdmin);
            Assert.Empty(settings.SuperAdmin.Email);
            Assert.Empty(settings.SuperAdmin.Password);
            Assert.Empty(settings.SuperAdmin.FullName);
        }

        [Fact]
        public async Task SeedAsync_When_EnableSeeding_Is_False_Should_Skip_Seeding()
        {
            // Arrange
            var roleManager = CreateRoleManager();
            var userManager = CreateUserManager();
            var options = Options.Create(new SystemSeedSettings { EnableSeeding = false });
            var seeder = new SystemDataSeeder(roleManager, userManager, options, _logger);

            // Act
            await seeder.SeedAsync();

            // Assert (Không tương tác với RoleManager hay UserManager)
            await roleManager.DidNotReceive().RoleExistsAsync(Arg.Any<string>());
            await userManager.DidNotReceive().FindByEmailAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task SeedAsync_When_EnableSeeding_Is_True_And_Email_Missing_Should_Throw_InvalidOperationException()
        {
            // Arrange
            var roleManager = CreateRoleManager();
            roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(Task.FromResult(true));

            var userManager = CreateUserManager();
            var settings = new SystemSeedSettings
            {
                EnableSeeding = true,
                SuperAdmin = new SuperAdminSeedSettings
                {
                    Email = "", // Thiếu email
                    Password = "AdminPassword@2026"
                }
            };
            var options = Options.Create(settings);
            var seeder = new SystemDataSeeder(roleManager, userManager, options, _logger);

            // Act & Assert (Fail-Fast)
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());
            Assert.Contains("SeedSettings:SuperAdmin:Email", ex.Message);
        }

        [Fact]
        public async Task SeedAsync_When_EnableSeeding_Is_True_And_Password_Invalid_Should_Throw_InvalidOperationException()
        {
            // Arrange
            var roleManager = CreateRoleManager();
            roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(Task.FromResult(true));

            var userManager = CreateUserManager();
            var settings = new SystemSeedSettings
            {
                EnableSeeding = true,
                SuperAdmin = new SuperAdminSeedSettings
                {
                    Email = "admin@AgentCoreSystem.vn",
                    Password = "123" // Mật khẩu quá ngắn (< 6 ký tự)
                }
            };
            var options = Options.Create(settings);
            var seeder = new SystemDataSeeder(roleManager, userManager, options, _logger);

            // Act & Assert (Fail-Fast)
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());
            Assert.Contains("SeedSettings:SuperAdmin:Password", ex.Message);
        }

        [Fact]
        public async Task SeedAsync_When_Config_Is_Valid_Should_Create_Roles_And_SuperAdmin()
        {
            // Arrange
            var roleManager = CreateRoleManager();
            roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(Task.FromResult(false));
            roleManager.CreateAsync(Arg.Any<ApplicationRole>()).Returns(Task.FromResult(IdentityResult.Success));

            var userManager = CreateUserManager();
            userManager.FindByEmailAsync("admin@AgentCoreSystem.vn").Returns(Task.FromResult<ApplicationUser?>(null));
            userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(Task.FromResult(IdentityResult.Success));
            userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), SystemRoles.SuperAdmin).Returns(Task.FromResult(IdentityResult.Success));

            var settings = new SystemSeedSettings
            {
                EnableSeeding = true,
                SuperAdmin = new SuperAdminSeedSettings
                {
                    Email = "admin@AgentCoreSystem.vn",
                    Password = "SecurePassword@2026",
                    FullName = "AgentCoreSystem Super Admin"
                }
            };
            var options = Options.Create(settings);
            var seeder = new SystemDataSeeder(roleManager, userManager, options, _logger);

            // Act
            await seeder.SeedAsync();

            // Assert
            await roleManager.Received(3).CreateAsync(Arg.Any<ApplicationRole>());
            await userManager.Received(1).CreateAsync(
                Arg.Is<ApplicationUser>(u => u.Email == "admin@AgentCoreSystem.vn" && u.FullName == "AgentCoreSystem Super Admin"),
                "SecurePassword@2026");
            await userManager.Received(1).AddToRoleAsync(Arg.Any<ApplicationUser>(), SystemRoles.SuperAdmin);
        }
    }
}

