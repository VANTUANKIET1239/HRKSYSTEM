using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace Oservability.Tracing;

// Attach the actual cache/backplane connections lazily. Telemetry setup never connects Redis.
public sealed class RedisTelemetry
{
    private readonly object _gate = new();
    private StackExchangeRedisInstrumentation? _instrumentation;
    private readonly List<IConnectionMultiplexer> _connections = [];

    internal void Initialize(StackExchangeRedisInstrumentation instrumentation)
    {
        lock (_gate)
        {
            _instrumentation = instrumentation;
            foreach (var connection in _connections) instrumentation.AddConnection(connection);
        }
    }

    public async Task<IConnectionMultiplexer> ConnectAsync(string configuration)
    {
        var connection = await ConnectionMultiplexer.ConnectAsync(configuration);
        lock (_gate)
        {
            _connections.Add(connection);
            _instrumentation?.AddConnection(connection);
        }
        return connection; // Ownership stays with RedisCache/SignalR.
    }
}

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
