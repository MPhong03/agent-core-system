// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.Organization.Application
{
    /// <summary>
    /// Lớp mở rộng đăng ký các dịch vụ tầng Application của phân hệ Organization.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Đăng ký các dịch vụ MediatR Handlers và FluentValidation Validators của phân hệ Organization vào DI container.
        /// </summary>
        /// <param name="services">Tập hợp các dịch vụ trong ứng dụng.</param>
        /// <returns>Tập hợp dịch vụ sau khi đã đăng ký.</returns>
        public static IServiceCollection AddOrganizationApplication(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
            services.AddValidatorsFromAssembly(assembly);

            return services;
        }
    }
}
