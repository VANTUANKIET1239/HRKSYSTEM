using System.Text;
using Admin.Application.DependencyInjection;
using Admin.Infrastructure.DependencyInjection;
using Core.HealthChecks;
using Core.Web.Filters;
using Core.Web.Responses;
using HRK.ADMIN.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Oservability;

namespace HRK.ADMIN.Configuration;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddCustomDependency(this WebApplicationBuilder builder)
    {
        builder.AddHrkObservability("HRK.ADMIN");
        AddServices(builder.Services, builder.Configuration);
        return builder;
    }

    public static IServiceCollection AddServices(IServiceCollection services, IConfiguration configuration)
    {
        AddApiServices(services);
        AddSecurityServices(services, configuration);
        AddApplicationServices(services, configuration);
        AddInfrastructureServices(services, configuration);
        AddServiceHealthChecks(services, configuration);
        return services;
    }

    private static void AddApiServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddControllers(options => options.AddHrkApiFilters())
            .AddApplicationPart(typeof(HealthController).Assembly);
        services.AddHrkApiResponses();
        services.AddSwaggerGen();
    }

    private static void AddApplicationServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddAdminApplication(configuration);

    private static void AddInfrastructureServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddAdminInfrastructure(configuration);

    public static void AddSecurityServices(IServiceCollection services, IConfiguration configuration)
    {
        var secret = configuration["JwtSettings:SecretKey"];
        var issuer = configuration["JwtSettings:Issuer"];
        var audience = configuration["JwtSettings:Audience"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32 ||
            string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Configure JwtSettings:SecretKey (at least 32 bytes), Issuer and Audience for Admin.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true, ValidIssuer = issuer,
                ValidateAudience = true, ValidAudience = audience,
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(60)
            };
        });
        services.AddAuthorization(options => options.AddPolicy("HealthAdmin", policy =>
            policy.RequireAuthenticatedUser().RequireRole("Admin")));
    }

    private static void AddServiceHealthChecks(IServiceCollection services, IConfiguration configuration) =>
        services.AddHrkHealthChecks("HRK.ADMIN", configuration);
}
