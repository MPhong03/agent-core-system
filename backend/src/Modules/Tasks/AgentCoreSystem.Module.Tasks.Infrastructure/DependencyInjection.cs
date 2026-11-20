// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Tasks.Application.Contracts;
using AgentCoreSystem.Module.Tasks.Infrastructure.Persistence;
using AgentCoreSystem.Shared.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.Tasks.Infrastructure
{
    /// <summary>
    /// Lớp mở rộng đăng ký các dịch vụ tầng hạ tầng (Infrastructure) của phân hệ Tasks.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Đăng ký các dịch vụ tầng hạ tầng (Infrastructure) của phân hệ Tasks bao gồm TasksDbContext.
        /// </summary>
        public static IServiceCollection AddTasksInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Lỗi cấu hình hệ thống: Thiếu chuỗi kết nối 'DefaultConnection' cho SQL Server.");

            services.AddDbContext<TasksDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Tasks", TasksDbContext.SchemaName);
                }).AddInterceptors(interceptor);
            });

            services.AddScoped<ITasksDbContext>(sp =>
                sp.GetRequiredService<TasksDbContext>());

            return services;
        }
    }
}
