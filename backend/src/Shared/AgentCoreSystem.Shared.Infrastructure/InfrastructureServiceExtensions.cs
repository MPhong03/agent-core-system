// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Shared.Infrastructure.Caching;
using AgentCoreSystem.Shared.Infrastructure.Hangfire;
using AgentCoreSystem.Shared.Infrastructure.Http;
using AgentCoreSystem.Shared.Infrastructure.LLM;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Shared.Infrastructure
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddSharedInfrastructure(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Caching
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();

            // Hangfire
            services.AddHangfireMongoStorage(configuration);

            // LLM Infrastructure
            services.AddLlmInfrastructure(configuration);

            // HTTP & REST API Tool Execution
            services.AddHttpInfrastructure();

            return services;
        }

        /// <summary>
        /// Đăng ký dịch vụ thực thi REST API không trạng thái kèm cấu hình ngăn chặn SSRF qua HTTP Redirects.
        /// </summary>
        /// <param name="services">Tập hợp các dịch vụ Dependency Injection.</param>
        /// <returns>Tập hợp các dịch vụ đã được cấu hình.</returns>
        public static IServiceCollection AddHttpInfrastructure(this IServiceCollection services)
        {
            services.AddHttpClient<IRestApiExecutor, RestApiExecutor>()
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    AllowAutoRedirect = false
                });

            return services;
        }

        public static IServiceCollection AddLlmInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));
            services.AddHttpClient();
            services.AddSingleton<ILLMProviderFactory, LlmProviderFactory>();
            services.AddTransient<ILLMClient>(sp => sp.GetRequiredService<ILLMProviderFactory>().GetClient());

            return services;
        }
    }
}
