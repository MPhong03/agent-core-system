// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Tasks.Application;
using AgentCoreSystem.Module.Tasks.Infrastructure;
using AgentCoreSystem.Module.Tasks.Infrastructure.Realtime;
using AgentCoreSystem.Module.Tasks.Presentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Api.Extensions
{
    /// <summary>
    /// Các phương thức mở rộng kích hoạt và cấu hình phân hệ Tasks trong API Host.
    /// </summary>
    public static class TasksModuleExtensions
    {
        /// <summary>
        /// Kích hoạt và kết nối toàn bộ các tầng (Application, Infrastructure, Presentation) của Phân hệ Tasks.
        /// </summary>
        public static IServiceCollection AddTasksModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddTasksApplication();
            services.AddTasksInfrastructure(configuration);
            services.AddTasksPresentation();

            return services;
        }

        /// <summary>
        /// Ánh xạ SignalR Hub cho phân hệ Tasks tại endpoint /hubs/tasks.
        /// </summary>
        public static IEndpointRouteBuilder MapTasksHub(this IEndpointRouteBuilder app)
        {
            app.MapHub<SignalRTasksHub>("/hubs/tasks");

            return app;
        }
    }
}
