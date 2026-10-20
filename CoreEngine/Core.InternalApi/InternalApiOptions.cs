namespace Core.InternalApi;

public sealed class InternalApiOptions
{
    public const string SectionName = "InternalApi";
    public Dictionary<string, InternalServiceOptions> Services { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class InternalServiceOptions
{
    public string? BaseAddress { get; set; }
    // Full header value, e.g. "Bearer <token>". Supply through secret/environment configuration.
    public string? Authorization { get; set; }
    public double TimeoutSeconds { get; set; } = 5;
    public InternalRetryOptions Retry { get; set; } = new();
    public InternalCircuitBreakerOptions CircuitBreaker { get; set; } = new();
}

public sealed class InternalCircuitBreakerOptions
{
    public bool Enabled { get; set; } = true;
    public double FailureRatio { get; set; } = 0.5;
    public int MinimumThroughput { get; set; } = 10;
    public double SamplingDurationSeconds { get; set; } = 30;
    public double BreakDurationSeconds { get; set; } = 5;
}

public sealed class InternalRetryOptions
{
    public bool Enabled { get; set; }
    public int MaxRetries { get; set; } = 2;
    public int RetryDelayMilliseconds { get; set; } = 200;
}

public sealed class InternalCallOptions
{
    // Overrides the credentials provider for this call only; never copied to shared default headers.
    public string? Authorization { get; init; }
    public double? TimeoutSeconds { get; init; }
    // Null uses service defaults for GET/HEAD/OPTIONS; writes require explicit opt-in and an idempotency key.
    public bool? RetryEnabled { get; init; }
    public string? CorrelationId { get; init; }
    public string? IdempotencyKey { get; init; }
    // Only application metadata (x-*); transport and authentication headers are managed separately.
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public readonly record struct InternalApiEmpty;
