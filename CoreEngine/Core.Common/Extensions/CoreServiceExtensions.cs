
using Core.Common.Constants.Common;
using Core.Common.Cookie;
using Core.Common.JwtHandler;
using Core.Common.JwtHandler.Entities;
using Core.Common.Repositories;
using Core.Common.SqlExecutor;
using Microsoft.Extensions.Configuration;

namespace Core.Common.Extensions
{
    public static class CoreServiceExtensions
    {
        public static IServiceCollection AddCoreService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICoreCookieService, CoreCookieService>();
            services.AddScoped(typeof(ILoggerService<>), typeof(LoggerService<>));
            services.AddScoped<ISqlExecutor, SqlExecutor.SqlExecutor>();       
            services.AddScoped<IJwtCoreService, JwtCoreService>();
            return services;
        }

        public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection(Constants.AppSettings.Constants.AppSetting_Auth.JWTSETTINGS));

            return services;
        }

        public static IServiceCollection AddRepositoryUOW<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
        {
            services.AddScoped<IUnitOfWork<TDbContext>, UnitOfWork<TDbContext>>();
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IUnitOfWork<TDbContext>>());

            // Register open generic repository with the current DbContext
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(ReadOnlyRepository<,>));

            // Register open generic adapters
            services.AddScoped(typeof(IRepository<>), typeof(RepositoryAdapter<>));
            services.AddScoped(typeof(IReadOnlyRepository<>), typeof(ReadOnlyRepositoryAdapter<>));

            return services;
        }
    }
}
