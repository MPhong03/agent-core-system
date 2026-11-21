// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.Agents.Application
{
    /// <summary>
    /// Đăng ký các dịch vụ thuộc tầng Application của phân hệ Agents.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Đăng ký MediatR handlers và FluentValidation validators cho phân hệ Agents.
        /// </summary>
        public static IServiceCollection AddAgentsApplication(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
            services.AddValidatorsFromAssembly(assembly);

            return services;
        }
    }
}
