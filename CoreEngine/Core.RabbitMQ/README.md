# Core.RabbitMQ

Library supports named publishers and named consumers. The legacy single
`Publisher`/`Consumer` configuration and the old publish overload remain available.

## Recommended configuration

```json
{
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "Publishers": {
      "DomainEvents": {
        "Exchange": "hrk.events",
        "ExchangeType": "topic",
        "DefaultRoutingKey": "auth.user.created",
        "Durable": true
      },
      "Commands": {
        "Exchange": "hrk.commands",
        "ExchangeType": "direct",
        "DefaultRoutingKey": "game.player.create",
        "Durable": true
      }
    },
    "Consumers": {
      "GameUserEvents": {
        "Exchange": "hrk.events",
        "ExchangeType": "topic",
        "Queue": "game.auth-user-events",
        "RoutingKeys": [
          "auth.user.created",
          "auth.user.updated"
        ],
        "PrefetchCount": 20,
        "RetryDelayMs": 15000
      }
    }
  }
}
```

When omitted, retry and DLQ names are derived from the queue:

- `{queue}.retry.exchange`
- `{queue}.retry`
- `{queue}.dlq.exchange`
- `{queue}.dlq`

## Registration

```csharp
services
    .AddRabbitMqMessaging(configuration)
    .EnsureRabbitTopology()
    .AddNamedRabbitConsumer<UserCreatedIntegrationEvent, UserCreatedHandler>(
        "GameUserEvents");
```

The string passed to `AddNamedRabbitConsumer` is the key under
`RabbitMq:Consumers`. The old `AddRabbitConsumer(..., queue)` API remains available
for legacy single-consumer configuration.

## Publishing

```csharp
await publisher.PublishJsonAsync(
    publisherName: "DomainEvents",
    message: new UserCreatedIntegrationEvent(userId, userName),
    routingKey: "auth.user.created",
    ct: cancellationToken);
```

If exactly one named publisher exists, the legacy overload can still be used:

```csharp
await publisher.PublishJsonAsync(message, "auth.user.created", cancellationToken);
```

When multiple publishers exist, callers must specify `publisherName`.

## Example flow: AUTH -> GAME and Notification

AUTH publishes one `auth.user.created` event to the topic exchange `hrk.events`.
GAME and Notification configure different queues, both bound to the same routing key:

```text
AUTH
  -> hrk.events / auth.user.created
       -> game.auth-user-created.queue
       -> notification.auth-user-created.queue
```

RabbitMQ puts an independent copy in each queue. A failure in the GAME handler does
not delay Notification. Each service owns its retry queue and DLQ.

GAME consumer:

```json
"GameUserCreated": {
  "Exchange": "hrk.events",
  "ExchangeType": "topic",
  "Queue": "game.auth-user-created.queue",
  "RoutingKeys": [ "auth.user.created" ],
  "RetryDelayMs": 15000
}
```

Notification consumer:

```json
"NotificationUserCreated": {
  "Exchange": "hrk.events",
  "ExchangeType": "topic",
  "Queue": "notification.auth-user-created.queue",
  "RoutingKeys": [ "auth.user.created" ],
  "RetryDelayMs": 30000
}
```

Use separate queues for separate services. If two services consume the same queue,
RabbitMQ load-balances messages between them instead of broadcasting a copy to each.

# Reliability behavior

The library uses publisher confirms, persistent messages, manual consumer acknowledgements, named publishers/consumers, configurable delayed retries and per-consumer dead-letter queues.

- Supply `RabbitPublishMetadata.MessageId` for Outbox publishing. Retrying the same Outbox row must reuse the same ID.
- Delivery is at-least-once. Handlers that mutate state must be idempotent.
- Handler failure and ACK failure are handled separately. An ACK failure does not republish an already successful handler as a new failure.
- Retry bucket queues dead-letter to a private redelivery exchange bound directly to the original consumer queue. This prevents retries from being broadcast again to other subscribers of the main topic exchange.
- `OperationCanceledException` caused by host shutdown is left unacknowledged for broker redelivery.

Consumer retry behavior is configured under `RabbitMq:Consumers:{name}:Retry` with `MaxRetryCount`, `DelaysMs`, `UnknownExceptionBehavior` and `ErrorMaxLength`.
