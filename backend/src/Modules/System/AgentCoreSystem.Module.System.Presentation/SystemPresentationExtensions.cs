// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.System.Presentation
{
    public static class SystemPresentationExtensions
    {
        /// <summary>
        /// Đăng ký Controllers của Phân hệ System vào hệ thống.
        /// </summary>
        public static IServiceCollection AddSystemPresentation(this IServiceCollection services)
        {
            services.AddControllers()
                    .AddApplicationPart(typeof(SystemPresentationExtensions).Assembly);

            return services;
        }
    }
}

