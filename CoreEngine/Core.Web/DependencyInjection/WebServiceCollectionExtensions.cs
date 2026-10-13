using Core.Web.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Web.DependencyInjection;

public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddCoreCookies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.Configure<HRKCookieOptions>(configuration.GetSection("CookieOptions"));
        services.TryAddScoped<ICoreCookieService, CoreCookieService>();
        return services;
    }
}
