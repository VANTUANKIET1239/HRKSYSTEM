

using Core.Persistence.Database.Interfaces;
using Core.Persistence.Database.Options;



namespace Core.Persistence.Database.Providers
{
    public class SqlServerProvider : IDatabaseHRKProvider
    {
        public void Configure<TContext>(
            IServiceCollection services,
            DatabaseOptions options)
            where TContext : DbContext
        {
            services.AddDbContext<TContext>((provider, dbOptions) =>
            {
                dbOptions.UseSqlServer(
                    options.ConnectionString,
                    sql =>
                    {
                        sql.CommandTimeout(options.CommandTimeout);

                        sql.EnableRetryOnFailure(
                                maxRetryCount: options.RetryCount,
                                maxRetryDelay: TimeSpan.FromSeconds(options.MaxRetryDelay),
                                errorNumbersToAdd: null
                            );
                    });

                dbOptions.AddInterceptors(
                    provider.GetRequiredService<PerformanceInterceptor>()
                    );
            });
        }

    }
}
