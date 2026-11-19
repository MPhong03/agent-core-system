// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Organization.Application;
using AgentCoreSystem.Module.Organization.Infrastructure;
using AgentCoreSystem.Module.Organization.Presentation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Api.Extensions
{
    public static class OrganizationModuleExtensions
    {
        /// <summary>
        /// Kích hoạt và kết nối toàn bộ các tầng (Application, Infrastructure, Presentation) của Phân hệ Organization.
        /// </summary>
        public static IServiceCollection AddOrganizationModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOrganizationApplication();
            services.AddOrganizationInfrastructure(configuration);
            services.AddOrganizationPresentation();

            return services;
        }
    }
}
