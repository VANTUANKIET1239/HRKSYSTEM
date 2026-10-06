using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Oservability.Tracing;

// Only call with fixed operation names; IDs belong in logs/spans, never metric tags.
public sealed class GameOperation : IDisposable
{
    public static readonly ActivitySource Source = new("HRK.Game");
    private static readonly Meter Meter = new("HRK.Game");
    private static readonly Counter<long> Count = Meter.CreateCounter<long>("game.operation.count");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("game.operation.duration", "s");
    private readonly string _name;
    private readonly long _started = Stopwatch.GetTimestamp();
    private readonly Activity? _activity;
    private string _result = "error";

    public GameOperation(string name)
    {
        _name = name;
        _activity = Source.StartActivity(name);
    }
    public void Complete(string result = "success") => _result = result;
    public void Dispose()
    {
        var tags = new TagList { { "operation", _name }, { "result", _result } };
        Count.Add(1, tags);
        Duration.Record(Stopwatch.GetElapsedTime(_started).TotalSeconds, tags);
        if (_result == "error") _activity?.SetStatus(ActivityStatusCode.Error);
        _activity?.Dispose();
    }
}
