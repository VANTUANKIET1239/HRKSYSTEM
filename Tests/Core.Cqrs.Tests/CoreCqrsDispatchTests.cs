using System.Security.Claims;
using Core.Application.CQRS;
using Core.Application.DependencyInjection;
using Core.Application.Validation;
using Core.Common.Constants.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using Core.Web.CQRS;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Core.Cqrs.Tests;

public sealed partial class CoreCqrsDispatchTests
{
    [Fact]
    public async Task DispatcherRunsTypedQueryAndCommandBasesWithSharedUserSessionAndScopedState()
    {
        await using var provider = Provider();
        using var scope = provider.CreateScope();
        var session = Guid.NewGuid();
        SetContext(scope.ServiceProvider, session.ToString());
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICqrsDispatcher>();
        var changed = await dispatcher.CommandAsync(new ChangeValueCommand(7));
        Assert.True(changed.Success);
        Assert.Equal("admin-1", changed.Data!.UserId);
        Assert.Equal(session, changed.Data.SessionId);
        Assert.Equal(7, changed.Data.Value);
        var queried = await dispatcher.QueryAsync(new ContextQuery("current"));
        Assert.Equal(changed.Data, queried.Data);
        using var otherScope = provider.CreateScope();
        var other = await otherScope.ServiceProvider.GetRequiredService<ICqrsDispatcher>().QueryAsync(new ContextQuery("other"));
        Assert.Equal(0, other.Data!.Value);
    }

    [Fact]
    public async Task DispatcherKeepsQueryAndCommandValidationAheadOfHandlers()
    {
        await using var provider = Provider();
        using var scope = provider.CreateScope();
        SetContext(scope.ServiceProvider, Guid.NewGuid().ToString());
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICqrsDispatcher>();
        var query = await Assert.ThrowsAsync<RequestValidationException>(() => dispatcher.QueryAsync(new ContextQuery("")));
        var command = await Assert.ThrowsAsync<RequestValidationException>(() => dispatcher.CommandAsync(new ChangeValueCommand(0)));
        Assert.Contains(nameof(ContextQuery.Label), query.Errors.Keys);
        Assert.Contains(nameof(ChangeValueCommand.Amount), command.Errors.Keys);
        var state = scope.ServiceProvider.GetRequiredService<State>();
        Assert.Equal(0, state.QueryCalls);
        Assert.Equal(0, state.CommandCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid-session")]
    public async Task TypedBasesPreserveExpectedSessionErrors(string? session)
    {
        await using var provider = Provider();
        using var scope = provider.CreateScope();
        SetContext(scope.ServiceProvider, session);
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICqrsDispatcher>();
        var query = await Assert.ThrowsAsync<BusinessException>(() => dispatcher.QueryAsync(new ContextQuery("session")));
        var command = await Assert.ThrowsAsync<BusinessException>(() => dispatcher.CommandAsync(new ChangeValueCommand(1)));
        Assert.Equal("INVALID_SESSION", query.Code);
        Assert.Equal("INVALID_SESSION", command.Code);
        Assert.Equal(BusinessErrorKind.Unauthorized, query.Kind);
        Assert.Equal(0, scope.ServiceProvider.GetRequiredService<State>().Value);
    }

    [Fact]
    public async Task DispatcherPropagatesCancellationIntoTheQueryHandler()
    {
        await using var provider = Provider();
        using var scope = provider.CreateScope();
        using var cancellation = new CancellationTokenSource();
        var pending = scope.ServiceProvider.GetRequiredService<ICqrsDispatcher>()
            .QueryAsync(new WaitingQuery(), cancellation.Token);
        var state = scope.ServiceProvider.GetRequiredService<State>();
        await state.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.True(state.Cancelled);
    }

    [Fact]
    public async Task AlreadyCancelledCommandDoesNotRunTheHandler()
    {
        await using var provider = Provider();
        using var scope = provider.CreateScope();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await scope.ServiceProvider
            .GetRequiredService<ICqrsDispatcher>().CommandAsync(new ChangeValueCommand(1), cancellation.Token));
        Assert.Equal(0, scope.ServiceProvider.GetRequiredService<State>().CommandCalls);
    }

    private static ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddScoped<State>();
        services.AddCoreApplication(typeof(CoreCqrsDispatchTests).Assembly);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static void SetContext(IServiceProvider services, string? session)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "admin-1") };
        if (session is not null) claims.Add(new(Constants.JSON_WEB_TOKEN.SESSIONID, session));
        services.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
    }
}
