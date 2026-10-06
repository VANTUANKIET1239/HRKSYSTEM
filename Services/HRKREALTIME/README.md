# HRK Realtime Service

`HRK.REALTIME` consumes versioned process-status events from RabbitMQ and forwards them to authenticated SignalR clients.

## Runtime configuration

- `JwtSettings__SecretKey`: must match the AUTH/GAME signing key.
- `RabbitMq__HostName`, `RabbitMq__UserName`, `RabbitMq__Password`: RabbitMQ connection.
- `Redis__Enabled=true` and `Redis__ConnectionString`: required when more than one realtime instance is deployed.
- `Cors__Origins__0`: SPA origin when the client connects directly.

The hub is available at `/hubs/realtime` and requires a `game-api` JWT. SignalR is a notification channel; clients must reload the job snapshot from GAME after initial load or reconnect.

## Deployment order

1. Apply `Migration_TransactionalOutboxAndQuickClimbMessaging.sql` to the GAME database.
2. Start RabbitMQ and Redis.
3. Deploy GAME so it can create the topology and publish its Outbox.
4. Deploy HRK.REALTIME.
5. Point Angular `realtimeUrl` at the public hub endpoint.

Do not delete existing RabbitMQ queues to change arguments. The retry topology uses versioned retry bucket queue names.
