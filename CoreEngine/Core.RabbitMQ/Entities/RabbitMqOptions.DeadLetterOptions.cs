namespace Core.RabbitMQ.Entities;
public sealed partial class RabbitMqOptions
{
    public sealed record DeadLetterOptions
    {
        public bool Enabled { get; set; } = true;
    }
}
