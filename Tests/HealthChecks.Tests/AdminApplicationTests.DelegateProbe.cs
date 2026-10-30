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
    private sealed class DelegateProbe(Func<MonitoredService, CancellationToken, Task<EndpointHealth>> live, Func<MonitoredService, CancellationToken, Task<EndpointHealth>> dependencies) : IServiceHealthProbe
    {
        public Task<EndpointHealth> CheckLivenessAsync(MonitoredService target, CancellationToken cancellationToken) => live(target, cancellationToken);
        public Task<EndpointHealth> CheckDependenciesAsync(MonitoredService target, CancellationToken cancellationToken) => dependencies(target, cancellationToken);
    }
}
