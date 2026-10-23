namespace Core.InternalApi;

public sealed class InternalRetryOptions
{
    public bool Enabled { get; set; }
    public int MaxRetries { get; set; } = 2;
    public int RetryDelayMilliseconds { get; set; } = 200;
}
