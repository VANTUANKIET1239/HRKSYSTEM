using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Core.HealthChecks;

public sealed class HrkHealthChecksBuilder
{
    private readonly IHealthChecksBuilder checks;
    private readonly HrkHealthCheckOptions options;
    private readonly HashSet<string> names = new(StringComparer.OrdinalIgnoreCase) { "self" };

    internal HrkHealthChecksBuilder(IHealthChecksBuilder checks, HrkHealthCheckOptions options)
    {
        this.checks = checks;
        this.options = options;
    }

    public HrkHealthChecksBuilder AddDependency<TCheck>(string name) where TCheck : class, IHealthCheck
    {
        checks.Services.TryAddScoped<TCheck>();
        return AddDependency(name, provider => provider.GetRequiredService<TCheck>());
    }

    /// <summary>Registers a configured check using the framework's per-check DI scope.</summary>
    public HrkHealthChecksBuilder AddDependency(string name, Func<IServiceProvider, IHealthCheck> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 128 || !names.Add(name))
            throw new ArgumentException("Each dependency check requires a unique name of at most 128 characters.", nameof(name));
        options.Dependencies.TryGetValue(name, out var settings);
        if (settings?.Enabled == false) return this;
        var seconds = settings?.TimeoutSeconds ?? options.DefaultTimeoutSeconds;
        if (!double.IsFinite(seconds) || seconds is < 0.01 or > 60)
            throw new ArgumentException($"Invalid health check timeout for '{name}'.");
        checks.Add(new HealthCheckRegistration(name, factory, HealthStatus.Unhealthy,
            [HealthKinds.Dependency], TimeSpan.FromSeconds(seconds)));
        return this;
    }
}
