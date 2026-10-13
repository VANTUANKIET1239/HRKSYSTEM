using Core.Persistence.Database.Options;

namespace Core.Persistence.Database.Interfaces
{
    public interface IDatabaseHRKProvider
    {
        void Configure<TContext>(
            IServiceCollection services,
            DatabaseOptions options)
            where TContext : DbContext;
    }
}
