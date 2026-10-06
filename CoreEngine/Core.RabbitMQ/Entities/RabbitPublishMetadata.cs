namespace Core.RabbitMQ.Entities;

public sealed record RabbitPublishMetadata
{
    public required string MessageId { get; init; }
    public string? CorrelationId { get; init; }
    public string? EventName { get; init; }
    public int EventVersion { get; init; } = 1;
    public IReadOnlyDictionary<string, object?>? Headers { get; init; }
}
