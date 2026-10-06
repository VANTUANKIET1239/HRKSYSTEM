namespace Core.RabbitMQ.Entities;

public enum MessageFailureCategory
{
    Transient,
    Permanent,
    Duplicate,
    Cancelled,
    Unknown
}
