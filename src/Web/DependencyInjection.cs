using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Infrastructure.Data;
using SAPFIAI.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddScoped<IUser, CurrentUser>();
        services.AddScoped<IHttpContextInfo, HttpContextInfo>();
        services.AddHttpContextAccessor();

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();

        services.AddExceptionHandler<CustomExceptionHandler>();
        services.AddRazorPages();

        services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        services.AddOpenApi(options =>
        {
            options.ShouldInclude = (desc) => true;

            options.AddDocumentTransformer((doc, ctx, ct) =>
            {
                doc.Info = new()
                {
                    Title = "SAPFIAI API",
                    Version = "v1",
                    Description = "API de autenticación y gestión de usuarios — SAPFIAI"
                };

                doc.Components ??= new();
                doc.Components.SecuritySchemes = new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
                    {
                        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "Ingresa el token JWT. Ejemplo: eyJhbGci..."
                    }
                };
                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, ctx, ct) =>
            {
                var requiresAuth = ctx.Description.ActionDescriptor.EndpointMetadata
                    .OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Any();

                if (requiresAuth)
                {
                    operation.Security =
                    [
                        new Microsoft.OpenApi.OpenApiSecurityRequirement
                        {
                            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", null)] = []
                        }
                    ];
                }
                return Task.CompletedTask;
            });
        });
        services.AddEndpointsApiExplorer();

        services.AddCors(options =>
        {
            if (environment.IsDevelopment())
            {
                options.AddPolicy("DevelopmentCors", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
            }
            else
            {
                var rawOrigins = configuration["AllowedOrigins"] ?? string.Empty;
                var origins = rawOrigins
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(o => !string.IsNullOrWhiteSpace(o))
                    .ToArray();

                if (origins.Length == 0)
                    throw new InvalidOperationException(
                        "AllowedOrigins must be configured in production. Set the 'AllowedOrigins' environment variable.");

                options.AddPolicy("ProductionCors", policy =>
                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());
            }
        });

        return services;
    }
}
