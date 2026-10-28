using Core.HealthChecks.Checks;
using Core.HealthChecks.Internal;
using Core.HealthChecks.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Core.HealthChecks;

public static class HrkHealthChecksRegistration
{
    public static HrkHealthChecksBuilder AddHrkHealthChecks(this IServiceCollection services, string serviceName,
        IConfiguration? configuration = null)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(HealthServiceIdentity)))
            throw new InvalidOperationException("Register HRK health checks once per service.");
        if (string.IsNullOrWhiteSpace(serviceName) || serviceName.Length > 100)
            throw new ArgumentException("A service name is required.", nameof(serviceName));
        var options = configuration?.GetSection(HrkHealthCheckOptions.SectionName).Get<HrkHealthCheckOptions>() ?? new();
        if (!double.IsFinite(options.DefaultTimeoutSeconds) || options.DefaultTimeoutSeconds is < 0.01 or > 60)
            throw new ArgumentException("HealthChecks:DefaultTimeoutSeconds must be between 0.01 and 60.");
        options.Dependencies = new(options.Dependencies, StringComparer.OrdinalIgnoreCase);
        var instance = options.InstanceId ?? Environment.MachineName;
        if (string.IsNullOrWhiteSpace(instance) || instance.Length > 128)
            throw new ArgumentException("HealthChecks:InstanceId must contain at most 128 characters.");
        services.AddSingleton(new HealthServiceIdentity(serviceName, instance));
        var checks = services.AddHealthChecks().AddCheck("self", new LivenessHealthCheck(), tags: [HealthKinds.Liveness]);
        return new(checks, options);
    }

    public static void MapHrkHealthChecks(this IEndpointRouteBuilder endpoints, bool legacyAliases = true)
    {
        endpoints.MapHealthChecks("/health/live", CreateOptions(HealthKinds.Liveness)).AllowAnonymous();
        endpoints.MapHealthChecks("/health/dependencies", CreateOptions(HealthKinds.Dependency)).AllowAnonymous();
        if (legacyAliases)
        {
            endpoints.MapHealthChecks("/health", CreateOptions(HealthKinds.Dependency)).AllowAnonymous();
            endpoints.MapHealthChecks("/health/ready", CreateOptions(HealthKinds.Dependency)).AllowAnonymous();
        }
    }

    private static HealthCheckOptions CreateOptions(string kind) => new()
    {
        Predicate = registration => registration.Tags.Contains(kind),
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        },
        ResponseWriter = (http, report) => HealthCheckResponseWriter.WriteAsync(http, report, kind)
    };
}
