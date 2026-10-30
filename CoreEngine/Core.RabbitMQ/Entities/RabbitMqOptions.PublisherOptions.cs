namespace Core.RabbitMQ.Entities;
public sealed partial class RabbitMqOptions
{
    public sealed record PublisherOptions
    {
        public string Exchange { get; set; } = "app.exchange";
        public string ExchangeType { get; set; } = "direct";
        public string DefaultRoutingKey { get; set; } = "app.default";
        public bool Durable { get; set; } = true;
    }
}
