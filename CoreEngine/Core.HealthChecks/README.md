# Core.HealthChecks

Reusable ASP.NET Core liveness and dependency endpoints. Dependency implementations live in each service's `HealthChecks/Dependencies` directory and implement `IHealthCheck` (optionally deriving from `DependencyHealthCheck`). This package does not own database, Redis or RabbitMQ connections.

Each type has its own file, grouped by responsibility:

```text
Core.HealthChecks/
  Checks/                DependencyHealthCheck, built-in LivenessHealthCheck
  Configuration/         HrkHealthCheckOptions, DependencyCheckOptions
  Contracts/             ServiceHealthReport, ComponentHealth, HealthKinds, HealthStatuses
  DependencyInjection/   HrkHealthChecksRegistration, HrkHealthChecksBuilder
  Internal/              HealthServiceIdentity
  Responses/             HealthCheckResponseWriter
```

Public types retain the `Core.HealthChecks` namespace, including the registration
and mapping extensions. Existing service imports and calls remain compatible.

```csharp
services.AddHrkHealthChecks("HRK.GAME", configuration)
    .AddDependency<GameDatabaseHealthCheck>("database")
    .AddDependency<GameRabbitMqHealthCheck>("rabbitmq");
app.MapHrkHealthChecks();
```

For multiple configured instances of one check type, use
`checks.AddDependency(name, provider => new MyCheck(provider.GetRequiredService<IMyDependency>(), target))`.
The factory receives the framework's per-check scoped provider; settings under
`HealthChecks:Dependencies:<name>` apply to generic and factory registrations alike.

`GET /health/live` only runs the built-in `self` check. `GET /health/dependencies` runs registered dependency checks. `/health` and `/health/ready` remain dependency aliases unless `legacyAliases: false` is specified. Healthy returns 200; other statuses, including .NET `Degraded`, are represented as `Unhealthy` with HTTP 503. JSON uses the versioned `ServiceHealthReport` contract, UTC observation time, instance identity, component statuses, durations and safe error codes. Exceptions and arbitrary check data are never serialized. No background polling or response cache is added.

```json
{
  "HealthChecks": {
    "DefaultTimeoutSeconds": 2,
    "Dependencies": {
      "database": { "TimeoutSeconds": 3 },
      "redis.localization": { "Enabled": false }
    }
  }
}
```

Checks are activated through DI in the framework's per-check scopes. Implementations must honor cancellation; vendor APIs without cancellation should be awaited with `WaitAsync(cancellationToken)`. Liveness never accesses dependencies. A dependency report is not a claim that migrations, business workflows, queue consumers or outbox progress have been verified. Register additional service-owned checks for those requirements.
