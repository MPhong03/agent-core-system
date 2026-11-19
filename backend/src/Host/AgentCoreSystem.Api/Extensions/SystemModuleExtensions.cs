// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.System.Application;
using AgentCoreSystem.Module.System.Infrastructure;
using AgentCoreSystem.Module.System.Presentation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Api.Extensions
{
    public static class SystemModuleExtensions
    {
        /// <summary>
        /// Kích hoạt và kết nối toàn bộ các tầng (Application, Infrastructure, Presentation) của Phân hệ System.
        /// </summary>
        public static IServiceCollection AddSystemModule(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            services.AddSystemApplication();
            services.AddSystemInfrastructure(configuration);
            services.AddSystemPresentation();

            return services;
        }
    }
}

