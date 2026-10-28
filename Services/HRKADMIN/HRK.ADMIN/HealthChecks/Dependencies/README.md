# Admin dependency checks

Place future Admin-owned custom IHealthCheck classes here and register them using
AddHrkHealthChecks(...).AddDependency<TCheck>(...). Admin currently has no database,
Redis or RabbitMQ dependency.

The monitored services are checked on demand by /api/admin/health/services.
They are deliberately not Admin's own dependency checks: Admin must remain reachable
when a monitored service is down, and Gateway's Admin liveness check must not recurse.
