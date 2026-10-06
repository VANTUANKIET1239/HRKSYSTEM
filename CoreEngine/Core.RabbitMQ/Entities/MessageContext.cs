namespace Core.RabbitMQ.Entities;

// Transport metadata for the scoped handler; independent of business payload shape.
public sealed class MessageContext
{
    public string? MessageId { get; internal set; }
    public string? CorrelationId { get; internal set; }
    public string? TraceId { get; internal set; }
}
