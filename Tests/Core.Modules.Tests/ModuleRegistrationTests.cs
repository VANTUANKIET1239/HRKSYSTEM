using Core.AUTH.DependencyInjection;
using Core.AUTH.Jwt;
using Core.Application.CQRS;
using Core.Application.DependencyInjection;
using Core.Caching;
using Core.Caching.Hybrid;
using Core.Common.Entity;
using Core.Logging;
using Core.Logging.DependencyInjection;
using Core.Persistence.Database.Extensions;
using Core.Persistence.DependencyInjection;
using Core.Persistence.Repositories;
using Core.Web.Cookies;
using Core.Web.DependencyInjection;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RedisBackendOptions = Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions;
using Xunit;

namespace Core.Modules.Tests;

public class ModuleRegistrationTests
{
    private static IConfiguration Configuration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "SqlServer",
            ["Database:ConnectionString"] = "Server=localhost;Database=Probe;Trusted_Connection=True",
            ["JwtSettings:SecretKey"] = new string('x', 64),
            ["JwtSettings:Issuer"] = "test",
            ["Redis:ConnectionString"] = "localhost:6379,abortConnect=false",
            ["Redis:InstanceName"] = "test:cache:"
        }).Build();

    [Fact]
    public void SharedContracts_DoNotReferenceInfrastructure()
    {
        foreach (var assembly in new[] { typeof(BaseEntity).Assembly, typeof(IUnitOfWork).Assembly })
        {
            Assert.All(assembly.GetReferencedAssemblies(), dependency =>
                Assert.StartsWith("System", dependency.Name!));
        }
    }

    [Fact]
    public async Task Persistence_UsesOneScopedContextAndAdaptersAreOptional()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDatabase<ProbeContext>(Configuration());
        services.AddRepositoryUOW<ProbeContext>();
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IRepository<>));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IJwtCoreService));
        services.AddRepositoryAdapters();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Assert.Same(uow, scope.ServiceProvider.GetRequiredService<IUnitOfWork<ProbeContext>>());
        await scope.ServiceProvider.GetRequiredService<IRepository<Probe>>().AddAsync(new Probe { Id = 1 });
        Assert.Single(scope.ServiceProvider.GetRequiredService<ProbeContext>().ChangeTracker.Entries<Probe>());
    }

    [Fact]
    public void JwtAndCookies_ResolveWithoutPersistenceOrRedis()
    {
        var services = new ServiceCollection();
        services.AddCoreJwt(Configuration());
        services.AddCoreCookies(Configuration());
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtCoreService>();
        Assert.NotEmpty(jwt.GenerateToken([], "test-audience").AccessToken);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICoreCookieService>());
        Assert.Null(scope.ServiceProvider.GetService<IUnitOfWork>());
        Assert.Null(scope.ServiceProvider.GetService<IDistributedCache>());
    }

    [Fact]
    public void RedisHybridAndLogging_ResolveWithoutJwtOrDatabase()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreLogging();
        services.AddHrkRedisCache(Configuration());
        services.AddHrkHybridCache(Configuration());
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        Assert.IsAssignableFrom<RedisCache>(scope.ServiceProvider.GetRequiredService<IDistributedCache>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHybridCacheService>());
        var redisOptions = scope.ServiceProvider.GetRequiredService<IOptions<RedisBackendOptions>>().Value;
        Assert.Equal("localhost:6379,abortConnect=false", redisOptions.Configuration);
        Assert.Equal("test:cache:", redisOptions.InstanceName);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ILoggerService<ModuleRegistrationTests>>());
        Assert.Null(scope.ServiceProvider.GetService<IUnitOfWork>());
        Assert.Null(scope.ServiceProvider.GetService<IJwtCoreService>());
    }

    [Fact]
    public async Task Application_ResolvesHandlersWithoutInfrastructure()
    {
        var services = new ServiceCollection();
        services.AddCoreApplication(typeof(ModuleRegistrationTests).Assembly);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        Assert.Equal(7, await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new ProbeQuery()));
        Assert.Null(scope.ServiceProvider.GetService<IUnitOfWork>());
    }

    public sealed record ProbeQuery : IQuery<int>;
    public sealed class ProbeHandler : IQueryHandler<ProbeQuery, int>
    {
        public Task<int> Handle(ProbeQuery request, CancellationToken cancellationToken) => Task.FromResult(7);
    }
    public sealed class Probe { public int Id { get; set; } }
    public sealed class ProbeContext(DbContextOptions<ProbeContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<Probe>().HasKey(x => x.Id);
    }
}
