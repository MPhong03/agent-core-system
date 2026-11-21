// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using AgentCoreSystem.Module.System.Application.Common;
using AgentCoreSystem.Module.System.Domain.Constants;
using AgentCoreSystem.Module.System.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentCoreSystem.Module.System.Infrastructure.Persistence.Seeds
{
    /// <summary>
    /// Dịch vụ khởi tạo dữ liệu mẫu cho vai trò và tài khoản quản trị tối cao (SuperAdmin).
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Khởi tạo các Role hệ thống và tài khoản SuperAdmin ban đầu từ cấu hình SeedSettings.</para>
    /// <para><strong>When to use:</strong> Được gọi bởi Host API trong quá trình khởi động (Program.cs) để đảm bảo database có đủ dữ liệu nền tảng.</para>
    /// <para><strong>Why:</strong> Không hardcode thông tin tài khoản trong source code, tuân thủ nguyên tắc Fail-Fast và Zero Hardcoded Secrets.</para>
    /// </remarks>
    public class SystemDataSeeder(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<SystemSeedSettings> seedOptions,
        ILogger<SystemDataSeeder> logger)
    {
        private readonly SystemSeedSettings _seedSettings = seedOptions.Value;

        public async Task SeedAsync()
        {
            if (!_seedSettings.EnableSeeding)
            {
                logger.LogInformation("Tính năng Data Seeding đang tắt (SeedSettings:EnableSeeding = false). Bỏ qua bước khởi tạo tài khoản.");
                return;
            }

            logger.LogInformation("Bắt đầu kiểm tra và khởi tạo dữ liệu mặc định cho Module System...");

            // 1. Seed Roles: SuperAdmin, Admin, Employee
            foreach (var roleName in SystemRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new ApplicationRole(roleName, $"Vai trò hệ thống: {roleName}");
                    await roleManager.CreateAsync(role);
                    logger.LogInformation("Đã khởi tạo Role mặc định: {RoleName}", roleName);
                }
            }

            // 2. Validate SuperAdmin credentials theo nguyên tắc Fail-Fast
            var superAdminConfig = _seedSettings.SuperAdmin;
            if (string.IsNullOrWhiteSpace(superAdminConfig.Email))
            {
                throw new InvalidOperationException("Lỗi cấu hình Seed Data: 'SeedSettings:SuperAdmin:Email' không được để trống khi EnableSeeding = true.");
            }

            if (string.IsNullOrWhiteSpace(superAdminConfig.Password) || superAdminConfig.Password.Length < 6)
            {
                throw new InvalidOperationException("Lỗi cấu hình Seed Data: 'SeedSettings:SuperAdmin:Password' không được để trống và phải có độ dài tối thiểu 6 ký tự.");
            }

            var adminEmail = superAdminConfig.Email.Trim();
            var normalizedEmail = adminEmail.ToUpperInvariant();
            var existingSuperAdmin = await userManager.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail || u.NormalizedUserName == normalizedEmail);

            if (existingSuperAdmin != null)
            {
                if (existingSuperAdmin.IsDeleted)
                {
                    existingSuperAdmin.IsDeleted = false;
                    existingSuperAdmin.DeletedDate = null;
                    existingSuperAdmin.IsActive = true;
                    await userManager.UpdateAsync(existingSuperAdmin);
                    logger.LogInformation("Tài khoản SuperAdmin ({Email}) đã được tự động khôi phục và kích hoạt lại.", adminEmail);
                }

                if (!await userManager.IsInRoleAsync(existingSuperAdmin, SystemRoles.SuperAdmin))
                {
                    await userManager.AddToRoleAsync(existingSuperAdmin, SystemRoles.SuperAdmin);
                }
            }
            else
            {
                var fullName = string.IsNullOrWhiteSpace(superAdminConfig.FullName)
                    ? "Super Administrator"
                    : superAdminConfig.FullName.Trim();

                var superAdmin = new ApplicationUser
                {
                    Email = adminEmail,
                    UserName = adminEmail,
                    FullName = fullName,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(superAdmin, superAdminConfig.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, SystemRoles.SuperAdmin);
                    logger.LogInformation("Đã khởi tạo tài khoản SuperAdmin thành công ({Email}) từ cấu hình ngoài.", adminEmail);
                }
                else
                {
                    logger.LogError("Khởi tạo SuperAdmin thất bại: {Errors}", string.Join(", ", result.Errors));
                    throw new InvalidOperationException($"Lỗi khởi tạo tài khoản SuperAdmin: {string.Join(", ", result.Errors)}");
                }
            }
        }
    }
}

