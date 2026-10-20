# Game event scheduling and activity history

## Ownership

`CoreEngine/Core.Scheduling` wraps Quartz.NET 3.15.1 in a reusable .NET 9 library. It contains database-managed job definitions, execution history, SQL Server persistent scheduling, clustering, reconciliation, retries, cooperative timeouts and scoped handlers. It has no GAME or ACTIVITY project dependency.

GAME owns `HRK_JobDefinitions`, `HRK_JobExecutions`, business settlement and the durable per-player checkpoint. ACTIVITY owns the queryable `PlayerActivities` timeline. Operational exceptions continue through ILogger/observability.

The first business handler is `EVENT_PERIOD_RESET`, for existing `DAILY` / `TOWER` events. Other event types require their own settlement handler and tests; arbitrary cron expressions do not automatically make daily gameplay logic support weekly or seasonal periods. Stamina purchase reset is unchanged.

## Rollout

Do not apply migrations automatically at startup. Stop the old GAME worker/API instances while applying schema changes. No application database has been migrated by this code change.

1. On the GAME database, apply existing tower and messaging migrations, including `Create_GameOperationProcedures.sql`, if they are not already installed.
2. Run `Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_EventScheduling.sql` on GAME. It creates job definitions/executions and adds checkpoint/snapshot columns. It does not insert job definitions. For the specific tower event, explicitly run `Register_TowerDailyResetJob.sql` from the same directory after checking `@EventCode`. The registration script preserves existing settings on reruns.
3. Run `CoreEngine/Core.Scheduling/Scripts/CreateQuartzSqlServer.sql` on the same GAME database. It creates Quartz's `QRTZ_*` tables without deleting existing data. A complete existing schema is preserved; a partial schema requires inspection.
4. Run `Services/HRKACTIVITY/Activity.Infrastructure/Scripts/Migration_EventActivityPeriods.sql` on ACTIVITY. Start the updated ACTIVITY consumer before GAME produces new activity types. Ensure the ActivityTimeline queue exists before publishing.
5. Deploy the updated binaries. Set `Scheduling__Enabled=true` on GAME after migrations. `appsettings.json` defaults to disabled so deployment does not start a scheduler against missing Quartz tables. The new columns are required by the updated binaries even while background scheduling is disabled.

Persistent scheduling uses `Database:ConnectionString` unless `Scheduling:ConnectionString` is explicitly supplied. The job definition/execution tables always belong to GAME's DbContext; a separate Quartz connection affects only the `QRTZ_*` tables. All active GAME scheduler nodes must share the same Quartz database and scheduler name (`HRK.GAME`). Nodes use instance ID `AUTO` and clustering. Keep their clocks synchronized.

```json
{
  "Scheduling": {
    "Enabled": true,
    "UsePersistentStore": true,
    "MaxConcurrency": 2,
    "SynchronizationIntervalSeconds": 30
  }
}
```

`UsePersistentStore=false` is for isolated development/tests. An API-only host can leave `Enabled=false`; another GAME host must enable scheduling for background execution. Request-time settlement remains available on API hosts.

## Definitions and management API

GAME service registration is composed in `HRK.GAME/Configuration/DependencyInjection.cs`. `AddServices` delegates to separate methods for persistence, API/Swagger, compression, security, caching, messaging, player/formation/progression/inventory/dungeon/battle/summon/event services and scheduling. `Program.cs` only builds the application and configures the middleware/endpoints. JWT and compression configuration are registered through the same custom dependency entry point as the game handlers.

Jobs are declared explicitly: register the handler in `GameSchedulingRegistration.AddJobHandlers`, then insert its configuration into `HRK_JobDefinitions` using UnitOfWork or a reviewed SQL script. There is no seeder and no scan of unregistered game events. The synchronizer only reads existing definitions. Previously inserted definitions are retained; this change does not remove or overwrite them.

