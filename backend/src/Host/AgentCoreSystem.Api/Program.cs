// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Text;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AgentCoreSystem.Api.Extensions;
using AgentCoreSystem.Api.Middleware;
using AgentCoreSystem.Api.Swagger;
using AgentCoreSystem.Module.Chat.Infrastructure;
using AgentCoreSystem.Module.System.Application.Common;
using AgentCoreSystem.Shared.Application;
using AgentCoreSystem.Shared.Infrastructure;
using AgentCoreSystem.Shared.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AgentCoreSystem.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Cấu hình Exception Handler & Problem Details theo chuẩn RFC 7807
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            // 2. Cấu hình Đa ngôn ngữ (i18n) hỗ trợ tiếng Việt và tiếng Anh
            builder.Services.AddLocalization();
            var supportedCultures = new[] { "vi-VN", "en-US" };
            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                options.SetDefaultCulture("vi-VN")
                       .AddSupportedCultures(supportedCultures)
                       .AddSupportedUICultures(supportedCultures);
            });

            // 3. Cấu hình CORS (Đọc từ Configuration theo nguyên tắc Fail-Fast)
            var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
                ?? throw new InvalidOperationException("Lỗi cấu hình hệ thống: Thiếu thiết lập 'CorsSettings:AllowedOrigins' trong cấu hình ứng dụng.");

            if (allowedOrigins.Length == 0)
            {
                throw new InvalidOperationException("Lỗi cấu hình hệ thống: 'CorsSettings:AllowedOrigins' phải chứa ít nhất một địa chỉ Origin.");
            }

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowConfiguredOrigins", policy =>
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        policy.SetIsOriginAllowed(_ => true)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    }
                    else
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    }
                });
            });

            // 4. Cấu hình JWT Bearer Authentication (Tuân thủ Fail-Fast & Zero Hardcoded Secrets)
            var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
            var jwtSettings = jwtSection.Get<JwtSettings>() 
                ?? throw new InvalidOperationException("Lỗi cấu hình hệ thống: Thiếu section 'JwtSettings' trong cấu hình ứng dụng.");

            if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
            {
                throw new InvalidOperationException("Lỗi cấu hình bảo mật: 'JwtSettings:Secret' không được để trống và phải có độ dài tối thiểu 32 ký tự (256 bits).");
            }

            if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
            {
                throw new InvalidOperationException("Lỗi cấu hình hệ thống: 'JwtSettings:Issuer' không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
            {
                throw new InvalidOperationException("Lỗi cấu hình hệ thống: 'JwtSettings:Audience' không được để trống.");
            }

            if (jwtSettings.ExpiryMinutes <= 0)
            {
                throw new InvalidOperationException("Lỗi cấu hình hệ thống: 'JwtSettings:ExpiryMinutes' phải là số nguyên dương (> 0).");
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization();

            // 5. Đăng ký các tầng Shared Building Blocks
            builder.Services.AddSharedApplication();
            builder.Services.AddSharedInfrastructure(builder.Configuration);
            builder.Services.AddSharedPersistence();

            // 6. Đăng ký các Phân hệ nghiệp vụ (Modular Monolith)
            builder.Services.AddSystemModule(builder.Configuration);
            builder.Services.AddOrganizationModule(builder.Configuration);
            builder.Services.AddChatModule(builder.Configuration);
            builder.Services.AddTasksModule(builder.Configuration);

            // 7. Cấu hình API Versioning (/api/v1/...)
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            // 6. Cấu hình OpenAPI / Swagger v3
            builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // 7. Pipeline Middleware
            app.UseExceptionHandler();

            var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(localizationOptions);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerEndpoint(
                            $"/swagger/{description.GroupName}/swagger.json",
                            $"AgentCoreSystem API {description.GroupName.ToUpperInvariant()}");
                    }
                });
            }

            app.UseCors("AllowConfiguredOrigins");
            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            // Hangfire Dashboard
            app.UseHangfireDashboard("/hangfire");

            app.MapControllers();
            app.MapChatHub();

            // 8. Tự động khởi tạo dữ liệu mẫu (Seeding)
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var seeder = scope.ServiceProvider.GetRequiredService<AgentCoreSystem.Module.System.Infrastructure.Persistence.Seeds.SystemDataSeeder>();
                    seeder.SeedAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(ex, "Chưa thể kết nối CSDL để Seed dữ liệu khi khởi động. Ứng dụng vẫn tiếp tục chạy.");
                }
            }

            app.Run();
        }
    }
}

