namespace Core.TransactionalMessaging.Entities;

public enum OutboxStatus : byte
{
    Pending = 1,
    Processing = 2,
    Published = 3,
    Failed = 4
}