`HRK_JobDefinitions` is the authoritative schedule for both Quartz and `TowerEventService` when a definition exists. Events without a definition keep their existing request-time reset behavior using the event's reset fields; no background job is created for them. Disabling a job stops background work; logical event periods still advance and request-time settlement still works.

Admin-role endpoints on GAME:

- `GET /api/admin/jobs`: definitions and configuration versions.
- `GET /api/admin/jobs/{id}/executions?page=1&pageSize=50`: execution snapshots, status, attempts and results/errors.
- `PUT /api/admin/jobs/{id}`: update an existing definition with the current `configVersion` and all editable fields.
- `POST /api/admin/jobs/{id}/trigger`: request execution of an enabled definition. Returns 202; inspect executions for completion.

Example update body (use the actual version returned by GET):

```json
{
  "configVersion": 1,
  "cronExpression": "0 0 12 * * ?",
  "timeZoneId": "Asia/Ho_Chi_Minh",
  "parametersJson": "{\"batchSize\":100}",
  "isEnabled": true,
  "misfirePolicy": "FireOnceNow",
  "maxRetryAttempts": 2,
  "retryDelaySeconds": 5,
  "timeoutSeconds": 300,
  "recoveryIntervalSeconds": 300
}
```

The updater validates cron/time zone, bounded JSON parameters, retry/time limits and optimistic versioning. Changes are reconciled within the synchronization interval and survive scheduler downtime. Admin changes emit an activity summary through the GAME Outbox, including parameter hashes rather than copying arbitrarily large configuration payloads.

For the daily reset handler, only `second minute hour * * ?` is accepted. Reset schedule/time-zone edits are rejected once an event has persisted periods: changing existing period interpretation requires a separately designed effective-date migration. Batch size, retry, timeout, recovery interval and enabled status remain editable. A job's `ConfigVersion` is immutable within an execution: retries reuse the captured configuration and get fresh DI/DbContext scopes.

## Reset behavior and recovery

`EventPeriodCalculator` is a static helper for time-zone resolution, daily period boundaries and reset countdowns. It has no persistence or tower-specific state. `TowerEventService` implements `ITowerEventService` and owns tower period creation, player progress and expired-period settlement. Both request handlers and `EventResetJobHandler` call this same service; there is no separate settlement service/interface. The job handler coordinates batching and execution metadata, while the service performs per-player business operations inside the caller's transaction and lock.

Each definition has one stable Quartz JobKey, shared by cron, manual and recovery triggers. The library prevents concurrent execution of that JobKey. A startup reconciliation also requests execution; the recovery interval revisits overdue work after outages or exhausted retries. Cron misfires support `FireOnceNow` and `DoNothing`; catch-up is implemented by business-period scanning, independently of how many cron firings were missed.

The reset handler creates the current event period, then scans every expired, unclosed player period, including days missed during downtime. Work is batched by player and uses the same `tower:player:{UserId}` SQL application lock and transaction as battles/claims/quick climb. A failed player does not prevent the rest of that pass being processed; failed players remain eligible for retry.

Per-player closure stores `ClosedAtUtc`, `ResetExecutionId` and `ResetSummaryJson` on the existing progress row. It expires old runs/quick-climb jobs and converts eligible unclaimed milestone chests to pending rewards. State changes, checkpoint and activity/status Outbox messages commit together. A rollback rolls them all back. Retrying skips already closed periods; database unique constraints continue to protect milestone rewards.

Accepted in-flight battles may finish in their original period while holding the player lock. Settlement waits for that lock, then sees the committed result. A new request resolves the current period. Quick-climb processing keeps its own period-expiry check.

New player progress is created lazily when needed, preserving all-time records and historical progress. Creating a period freezes milestone chest configuration. Chest display, claiming and automatic conversion use the same snapshot. Legacy periods without snapshots use existing active configuration; pre-upgrade reward settings cannot be reconstructed.

