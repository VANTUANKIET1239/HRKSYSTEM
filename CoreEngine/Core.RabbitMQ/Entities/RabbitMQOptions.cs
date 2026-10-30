namespace Core.RabbitMQ.Entities;

public sealed partial class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    public Dictionary<string, PublisherOptions> Publishers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, ConsumerOptions> Consumers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Backward-compatible single-endpoint configuration.
    public PublisherOptions Publisher { get; set; } = new();
    public ConsumerOptions Consumer { get; set; } = new();

    public PublisherOptions ResolvePublisher(string? name = null)
    {
        if (!string.IsNullOrWhiteSpace(name) && Publishers.TryGetValue(name, out var named))
        {
            return named;
        }

        if (string.IsNullOrWhiteSpace(name) && Publishers.Count == 1)
        {
            return Publishers.Values.Single();
        }

        if (string.IsNullOrWhiteSpace(name) && Publishers.Count > 1)
        {
            throw new InvalidOperationException(
                "More than one RabbitMQ publisher is configured. Specify publisherName when publishing.");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            return Publisher;
        }

        throw new InvalidOperationException($"RabbitMQ publisher '{name}' is not configured.");
    }

    public ConsumerOptions ResolveConsumer(string nameOrLegacyQueue)
    {
        if (Consumers.TryGetValue(nameOrLegacyQueue, out var named))
        {
            return named;
        }

        return Consumer with { Queue = nameOrLegacyQueue };
    }

    public IEnumerable<KeyValuePair<string, PublisherOptions>> GetPublishers() =>
        Publishers.Count > 0
            ? Publishers
            : new[] { new KeyValuePair<string, PublisherOptions>("Default", Publisher) };

    public IEnumerable<KeyValuePair<string, ConsumerOptions>> GetConsumers() =>
        Consumers.Count > 0
            ? Consumers
            : new[] { new KeyValuePair<string, ConsumerOptions>("Default", Consumer) };
}
