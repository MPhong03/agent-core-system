// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Shared.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Shared.Persistence
{
    /// <summary>
    /// Lớp cơ sở DbContext trừu tượng cung cấp các tiện ích dùng chung cho các DbContext theo từng Module.
    /// </summary>
    public abstract class BaseDbContext : DbContext
    {
        protected BaseDbContext(DbContextOptions options) : base(options)
        {
        }
    }

    public static class PersistenceServiceExtensions
    {
        public static IServiceCollection AddSharedPersistence(this IServiceCollection services)
        {
            services.AddSingleton<AuditableEntityInterceptor>();
            return services;
        }
    }
}

