// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Chat.Application;
using AgentCoreSystem.Module.Chat.Infrastructure;
using AgentCoreSystem.Module.Chat.Infrastructure.Hubs;
using AgentCoreSystem.Module.Chat.Presentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Api.Extensions
{
    /// <summary>
    /// Các phương thức mở rộng kích hoạt và cấu hình phân hệ Chat trong API Host.
    /// </summary>
    public static class ChatModuleExtensions
    {
        /// <summary>
        /// Kích hoạt và kết nối toàn bộ các tầng (Application, Infrastructure, Presentation) của Phân hệ Chat.
        /// </summary>
        /// <param name="services">Tập hợp dịch vụ ứng dụng.</param>
        /// <param name="configuration">Cấu hình ứng dụng.</param>
        /// <returns>Tập hợp dịch vụ sau khi đăng ký.</returns>
        public static IServiceCollection AddChatModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddChatApplication();
            services.AddChatInfrastructure(configuration);
            services.AddChatPresentation();

            return services;
        }

        /// <summary>
        /// Ánh xạ SignalR Hub cho phân hệ Chat tại endpoint /hubs/chat.
        /// </summary>
        /// <param name="app">Endpoint route builder của ứng dụng Web.</param>
        /// <returns>Endpoint route builder sau khi đã ánh xạ.</returns>
        public static IEndpointRouteBuilder MapChatHub(this IEndpointRouteBuilder app)
        {
            app.MapHub<SignalRChatHub>("/hubs/chat");

            return app;
        }

        /// <summary>
        /// Ánh xạ các endpoints của phân hệ Chat.
        /// </summary>
        /// <param name="app">Endpoint route builder của ứng dụng Web.</param>
        /// <returns>Endpoint route builder sau khi đã ánh xạ.</returns>
        public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
        {
            return app.MapChatHub();
        }
    }
}
