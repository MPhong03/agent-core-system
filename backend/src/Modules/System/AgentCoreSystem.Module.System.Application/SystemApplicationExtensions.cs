// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.System.Application
{
    public static class SystemApplicationExtensions
    {
        public static IServiceCollection AddSystemApplication(this IServiceCollection services)
        {
            var assembly = typeof(SystemApplicationExtensions).Assembly;

            // MediatR Handlers
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

            // FluentValidation
            services.AddValidatorsFromAssembly(assembly);

            return services;
        }
    }
}