Execution statuses: `RUNNING`, `SUCCEEDED`, `FAILED`, `TIMED_OUT`, `CANCELLED`, `INTERRUPTED`. A recovered firing marks a leftover RUNNING record interrupted and resumes from business checkpoints. Timeouts are cooperative cancellation; handlers must pass the token to I/O. Never use an execution row alone to decide whether a particular player's reward was applied.

## Activity timeline

The existing `game.activity.recorded.v1` contract gains optional `GameEventId`, `EventPeriodId`, `PeriodKey`, `JobExecutionId`. `EventId` in that contract remains the unique activity/message ID. Old producers/messages remain compatible. New types must be deployed to the consumer allowlist before publishing.

Events cover tower battle completion (manual and quick climb), milestone chest claiming, pending-reward claiming, starting another run, quick-climb start/end, player-period closure and reset completion. Existing authoritative gameplay history remains in GAME.

`OccurredAt` is when the business action actually happened; `RecordedAt` is when ACTIVITY persisted it. A delayed reset's metadata includes `EffectiveEndAtUtc`, while `PeriodKey` continues to identify the closed game day. Times are stored in UTC. UI/reporting should convert them to the chosen display time zone. IDs in ACTIVITY are references, with no cross-database foreign keys.

Filter the existing admin Activity API with `playerId`, `gameEventId`, `eventPeriodId`, `periodKey`, `jobExecutionId`, `activityType`, `fromUtc` (inclusive), `toUtc` (exclusive), and pagination. For example:

```text
GET /api/activities?playerId=42&gameEventId=1&periodKey=2026-10-19&page=1&pageSize=50
```

Timeline delivery is asynchronous through Outbox → RabbitMQ → Inbox. An ACTIVITY outage delays timeline visibility without moving reset ownership out of GAME. Stable activity IDs and Inbox deduplication protect redelivery. Retention for activity, Inbox, execution history and authoritative reset checkpoints must be managed separately; no automatic deletion is introduced.

## Reuse in another service

Reference `Core.Scheduling`, call `modelBuilder.ConfigureScheduling()` in the owning DbContext, register its scoped `IUnitOfWork` and call `AddManagedScheduling(...)`. Register `IScheduledJobHandler` implementations by stable `HandlerKey` and explicitly insert the required `JobDefinition` rows. Handlers receive the definition snapshot, execution ID, scheduled time and cancellation token; they must implement their own durable business idempotency. `IJobScheduleManager` exposes synchronization/manual execution without exposing Quartz APIs to application code.

`UnitOfWorkSchedulingStore` uses read-only repositories for queries and tracked repositories inside UnitOfWork transactions for execution-history writes. It does not inject DbContext. Execution-history scopes remain separate from business-handler scopes, so saving job history cannot commit pending gameplay changes. Quartz still owns its internal ADO.NET access to `QRTZ_*`; application code does not query or update those tables.

Use a different scheduler name for an independent service. Apply the shared Quartz schema on that scheduler's database and create its definition/execution tables from its EF model/migration. The library is packable with `dotnet pack CoreEngine/Core.Scheduling/Core.Scheduling.csproj`.

## Verification

Standard test projects: `Tests/Core.Modules.Tests`, `Tests/dcs-game/GAME.Domain.Tests`, `Tests/Oservability.Tests`.

Additional isolated LocalDB tests:

```powershell
$env:HRK_SUMMON_SQL_TESTS = '1'
dotnet test Tests/dcs-game/GAME.Domain.Tests/GAME.Domain.Tests.csproj --filter FullyQualifiedName~EventResetSqlTests
$env:HRK_ACTIVITY_SQL_TESTS = '1'
dotnet test Tests/Oservability.Tests/Oservability.Tests.csproj --filter FullyQualifiedName~EventActivitySqlTests
```

These fixtures create and remove randomly named scratch databases. They never target the configured application database. The tests cover SQL rollback with Outbox, competing player closures, migration reruns, persistent Quartz execution and activity migration/deduplication/filtering. Existing Docker transport tests remain opt-in via `HRK_RUN_INTEGRATION=1`.
