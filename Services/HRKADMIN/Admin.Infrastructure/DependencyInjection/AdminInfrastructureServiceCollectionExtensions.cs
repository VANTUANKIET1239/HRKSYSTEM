using Admin.Application.Configuration;
using Admin.Application.Interfaces.HealthMonitoring;
using Admin.Infrastructure.HealthMonitoring;
using Core.InternalApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oservability;

namespace Admin.Infrastructure.DependencyInjection;

public static class AdminInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAdminInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var monitoring = configuration.GetSection(HealthMonitoringOptions.SectionName).Get<HealthMonitoringOptions>() ?? new();
        var transports = configuration.GetSection(InternalApiOptions.SectionName).Get<InternalApiOptions>() ?? new();
        transports.Services = new(transports.Services, StringComparer.OrdinalIgnoreCase);
        foreach (var target in monitoring.Services.Where(target => target.Enabled))
        {
            if (!transports.Services.TryGetValue(target.ServiceName, out var transport))
                throw new ArgumentException($"Missing InternalApi configuration for health target '{target.ServiceName}'.");
            // Every probe measures a fresh request, including after previous failures.
            if (transport.Retry.Enabled || transport.Retry.MaxRetries != 0 || transport.CircuitBreaker.Enabled)
                throw new ArgumentException($"Disable retries and circuit breaking for health target '{target.ServiceName}'.");
        }
        services.AddHrkInternalApi(configuration);
        services.AddScoped<IServiceHealthProbe, InternalApiHealthProbe>();
        return services;
    }
}
