// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.System.Domain.Entities;
using AgentCoreSystem.Module.System.Domain.Repositories;
using AgentCoreSystem.Module.System.Infrastructure.Jobs;
using AgentCoreSystem.Module.System.Infrastructure.Persistence;
using AgentCoreSystem.Module.System.Infrastructure.Persistence.Repositories;
using AgentCoreSystem.Module.System.Infrastructure.Persistence.Seeds;
using AgentCoreSystem.Module.System.Infrastructure.Security;
using AgentCoreSystem.Shared.Persistence.Interceptors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.System.Infrastructure
{
    public static class SystemInfrastructureExtensions
    {
        public static IServiceCollection AddSystemInfrastructure(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Lỗi cấu hình hệ thống: Thiếu chuỗi kết nối 'DefaultConnection' cho SQL Server.");

            services.AddDbContext<SystemDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_System", SystemDbContext.SchemaName);
                }).AddInterceptors(interceptor);
            });

            // Cấu hình Microsoft Identity
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<SystemDbContext>()
            .AddDefaultTokenProviders();

            // Seed Data Settings (Options Pattern)
            services.AddOptions<AgentCoreSystem.Module.System.Application.Common.SystemSeedSettings>()
                .Bind(configuration.GetSection(AgentCoreSystem.Module.System.Application.Common.SystemSeedSettings.SectionName));

            // Repositories & Services
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<SystemDataSeeder>();
            services.AddTransient<SystemAuditLogCleanupJob>();

            // JWT Token Generator (Options Pattern & Validation Fail-Fast)
            services.AddOptions<AgentCoreSystem.Module.System.Application.Common.JwtSettings>()
                .Bind(configuration.GetSection(AgentCoreSystem.Module.System.Application.Common.JwtSettings.SectionName))
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.Secret) && settings.Secret.Length >= 32,
                    "Lỗi cấu hình bảo mật: 'JwtSettings:Secret' không được để trống và phải có độ dài tối thiểu 32 ký tự (256 bits).")
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer),
                    "Lỗi cấu hình hệ thống: 'JwtSettings:Issuer' không được để trống.")
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience),
                    "Lỗi cấu hình hệ thống: 'JwtSettings:Audience' không được để trống.")
                .Validate(settings => settings.ExpiryMinutes > 0,
                    "Lỗi cấu hình hệ thống: 'JwtSettings:ExpiryMinutes' phải là số nguyên dương (> 0).")
                .ValidateOnStart();

            services.AddScoped<AgentCoreSystem.Module.System.Application.Contracts.IJwtTokenGenerator, AgentCoreSystem.Module.System.Infrastructure.Authentication.JwtTokenGenerator>();

            // SuperAdmin Bypass Authorization
            services.AddSingleton<IAuthorizationHandler, SuperAdminBypassAuthorizationHandler>();

            return services;
        }
    }
}

