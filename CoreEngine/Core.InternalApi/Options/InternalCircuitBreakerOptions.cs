namespace Core.InternalApi;

public sealed class InternalCircuitBreakerOptions
{
    public bool Enabled { get; set; } = true;
    public double FailureRatio { get; set; } = 0.5;
    public int MinimumThroughput { get; set; } = 10;
    public double SamplingDurationSeconds { get; set; } = 30;
    public double BreakDurationSeconds { get; set; } = 5;
}
