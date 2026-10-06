# Core.TransactionalMessaging

Shared SQL Server implementation of the transactional Outbox and Inbox patterns.

## Register a service

```csharp
services.AddRabbitMqMessaging(configuration);
services.AddTransactionalMessaging<MyDbContext>(configuration);
```

Map the shared entities in the service database model. The SQL stores discover the
physical table names from this EF mapping, so there is no second table-name setting
that can drift away from the model.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ConfigureTransactionalMessaging(
        outboxTable: "MyService_OutboxMessages",
        inboxTable: "MyService_InboxMessages");
}
```

Create the mapped tables through that service's EF migration or SQL migration. Every
service owns its own Outbox and Inbox data; services must not share these tables.

## Configuration

```json
{
  "TransactionalMessaging": {
    "Outbox": {
      "Enabled": true,
      "BatchSize": 20,
      "PollingIntervalMs": 1000,
      "LeaseSeconds": 60,
      "PublishTimeoutSeconds": 15,
      "MaxRetryCount": 10,
      "RetryDelaysMs": [1000, 5000, 15000, 30000, 60000],
      "ErrorMaxLength": 2000
    },
    "Inbox": {
      "CleanupEnabled": true,
      "RetentionDays": 30,
      "CleanupBatchSize": 1000,
      "CleanupIntervalMinutes": 60
    }
  }
}
```

## Produce through the Outbox

Inject `IOutboxWriter`, add the domain/integration event while the business
transaction is active, then commit once. `Add` only tracks the row; it does not call
RabbitMQ or save changes by itself.

```csharp
outbox.Add(
    integrationEvent,
    eventName: "order.created.v1",
    publisherName: "orders",
    routingKey: "order.created.v1",
    partitionKey: order.Id.ToString(),
    sequence: order.Version);
```

The hosted Outbox worker claims rows with a lease and publishes them with retry.
Delivery is at-least-once: a broker publish can succeed while the database status
update fails, so consumers must use the Inbox.

## Consume through the Inbox

Use `ExecuteAsync` for an ordinary consumer. The message reservation and business
changes are committed in one local database transaction.

```csharp
var execution = await inbox.ExecuteAsync(
    consumerName: "OrderCreated",
    messageId,
    async ct =>
    {
        await ApplyBusinessChangesAsync(ct);
        return true;
    },
    cancellationToken);
```

Use `ExecuteInCurrentTransactionAsync` only when a service-specific transaction or
lock runner already owns the transaction. A duplicate `(ConsumerName, MessageId)`
skips the action and returns `IsDuplicate = true`. If the action or commit fails, the
Inbox reservation rolls back too, allowing RabbitMQ to redeliver safely.

The Inbox cleanup worker removes old deduplication rows in bounded batches. Set the
retention period longer than RabbitMQ's maximum possible retry/redelivery window.
