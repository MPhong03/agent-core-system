// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.Tasks.Presentation
{
    /// <summary>
    /// Đăng ký các dịch vụ thuộc tầng Presentation của phân hệ Tasks.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Đăng ký các controller thuộc phân hệ Tasks vào bộ container dịch vụ của ứng dụng.
        /// </summary>
        public static IServiceCollection AddTasksPresentation(this IServiceCollection services)
        {
            services.AddControllers()
                    .AddApplicationPart(typeof(DependencyInjection).Assembly);

            return services;
        }
    }
}
