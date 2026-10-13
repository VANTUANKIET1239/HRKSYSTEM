using Core.Persistence.SqlExecutor;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Persistence.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddRepositoryUOW<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IUnitOfWork<TDbContext>, UnitOfWork<TDbContext>>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IUnitOfWork<TDbContext>>());
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(ReadOnlyRepository<,>));
        return services;
    }

    /// <summary>Opt in only when consumers inject IRepository&lt;T&gt; directly.</summary>
    public static IServiceCollection AddRepositoryAdapters(this IServiceCollection services)
    {
        services.TryAddScoped(typeof(IRepository<>), typeof(RepositoryAdapter<>));
        services.TryAddScoped(typeof(IReadOnlyRepository<>), typeof(ReadOnlyRepositoryAdapter<>));
        return services;
    }

    /// <summary>Optional direct injection alias. Requires AddRepositoryUOW; shares its executor and transaction.</summary>
    public static IServiceCollection AddCoreSqlExecutor(this IServiceCollection services)
    {
        services.TryAddScoped<ISqlExecutor>(sp => sp.GetRequiredService<IUnitOfWork>().Sql);
        return services;
    }
}
