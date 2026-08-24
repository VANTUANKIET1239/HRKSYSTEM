using Core.Common.Database.Interfaces;
using Core.Common.Database.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common.Database.Providers
{
    public class PostgreSqlProvider : IDatabaseHRKProvider
    {
        public void Configure<TContext>(
            IServiceCollection services,
            DatabaseOptions options)
            where TContext : DbContext
        {
            services.AddDbContext<TContext>((provider, dbOptions) =>
            {
                dbOptions.UseNpgsql(
                    options.ConnectionString,
                    sql =>
                    {
                        sql.CommandTimeout(options.CommandTimeout);

                        sql.EnableRetryOnFailure(
                                maxRetryCount: options.RetryCount,
                                maxRetryDelay: TimeSpan.FromSeconds(options.MaxRetryDelay),
                                errorCodesToAdd: null
                            );
                    });

                dbOptions.AddInterceptors(
                    provider.GetRequiredService<PerformanceInterceptor>()
                    );
            });
        }
    }
}
