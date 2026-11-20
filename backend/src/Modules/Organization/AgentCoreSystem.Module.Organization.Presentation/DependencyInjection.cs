// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.Organization.Presentation
{
    /// <summary>
    /// Lớp mở rộng đăng ký các dịch vụ tầng Presentation của phân hệ Organization.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Đăng ký các Controller của phân hệ Organization vào DI container của ứng dụng Web API.
        /// </summary>
        /// <param name="services">Tập hợp các dịch vụ ứng dụng.</param>
        /// <returns>Tập hợp dịch vụ sau khi đã đăng ký.</returns>
        public static IServiceCollection AddOrganizationPresentation(this IServiceCollection services)
        {
            services.AddControllers()
                    .AddApplicationPart(typeof(DependencyInjection).Assembly);

            return services;
        }
    }
}
