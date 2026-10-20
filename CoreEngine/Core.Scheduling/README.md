# Core.Scheduling

Reusable .NET 9 wrapper around Quartz.NET with database-managed schedules, SQL Server persistence/clustering, execution snapshots, retry, cooperative timeout and scoped handlers.

1. Call `modelBuilder.ConfigureScheduling()` in the service's EF DbContext and migrate its `JobDefinition` / `JobExecution` tables.
2. Apply `Scripts/CreateQuartzSqlServer.sql` to the Quartz database.
3. Register the owning service's scoped `IUnitOfWork`, then call `AddManagedScheduling(options => { options.Enabled = true; options.SchedulerName = "SERVICE"; options.ConnectionString = connection; })`.
4. Register scoped `IScheduledJobHandler` implementations and explicitly insert the jobs that should run into `JobDefinitions` through UnitOfWork or an SQL script. No event scanning or automatic seeding occurs.

Use `IJobScheduleManager` to synchronize or trigger jobs. A handler receives a configuration snapshot and execution ID. Persist business checkpoints in the owning service's transaction; every retry gets a fresh DI scope.

Persistent schedulers use clustering and instance ID AUTO. Independent services need different scheduler names. In-memory storage is available for tests. Timeouts request cancellation; handlers must honor the supplied cancellation token.

See `docs/game-event-scheduling.md` in the HRK repository for the GAME integration, management endpoints, migration order and recovery behavior.
