// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Module.Chat.Application
{
    /// <summary>
    /// Đăng ký các dịch vụ thuộc tầng Application của phân hệ Chat.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Đăng ký MediatR handlers và FluentValidation validators cho phân hệ Chat.
        /// </summary>
        /// <param name="services">Bộ tập hợp dịch vụ ứng dụng.</param>
        /// <returns>Bộ tập hợp dịch vụ ứng dụng sau khi đã đăng ký.</returns>
        public static IServiceCollection AddChatApplication(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
            services.AddValidatorsFromAssembly(assembly);

            return services;
        }
    }
}
