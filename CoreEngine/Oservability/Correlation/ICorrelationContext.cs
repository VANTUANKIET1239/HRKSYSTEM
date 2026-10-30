namespace Oservability.Correlation;

public interface ICorrelationContext
{
    string? CorrelationId { get; }
}
