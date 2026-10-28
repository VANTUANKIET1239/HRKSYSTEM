using Admin.Application.Configuration;
using Admin.Application.Features.Queries.HealthMonitoring;
using Admin.Application.Interfaces.HealthMonitoring;
using Admin.Application.Services.HealthMonitoring;
using Core.Application.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Admin.Application.DependencyInjection;

public static class AdminApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAdminApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(HealthMonitoringOptions.SectionName).Get<HealthMonitoringOptions>() ?? new();
        if (!ValidTimeout(options.RequestTimeoutSeconds) || !ValidTimeout(options.ProbeTimeoutSeconds) ||
            options.MaxConcurrency is < 1 or > 16 || options.Services.Count is < 1 or > 32 ||
            !options.Services.Any(target => target.Enabled))
            throw new ArgumentException("Invalid HealthMonitoring timeouts, concurrency or service list.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in options.Services.Where(target => target.Enabled))
            if (string.IsNullOrWhiteSpace(target.ServiceName) || string.IsNullOrWhiteSpace(target.ExpectedService) ||
                target.ExpectedService.Length > 100 || !names.Add(target.ServiceName))
                throw new ArgumentException("Each monitored service requires a unique service name and an expected identity.");

        services.AddSingleton<IOptions<HealthMonitoringOptions>>(Options.Create(options));
        services.AddScoped<IServiceHealthAggregator, ServiceHealthAggregator>();
        services.AddCoreApplication(typeof(GetServicesHealthQuery).Assembly);
        return services;
    }

    private static bool ValidTimeout(double seconds) => double.IsFinite(seconds) && seconds is >= 0.01 and <= 60;
}
