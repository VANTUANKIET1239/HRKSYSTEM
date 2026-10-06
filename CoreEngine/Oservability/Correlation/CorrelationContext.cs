namespace Oservability.Correlation;

public interface ICorrelationContext
{
    string? CorrelationId { get; }
}

// One instance per request or message scope, never shared across deliveries.
public sealed class CorrelationContext : ICorrelationContext
{
    public const string HeaderName = "X-Correlation-Id";
    public string? CorrelationId { get; private set; }

    public void Initialize(string? value) => CorrelationId = IsValid(value)
        ? value : Guid.NewGuid().ToString("N");

    public static bool IsValid(string? value) => value is { Length: > 0 and <= 128 } &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
}
