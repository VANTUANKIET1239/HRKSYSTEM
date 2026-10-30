namespace Core.RabbitMQ.Entities;
public sealed partial class RabbitMqOptions
{
    public sealed record RetryOptions
    {
        public int MaxRetryCount { get; set; } = 1;
        public List<int> DelaysMs { get; set; } = new();
        public string UnknownExceptionBehavior { get; set; } = "DeadLetter";
        public int ErrorMaxLength { get; set; } = 2000;
    }
}
