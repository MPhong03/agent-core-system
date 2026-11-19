// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Chat.Application.Contracts;
using AgentCoreSystem.Module.Chat.Infrastructure.Persistence;
using AgentCoreSystem.Module.Chat.Infrastructure.Services;
using AgentCoreSystem.Shared.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.Chat.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Đăng ký các dịch vụ tầng hạ tầng (Infrastructure) của phân hệ Chat bao gồm ChatDbContext, SignalR và MessageDispatcherService.
        /// </summary>
        /// <param name="services">Tập hợp các dịch vụ ứng dụng.</param>
        /// <param name="configuration">Cấu hình hệ thống.</param>
        /// <returns>Tập hợp dịch vụ sau khi đã đăng ký.</returns>
        public static IServiceCollection AddChatInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Lỗi cấu hình hệ thống: Thiếu chuỗi kết nối 'DefaultConnection' cho SQL Server.");

            services.AddDbContext<ChatDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Chat", ChatDbContext.SchemaName);
                }).AddInterceptors(interceptor);
            });

            services.AddScoped<IChatDbContext>(sp =>
                sp.GetRequiredService<ChatDbContext>());

            services.AddSignalR();
            services.AddScoped<IMessageDispatcherService, MessageDispatcherService>();

            return services;
        }
    }
}
