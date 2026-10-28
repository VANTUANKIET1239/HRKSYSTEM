namespace Core.InternalApi;

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
