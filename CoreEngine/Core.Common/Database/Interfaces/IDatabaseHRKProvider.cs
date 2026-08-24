using Core.Common.Database.Options;

namespace Core.Common.Database.Interfaces
{
    public interface IDatabaseHRKProvider
    {
        void Configure<TContext>(
            IServiceCollection services,
            DatabaseOptions options)
            where TContext : DbContext;
    }
}
