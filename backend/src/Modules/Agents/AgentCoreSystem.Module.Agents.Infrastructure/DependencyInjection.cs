// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Infrastructure.Persistence;
using AgentCoreSystem.Shared.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.Agents.Infrastructure
{
    /// <summary>
    /// Lớp mở rộng đăng ký các dịch vụ tầng hạ tầng (Infrastructure) của phân hệ Agents.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Đăng ký các dịch vụ tầng hạ tầng (Infrastructure) của phân hệ Agents bao gồm AgentsDbContext.
        /// </summary>
        public static IServiceCollection AddAgentsInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Lỗi cấu hình hệ thống: Thiếu chuỗi kết nối 'DefaultConnection' cho SQL Server.");

            services.AddDbContext<AgentsDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Agents", AgentsDbContext.SchemaName);
                }).AddInterceptors(interceptor);
            });

            services.AddScoped<IAgentsDbContext>(sp =>
                sp.GetRequiredService<AgentsDbContext>());

            return services;
        }
    }
}
