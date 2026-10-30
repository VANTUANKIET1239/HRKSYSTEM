using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace Oservability.Tracing;

public static class RedisTelemetryExtensions
{
    public static IServiceCollection AddHrkRedisTelemetry(this IServiceCollection services)
    {
        var registry = new RedisTelemetry();
        services.AddSingleton(registry);
        services.AddOpenTelemetry().WithTracing(traces => traces.AddRedisInstrumentation()
            .ConfigureRedisInstrumentation(instrumentation => registry.Initialize(instrumentation)));
        return services;
    }
}
