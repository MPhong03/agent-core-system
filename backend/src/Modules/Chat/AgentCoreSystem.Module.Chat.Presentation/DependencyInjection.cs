// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.Chat.Presentation
{
    /// <summary>
    /// Đăng ký các dịch vụ thuộc tầng Presentation của phân hệ Chat.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Đăng ký các controller thuộc phân hệ Chat vào bộ container dịch vụ của ứng dụng.
        /// </summary>
        /// <param name="services">Tập hợp các dịch vụ ứng dụng.</param>
        /// <returns>Tập hợp dịch vụ sau khi đã đăng ký.</returns>
        public static IServiceCollection AddChatPresentation(this IServiceCollection services)
        {
            services.AddControllers()
                    .AddApplicationPart(typeof(DependencyInjection).Assembly);

            return services;
        }
    }
}
