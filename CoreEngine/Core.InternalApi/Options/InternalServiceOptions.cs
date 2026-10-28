namespace Core.InternalApi;

public sealed class InternalServiceOptions
{
    public string? BaseAddress { get; set; }
    // Full header value, e.g. "Bearer <token>". Supply through secret/environment configuration.
    public string? Authorization { get; set; }
    public double TimeoutSeconds { get; set; } = 5;
    public InternalRetryOptions Retry { get; set; } = new();
    public InternalCircuitBreakerOptions CircuitBreaker { get; set; } = new();
}
