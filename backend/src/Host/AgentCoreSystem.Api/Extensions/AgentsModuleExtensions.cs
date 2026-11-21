// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Agents.Application;
using AgentCoreSystem.Module.Agents.Infrastructure;
using AgentCoreSystem.Module.Agents.Presentation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Api.Extensions
{
    /// <summary>
    /// Các phương thức mở rộng kích hoạt và cấu hình phân hệ Agents trong API Host.
    /// </summary>
    public static class AgentsModuleExtensions
    {
        /// <summary>
        /// Kích hoạt và kết nối toàn bộ các tầng (Application, Infrastructure, Presentation) của Phân hệ Agents.
        /// </summary>
        public static IServiceCollection AddAgentsModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddAgentsApplication();
            services.AddAgentsInfrastructure(configuration);
            services.AddAgentsPresentation();

            return services;
        }
    }
}
