namespace Core.RabbitMQ.Entities;
public sealed partial class RabbitMqOptions
{
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

        public string ResolveExchange(PublisherOptions legacyPublisher) => string.IsNullOrWhiteSpace(Exchange) ? legacyPublisher.Exchange : Exchange;
        public IReadOnlyCollection<string> ResolveRoutingKeys(PublisherOptions legacyPublisher) => RoutingKeys.Count > 0 ? RoutingKeys : new[]
        {
            legacyPublisher.DefaultRoutingKey
        };
        public string ResolveRetryExchange() => string.IsNullOrWhiteSpace(RetryExchange) ? $"{Queue}.retry.exchange" : RetryExchange;
        public string ResolveRetryQueue() => string.IsNullOrWhiteSpace(RetryQueue) ? $"{Queue}.retry" : RetryQueue;
        public string ResolveDlqExchange() => string.IsNullOrWhiteSpace(DlqExchange) ? $"{Queue}.dlq.exchange" : DlqExchange;
        public string ResolveDlqQueue() => string.IsNullOrWhiteSpace(DlqQueue) ? $"{Queue}.dlq" : DlqQueue;
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

            return Enumerable.Range(0, maxRetryCount).Select(index => configured[Math.Min(index, configured.Length - 1)]).ToArray();
        }

        public string ResolveRedeliveryExchange() => $"{Queue}.redelivery.exchange";
        public string ResolveRedeliveryRoutingKey() => $"redeliver.{Queue}";
        public string ResolveRetryBucketRoutingKey(int index) => $"retry.{Queue}.{index}";
        public string ResolveRetryBucketQueue(int index, int delayMs) => $"{ResolveRetryQueue()}.v2.{index}.{delayMs}";
    }
}
