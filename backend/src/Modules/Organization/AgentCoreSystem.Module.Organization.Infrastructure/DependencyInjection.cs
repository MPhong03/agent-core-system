// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Organization.Infrastructure.Persistence;
using AgentCoreSystem.Shared.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.Organization.Infrastructure
{
    /// <summary>
    /// Lớp mở rộng đăng ký các dịch vụ tầng hạ tầng (Infrastructure) của phân hệ Organization.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Đăng ký DbContext, SQL Server provider và liên kết interface <c>IOrganizationDbContext</c>.
        /// </summary>
        /// <param name="services">Tập hợp các dịch vụ ứng dụng.</param>
        /// <param name="configuration">Cấu hình hệ thống (chứa chuỗi kết nối DefaultConnection).</param>
        /// <returns>Tập hợp dịch vụ sau khi đăng ký.</returns>
        public static IServiceCollection AddOrganizationInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Lỗi cấu hình hệ thống: Thiếu chuỗi kết nối 'DefaultConnection' cho SQL Server.");

            services.AddDbContext<OrganizationDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Organization", OrganizationDbContext.SchemaName);
                }).AddInterceptors(interceptor);
            });

            services.AddScoped<AgentCoreSystem.Module.Organization.Application.Contracts.IOrganizationDbContext>(sp => 
                sp.GetRequiredService<OrganizationDbContext>());

            return services;
        }
    }
}
