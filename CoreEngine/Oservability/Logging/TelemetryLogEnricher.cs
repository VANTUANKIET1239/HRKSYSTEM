using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;
using Microsoft.AspNetCore.Http;

namespace Oservability.Logging;

public sealed class TelemetryLogEnricher(IHttpContextAccessor? accessor = null) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is not null)
        {
            // Same IDs as the native Serilog trace fields, useful for property-based queries.
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToHexString()));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToHexString()));
            if (activity.GetTagItem("content.version") is { } version)
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ContentVersion", version));
        }
        if (accessor?.HttpContext?.Items["PlayerId"] is { } playerId)
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("PlayerId", playerId));
    }
}
