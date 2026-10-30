using Core.Persistence.DependencyInjection;
using Core.Persistence.Repositories;
using Core.Persistence.SqlExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Core.Modules.Tests;

public partial class SqlExecutorTests
{
    [Fact]
    public void DirectInjectionAliasesTheScopedUnitOfWorkExecutor()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ProbeContext>(options => options.UseSqlServer("Server=localhost;Database=Unused;Integrated Security=true"));
        services.AddRepositoryUOW<ProbeContext>();
        services.AddCoreSqlExecutor();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var uow = first.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Assert.Same(uow.Sql, first.ServiceProvider.GetRequiredService<ISqlExecutor>());
        Assert.Same(uow.Sql, first.ServiceProvider.GetRequiredService<IUnitOfWork<ProbeContext>>().Sql);
        Assert.NotSame(uow.Sql, second.ServiceProvider.GetRequiredService<IUnitOfWork>().Sql);
    }

    [Theory]
    [InlineData("dbo.View; DROP TABLE Players")]
    [InlineData("dbo.View --comment")]
    [InlineData("OtherDb.dbo.View")]
    [InlineData("[dbo].[View]")]
    [InlineData("dbo..View")]
    [InlineData("dbo.")]
    [InlineData("dbo.View()")]
    [InlineData(" ")]
    public async Task RejectsSqlFragmentsBeforeOpeningAConnection(string name)
    {
        await using var db = Context();
        await using var uow = new UnitOfWork<ProbeContext>(db);
        await Assert.ThrowsAnyAsync<ArgumentException>(() => uow.Sql.QueryViewAsync<int>(name));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => uow.Sql.QueryProcedureAsync<int>(name));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => uow.Sql.ExecuteProcedureAsync(name));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => uow.Sql.QueryFunctionAsync<int>(name));
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Fact]
    public async Task PreCancelledCallsDoNotOpenTheConnection()
    {
        await using var db = Context();
        await using var uow = new UnitOfWork<ProbeContext>(db);
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uow.Sql.QueryViewAsync<int>("dbo.View", source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uow.Sql.QueryFunctionAsync<int>("dbo.Function", cancellationToken: source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uow.Sql.QueryProcedureAsync<int>("dbo.Procedure", cancellationToken: source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uow.Sql.ExecuteProcedureAsync("dbo.Procedure", cancellationToken: source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uow.Sql.QueryAsync<int>("SELECT 1", cancellationToken: source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uow.Sql.ExecuteAsync("SELECT 1", cancellationToken: source.Token));
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Fact]
    public async Task NamedObjectCallsRejectOtherProvidersExplicitly()
    {
        await using var db = new ProbeContext(new DbContextOptionsBuilder<ProbeContext>()
            .UseNpgsql("Host=localhost;Database=Unused;Username=Unused").Options);
        await using var uow = new UnitOfWork<ProbeContext>(db);
        await Assert.ThrowsAsync<NotSupportedException>(() => uow.Sql.QueryViewAsync<int>("dbo.View"));
        await Assert.ThrowsAsync<NotSupportedException>(() => uow.Sql.QueryFunctionAsync<int>("dbo.Function"));
        await Assert.ThrowsAsync<NotSupportedException>(() => uow.Sql.QueryProcedureAsync<int>("dbo.Procedure"));
        await Assert.ThrowsAsync<NotSupportedException>(() => uow.Sql.ExecuteProcedureAsync("dbo.Procedure"));
    }

    private static ProbeContext Context() => new(new DbContextOptionsBuilder<ProbeContext>()
        .UseSqlServer("Server=localhost;Database=Unused;Integrated Security=true").Options);
}
