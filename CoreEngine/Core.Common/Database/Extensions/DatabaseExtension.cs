using Core.Common.Database.Interfaces;
using Core.Common.Database.Options;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common.Database.Extensions
{
    public static class DatabaseExtension
    {
        public static IServiceCollection AddDatabase<TContext>(
            this IServiceCollection services,
            IConfiguration configuration)
            where TContext : DbContext
        {
            services.Configure<DatabaseOptions>(
            configuration.GetSection(Constants.Common.Constants.Database.DATABASE));

            services.AddSingleton<PerformanceInterceptor>();

            var options = configuration
                .GetSection(Constants.Common.Constants.Database.DATABASE)
                .Get<DatabaseOptions>()!;

            var provider = DatabaseFactory.Create(options.Provider);

            provider.Configure<TContext>(
                services,
                options);

            return services;
        }
    }
}
