using System.Diagnostics;
using Admin.Application.Configuration;
using Admin.Application.DTOs.HealthMonitoring;
using Admin.Application.Interfaces.HealthMonitoring;
using Core.HealthChecks;
using Microsoft.Extensions.Options;

namespace Admin.Application.Services.HealthMonitoring;

public sealed class ServiceHealthAggregator(IServiceHealthProbe probe, IOptions<HealthMonitoringOptions> options)
    : IServiceHealthAggregator
{
    public async Task<SystemHealthReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = options.Value;
        var started = Stopwatch.GetTimestamp();
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(TimeSpan.FromSeconds(settings.RequestTimeoutSeconds));
        using var slots = new SemaphoreSlim(settings.MaxConcurrency);
        var tasks = settings.Services.Where(target => target.Enabled).Select(CheckBoundedAsync).ToArray();
        var results = await Task.WhenAll(tasks);
        cancellationToken.ThrowIfCancellationRequested();
        return new()
        {
            Status = results.All(service => service.Status == HealthStatuses.Healthy)
                ? HealthStatuses.Healthy : HealthStatuses.Unhealthy,
            CheckedAtUtc = DateTimeOffset.UtcNow,
            DurationMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            Services = results
        };

        async Task<MonitoredServiceHealth> CheckBoundedAsync(MonitoredService target)
        {
            var entered = false;
            try
            {
                await slots.WaitAsync(budget.Token);
                entered = true;
                var live = await RunProbeAsync(token => probe.CheckLivenessAsync(target, token), budget.Token, cancellationToken);
                var dependencies = live.Report is null ? null :
                    await RunProbeAsync(token => probe.CheckDependenciesAsync(target, token), budget.Token, cancellationToken);
                return new()
                {
                    Target = target.ServiceName, Service = target.ExpectedService, Liveness = live, Dependencies = dependencies,
                    Status = live.Status == HealthStatuses.Healthy && dependencies?.Status == HealthStatuses.Healthy
                        ? HealthStatuses.Healthy : HealthStatuses.Unhealthy
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new()
                {
                    Target = target.ServiceName, Service = target.ExpectedService, Status = HealthStatuses.Unhealthy,
                    Liveness = new() { ErrorCode = "Timeout" }
                };
            }
            finally { if (entered) slots.Release(); }
        }
    }

    private static async Task<EndpointHealth> RunProbeAsync(Func<CancellationToken, Task<EndpointHealth>> run,
        CancellationToken budget, CancellationToken caller)
    {
        var started = Stopwatch.GetTimestamp();
        try { return await run(budget); }
        catch (OperationCanceledException) when (!caller.IsCancellationRequested)
        {
            // Retain the other completed endpoint when the overall deadline expires.
            return new() { ErrorCode = "Timeout", DurationMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds };
        }
    }
}
