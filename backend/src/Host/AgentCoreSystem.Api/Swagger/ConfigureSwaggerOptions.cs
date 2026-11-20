// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AgentCoreSystem.Api.Swagger
{
    /// <summary>
    /// Cấu hình Swagger tự động sinh tài liệu OpenAPI theo từng phiên bản API.
    /// </summary>
    public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
    {
        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, new OpenApiInfo
                {
                    Title = $"AgentCoreSystem Modular Monolith API {description.ApiVersion}",
                    Version = description.ApiVersion.ToString(),
                    Description = "Hệ thống Web API theo kiến trúc Modular Monolith + Clean Architecture + CQRS (MediatR).",
                    Contact = new OpenApiContact
                    {
                        Name = "Phong Minh Dang",
                        Email = "dangminhphong912@gmail.com"
                    }
                });
            }

            // Cấu hình JWT Bearer Security Definition cho Swagger UI
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Nhập chuỗi JWT Token của bạn theo định dạng: Bearer {token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Đọc các file XML comments nếu có
            IncludeXmlCommentsIfExists(options, Assembly.GetExecutingAssembly());
            IncludeXmlCommentsIfExists(options, typeof(AgentCoreSystem.Module.System.Presentation.SystemPresentationExtensions).Assembly);
            IncludeXmlCommentsIfExists(options, typeof(AgentCoreSystem.Module.Organization.Presentation.DependencyInjection).Assembly);
            IncludeXmlCommentsIfExists(options, typeof(AgentCoreSystem.Module.Chat.Presentation.DependencyInjection).Assembly);
        }

        private static void IncludeXmlCommentsIfExists(SwaggerGenOptions options, Assembly assembly)
        {
            var xmlFile = $"{assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }
        }
    }
}

