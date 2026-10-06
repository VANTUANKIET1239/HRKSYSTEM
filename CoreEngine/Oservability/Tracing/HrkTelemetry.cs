using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace Oservability.Tracing;

public static class HrkTelemetry
{
    public const string SourceName = "HRK.Messaging";
    public const string MeterName = "HRK.Messaging";
    public static readonly ActivitySource Source = new(SourceName);
    public static readonly Meter Meter = new(MeterName);
    public static readonly Counter<long> Processed = Meter.CreateCounter<long>("rabbitmq.message.processed.count");
    public static readonly Counter<long> Failed = Meter.CreateCounter<long>("rabbitmq.message.failed.count");
    public static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("rabbitmq.consumer.duration", "s");

    public static ActivityContext Extract(string? traceParent, string? traceState) =>
        ActivityContext.TryParse(traceParent, traceState, isRemote: true, out var context) ? context : default;

    public static ActivityContext Extract(IDictionary<string, object?>? headers) =>
        Extract(ReadHeader(headers, "traceparent"), ReadHeader(headers, "tracestate"));

    public static void Inject(IDictionary<string, object?> headers)
    {
        if (Activity.Current is not { IdFormat: ActivityIdFormat.W3C } activity) return;
        headers["traceparent"] = activity.Id;
        if (!string.IsNullOrWhiteSpace(activity.TraceStateString))
            headers["tracestate"] = activity.TraceStateString;
        else headers.Remove("tracestate");
    }

    private static string? ReadHeader(IDictionary<string, object?>? headers, string name) =>
        headers?.TryGetValue(name, out var value) == true ? value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => null
        } : null;
}
