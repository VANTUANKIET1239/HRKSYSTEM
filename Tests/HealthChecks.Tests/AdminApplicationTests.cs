using Admin.Application.Configuration;
using Admin.Application.DependencyInjection;
using Admin.Application.DTOs.HealthMonitoring;
using Admin.Application.Features.Queries.HealthMonitoring;
using Admin.Application.Interfaces.HealthMonitoring;
using Admin.Application.Services.HealthMonitoring;
using Core.HealthChecks;
using Core.Application.CQRS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace HealthChecks.Tests;

public sealed partial class AdminApplicationTests
{
    [Fact]
    public async Task ApplicationQueryRunsWithoutApiHostOrHttpClientRegistrations()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HealthMonitoring:Services:0:ServiceName"] = "worker",
            ["HealthMonitoring:Services:0:ExpectedService"] = "WORKER"
        }).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddHttpContextAccessor();
        services.AddAdminApplication(config);
        services.AddSingleton<IServiceHealthProbe>(new DelegateProbe(
            (_, _) => Task.FromResult(Healthy(HealthKinds.Liveness)),
            (_, _) => Task.FromResult(Healthy(HealthKinds.Dependency))));
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ICqrsDispatcher>().QueryAsync(new GetServicesHealthQuery());
        Assert.True(result.Success);
        Assert.Equal(HealthStatuses.Healthy, result.Data!.Status);
        Assert.Equal("worker", Assert.Single(result.Data.Services).Target);
    }

    [Fact]
    public async Task DisabledTargetsAreExcludedAndUnreachableLivenessSkipsDependencies()
    {
        var liveCalls = 0;
        var dependencyCalls = 0;
        var probe = new DelegateProbe((target, _) =>
        {
            Assert.Equal("down", target.ServiceName);
            liveCalls++;
            return Task.FromResult(new EndpointHealth { ErrorCode = "Unavailable" });
        }, (_, _) =>
        {
            dependencyCalls++;
            return Task.FromResult(Healthy(HealthKinds.Dependency));
        });
        var aggregator = new ServiceHealthAggregator(probe, Options.Create(new HealthMonitoringOptions
        {
            Services = [new() { ServiceName = "disabled", Enabled = false }, new() { ServiceName = "down", ExpectedService = "DOWN" }]
        }));
        var result = await aggregator.CheckAsync();
        Assert.Equal(1, liveCalls);
        Assert.Equal(0, dependencyCalls);
        Assert.Equal(HealthStatuses.Unhealthy, result.Status);
        Assert.Null(Assert.Single(result.Services).Dependencies);
    }

    [Fact]
    public async Task CallerCancellationPropagatesToApplicationProbe()
    {
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new DelegateProbe(async (_, ct) =>
        {
            try { await Task.Delay(Timeout.Infinite, ct); }
            finally { if (ct.IsCancellationRequested) cancelled.TrySetResult(); }
            return Healthy(HealthKinds.Liveness);
        }, (_, _) => Task.FromResult(Healthy(HealthKinds.Dependency)));
        var aggregator = new ServiceHealthAggregator(probe, Options.Create(new HealthMonitoringOptions
        {
            Services = [new() { ServiceName = "worker", ExpectedService = "WORKER" }]
        }));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => aggregator.CheckAsync(cancellation.Token));
        Assert.True(cancelled.Task.IsCompletedSuccessfully);
    }

    private static EndpointHealth Healthy(string kind) => new()
    {
        Status = HealthStatuses.Healthy, HttpStatusCode = 200,
        Report = new()
        {
            Service = "WORKER", InstanceId = "worker-1", Kind = kind,
            Status = HealthStatuses.Healthy, CheckedAtUtc = DateTimeOffset.UtcNow,
            Checks = [new() { Name = kind == HealthKinds.Liveness ? "self" : "database", Status = HealthStatuses.Healthy }]
        }
    };
}
