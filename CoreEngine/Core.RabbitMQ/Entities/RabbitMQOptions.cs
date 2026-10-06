namespace Core.RabbitMQ.Entities;

public sealed class RabbitMqOptions
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

    public sealed record PublisherOptions
    {
        public string Exchange { get; set; } = "app.exchange";
        public string ExchangeType { get; set; } = "direct";
        public string DefaultRoutingKey { get; set; } = "app.default";
        public bool Durable { get; set; } = true;
    }

    public sealed record ConsumerOptions
    {
        public string Exchange { get; set; } = "";
        public string ExchangeType { get; set; } = "topic";
        public bool ExchangeDurable { get; set; } = true;
        public string Queue { get; set; } = "app.queue";
        public List<string> RoutingKeys { get; set; } = new();
        public ushort PrefetchCount { get; set; } = 20;
        public bool QueueDurable { get; set; } = true;
        public string RetryExchange { get; set; } = "";
        public string RetryQueue { get; set; } = "";
        public int RetryDelayMs { get; set; } = 15000;
        public RetryOptions Retry { get; set; } = new();
        public DeadLetterOptions DeadLetter { get; set; } = new();
        public string DlqExchange { get; set; } = "";
        public string DlqQueue { get; set; } = "";

        public string ResolveExchange(PublisherOptions legacyPublisher) =>
            string.IsNullOrWhiteSpace(Exchange) ? legacyPublisher.Exchange : Exchange;

        public IReadOnlyCollection<string> ResolveRoutingKeys(PublisherOptions legacyPublisher) =>
            RoutingKeys.Count > 0 ? RoutingKeys : new[] { legacyPublisher.DefaultRoutingKey };

        public string ResolveRetryExchange() =>
            string.IsNullOrWhiteSpace(RetryExchange) ? $"{Queue}.retry.exchange" : RetryExchange;

        public string ResolveRetryQueue() =>
            string.IsNullOrWhiteSpace(RetryQueue) ? $"{Queue}.retry" : RetryQueue;

        public string ResolveDlqExchange() =>
            string.IsNullOrWhiteSpace(DlqExchange) ? $"{Queue}.dlq.exchange" : DlqExchange;

        public string ResolveDlqQueue() =>
            string.IsNullOrWhiteSpace(DlqQueue) ? $"{Queue}.dlq" : DlqQueue;

        public IReadOnlyList<int> ResolveRetryDelays()
        {
            var maxRetryCount = Math.Max(0, Retry.MaxRetryCount);
            if (maxRetryCount == 0)
            {
                return Array.Empty<int>();
            }

            var configured = Retry.DelaysMs.Where(delay => delay > 0).ToArray();
            if (configured.Length == 0)
            {
                return Enumerable.Repeat(Math.Max(1, RetryDelayMs), maxRetryCount).ToArray();
            }

            return Enumerable.Range(0, maxRetryCount)
                .Select(index => configured[Math.Min(index, configured.Length - 1)])
                .ToArray();
        }

        public string ResolveRedeliveryExchange() => $"{Queue}.redelivery.exchange";

        public string ResolveRedeliveryRoutingKey() => $"redeliver.{Queue}";

        public string ResolveRetryBucketRoutingKey(int index) => $"retry.{Queue}.{index}";

        public string ResolveRetryBucketQueue(int index, int delayMs) =>
            $"{ResolveRetryQueue()}.v2.{index}.{delayMs}";
    }

    public sealed record RetryOptions
    {
        public int MaxRetryCount { get; set; } = 1;
        public List<int> DelaysMs { get; set; } = new();
        public string UnknownExceptionBehavior { get; set; } = "DeadLetter";
        public int ErrorMaxLength { get; set; } = 2000;
    }

    public sealed record DeadLetterOptions
    {
        public bool Enabled { get; set; } = true;
    }
}
