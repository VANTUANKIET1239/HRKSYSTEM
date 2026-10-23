using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Core.InternalApi;

public static class InternalApiRegistration
{
    public static IServiceCollection AddInternalApi(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new InternalApiOptions();
        configuration.GetSection(InternalApiOptions.SectionName).Bind(options);
        return services.AddInternalApi(options);
    }

    public static IServiceCollection AddInternalApi(this IServiceCollection services, InternalApiOptions options)
    {
        if (services.Any(x => x.ServiceType == typeof(IInternalApiClient)))
            throw new InvalidOperationException("Register InternalApi once per service provider.");
        var snapshot = JsonSerializer.Deserialize<InternalApiOptions>(JsonSerializer.Serialize(options))!;
        snapshot.Services = new(snapshot.Services, StringComparer.OrdinalIgnoreCase);
        Validate(snapshot);
        services.AddHttpClient();
        foreach (var (serviceName, service) in snapshot.Services)
        {
            var client = services.AddHttpClient(InternalApiClientNames.Rest(serviceName), http =>
            {
                http.BaseAddress = new Uri(service.BaseAddress!);
                http.Timeout = Timeout.InfiniteTimeSpan;
            }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            });
            // Own resilience for these clients; preserve global correlation and other non-resilience handlers.
            client.ConfigureAdditionalHttpMessageHandlers((handlers, _) =>
            {
                for (var i = handlers.Count - 1; i >= 0; i--)
                    if (handlers[i] is ResilienceHandler) handlers.RemoveAt(i);
            });
            client.AddResilienceHandler("internal-api", pipeline =>
            {
                // Policy exists once per service. Each request decides whether it may be retried.
                if (service.Retry.MaxRetries > 0)
                    pipeline.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = service.Retry.MaxRetries,
                        Delay = TimeSpan.FromMilliseconds(service.Retry.RetryDelayMilliseconds),
                        MaxDelay = TimeSpan.FromMilliseconds(Math.Max(2000, service.Retry.RetryDelayMilliseconds)),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        ShouldRetryAfterHeader = true,
                        ShouldHandle = args => ValueTask.FromResult(
                            args.Context.GetRequestMessage()?.Options.TryGetValue(InternalApiResilience.RetryAllowed, out var allowed) == true &&
                            allowed && InternalApiResilience.IsTransientHttp(args.Outcome)),
                        OnRetry = args =>
                        {
                            args.Outcome.Result?.Dispose();
                            return default;
                        }
                    });
                var circuit = service.CircuitBreaker;
                if (circuit.Enabled)
                    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        FailureRatio = circuit.FailureRatio,
                        MinimumThroughput = circuit.MinimumThroughput,
                        SamplingDuration = TimeSpan.FromSeconds(circuit.SamplingDurationSeconds),
                        BreakDuration = TimeSpan.FromSeconds(circuit.BreakDurationSeconds),
                        ShouldHandle = args => ValueTask.FromResult(InternalApiResilience.IsTransientHttp(args.Outcome))
                    });
            });
        }
        services.AddSingleton<IOptions<InternalApiOptions>>(Options.Create(snapshot));
        services.TryAddScoped<IInternalApiCredentialsProvider, ConfiguredInternalApiCredentialsProvider>();
        services.TryAddScoped<IInternalApiMetadataProvider, NoInternalApiMetadata>();
        services.AddScoped<IInternalApiClient, InternalApiClient>();
        return services;
    }

    internal static bool ValidTimeout(double value) => double.IsFinite(value) && value is >= 0.01 and <= 3600;

    private static void Validate(InternalApiOptions options)
    {

        if (options.MaxBufferedBytes is <= 0 or > int.MaxValue)
            throw new ArgumentException(
                "InternalApi:MaxBufferedBytes must be between 1 and 2147483647.");

        foreach (var (name, service) in options.Services)
        {
            InternalApiClientNames.ValidateServiceName(name);
            if (!Uri.TryCreate(service.BaseAddress, UriKind.Absolute, out var address) ||
                address.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(address.UserInfo) ||
                !string.IsNullOrEmpty(address.Query) || !string.IsNullOrEmpty(address.Fragment))
                throw new ArgumentException($"Invalid BaseAddress for internal service '{name}'. Use an absolute HTTP(S) address without credentials, query or fragment.");
            if (!ValidTimeout(service.TimeoutSeconds))
                throw new ArgumentException($"Invalid timeout for internal service '{name}'.");
            var retry = service.Retry;
            if (retry.MaxRetries is < 0 or > 4 || retry.Enabled && retry.MaxRetries == 0 ||
                retry.RetryDelayMilliseconds is < 1 or > 10000)
                throw new ArgumentException($"Invalid retry policy for internal service '{name}'.");
            var circuit = service.CircuitBreaker;
            if (!double.IsFinite(circuit.FailureRatio) || circuit.FailureRatio is <= 0 or > 1 ||
                circuit.MinimumThroughput is < 2 or > 10000 ||
                !ValidTimeout(circuit.SamplingDurationSeconds) || circuit.SamplingDurationSeconds < 0.5 ||
                !ValidTimeout(circuit.BreakDurationSeconds) || circuit.BreakDurationSeconds < 0.5)
                throw new ArgumentException($"Invalid circuit breaker for internal service '{name}'.");
        }
    }
}
