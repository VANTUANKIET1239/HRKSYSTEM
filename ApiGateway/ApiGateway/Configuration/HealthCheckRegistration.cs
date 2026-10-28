using Core.HealthChecks;
using Core.InternalApi;
using ApiGateway.HealthChecks.Dependencies;
using Oservability;

namespace ApiGateway.Configuration;

public static class HealthCheckRegistration
{
    public static void AddGatewayHealthChecks(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(GatewayHealthCheckOptions.SectionName)
            .Get<GatewayHealthCheckOptions>() ?? new();
        var internalApi = builder.Configuration.GetSection(InternalApiOptions.SectionName)
            .Get<InternalApiOptions>() ?? new();
        var serviceNames = internalApi.Services.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, target) in options.Dependencies)
            if (target is null || string.IsNullOrWhiteSpace(target.ServiceName) ||
                !serviceNames.Contains(target.ServiceName) || string.IsNullOrWhiteSpace(target.ExpectedService) ||
                target.ExpectedService.Length > 100 || string.IsNullOrWhiteSpace(target.Path) ||
                !target.Path.StartsWith('/') || target.Path.StartsWith("//", StringComparison.Ordinal) ||
                target.Path.Any(char.IsControl) || target.Path.Contains('\\') || target.Path.Contains('#'))
                throw new ArgumentException($"Invalid GatewayHealthChecks dependency '{name}'. Configure an InternalApi service, expected identity and root-relative health path.");

        builder.Services.AddHrkInternalApi(builder.Configuration);
        var checks = builder.Services.AddHrkHealthChecks("ApiGateway", builder.Configuration);
        foreach (var (name, target) in options.Dependencies)
            checks.AddDependency(name, provider => new DownstreamServiceHealthCheck(
                provider.GetRequiredService<IInternalApiClient>(), target));
    }
}
