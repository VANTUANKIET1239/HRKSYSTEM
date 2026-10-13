using Core.AUTH.Jwt;
using Core.AUTH.Jwt.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.AUTH.DependencyInjection;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddCoreJwt(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.TryAddScoped<IJwtCoreService, JwtCoreService>();
        return services;
    }
}
