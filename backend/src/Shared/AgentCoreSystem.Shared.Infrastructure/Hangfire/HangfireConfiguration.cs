// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentCoreSystem.Shared.Infrastructure.Hangfire
{
    /// <summary>
    /// Cấu hình và tích hợp Hangfire sử dụng MongoDB Storage.
    /// </summary>
    /// <remarks>
    /// <strong>Why MongoDB:</strong> Giảm tải hoàn toàn các polling query, job locks và state telemetry khỏi SQL Server.
    /// </remarks>
    public static class HangfireConfiguration
    {
        public static IServiceCollection AddHangfireMongoStorage(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            var mongoConnection = configuration.GetConnectionString("HangfireMongoConnection") 
                ?? throw new InvalidOperationException("Lỗi cấu hình hệ thống: Thiếu chuỗi kết nối 'HangfireMongoConnection' cho MongoDB Hangfire Storage.");

            services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                      .UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings()
                      .UseMongoStorage(mongoConnection, new MongoStorageOptions
                      {
                          MigrationOptions = new MongoMigrationOptions
                          {
                              MigrationStrategy = new MigrateMongoMigrationStrategy(),
                              BackupStrategy = new CollectionMongoBackupStrategy()
                          },
                          Prefix = "hangfire",
                          CheckConnection = true
                      });
            });

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = Environment.ProcessorCount * 2;
                options.Queues = ["critical", "default", "system"];
            });

            return services;
        }
    }
}

