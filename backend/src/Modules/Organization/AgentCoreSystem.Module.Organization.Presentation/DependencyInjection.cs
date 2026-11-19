// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.Organization.Presentation
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddOrganizationPresentation(this IServiceCollection services)
        {
            services.AddControllers()
                    .AddApplicationPart(typeof(DependencyInjection).Assembly);

            return services;
        }
    }
}
