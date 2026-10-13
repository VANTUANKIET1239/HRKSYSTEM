using Core.AUTH.DependencyInjection;
using Core.AUTH.Jwt;
using Core.Application.CQRS;
using Core.Application.DependencyInjection;
using Core.Caching;
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
using Microsoft.Extensions.DependencyInjection;
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
            ["JwtSettings:Issuer"] = "test"
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
        Assert.Null(scope.ServiceProvider.GetService<ICacheService>());
    }

    [Fact]
    public void RedisAndLogging_ResolveWithoutJwtOrDatabase()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreLogging();
        services.AddHrkRedisCache(Configuration());
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICacheService>());
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
