# Core modules

`Core.Common` contains only shared values, responses, base entities, constants and text helpers.
It has no NuGet packages, framework references or references to other Core projects.
Do not add database, HTTP, authentication, caching or dependency-injection code to it.

| Project | Responsibility | Registration |
| --- | --- | --- |
| Core.Persistence.Abstractions | Repository and UnitOfWork contracts without EF dependencies | None |
| Core.Persistence | EF repositories, transactions, database providers/interceptor, Dapper executor | `AddDatabase<TContext>(configuration)`, `AddRepositoryUOW<TContext>()` |
| Core.Application | MediatR command/query contracts and FluentValidation helpers | `AddCoreApplication(applicationAssembly)` |
| Core.Web | HTTP response/error handling, HTTP-aware command/query bases, cookies | `AddControllers(options => options.AddHrkApiFilters())`, `AddHrkApiResponses()`, `AddCoreCookies(configuration)` when cookies are needed |
| Core.AUTH | JWT generation/validation, token hashing and JWT settings | `AddCoreJwt(configuration)` |
| Core.Caching | Redis `IDistributedCache` backend and typed HybridCache L1/L2 adapter | `AddHrkRedisCache(configuration)`, `AddHrkHybridCache(configuration)` |
| Core.Logging | Optional `ILoggerService<T>` wrapper | `AddCoreLogging()` after host logging setup |

## Host composition

GAME registers database/UnitOfWork, Redis, HybridCache and application handlers. Redis supplies
HybridCache's distributed tier and template revision markers. `AddHrkRedisCache` registers
only the backend; cache consumers use `IHybridCacheService` or the GAME template catalog.
The old JSON cache adapter and legacy string-cache wrapper have been removed. Entry TTLs
come from `HybridCache` / `GameTemplateCache`; the old adapter's default TTL setting has
been removed. GAME's existing JWT bearer middleware still validates access tokens; it
does not register token generation or cookies.
AUTH registers database/UnitOfWork, JWT, cookies and application handlers/validators.
RabbitMQ no longer references Core.Common. GAME.Domain references only the dependency-free
Core.Common error contracts; it has no references to HTTP, EF or DI projects.

`AddCoreService(configuration)` and the unqualified `AddOptions(configuration)` have been removed.
Each host composes only the modules it uses.

Repository adapters are optional: call `AddRepositoryAdapters()` only when a consumer injects
`IRepository<T>` or `IReadOnlyRepository<T>` directly. Consumers injecting `IUnitOfWork` need
only `AddRepositoryUOW<TContext>()`. Scoped repositories use that UnitOfWork's context.

The Dapper executor is available as `unitOfWork.Sql` after `AddRepositoryUOW<TContext>()`.
It borrows the same context connection and reads its current transaction for every command.
No separate `IDbConnection` registration is needed. Optional `AddCoreSqlExecutor()` registers
`ISqlExecutor` as an alias of that UnitOfWork's executor for direct injection; it requires
`AddRepositoryUOW<TContext>()`. Prefer UnitOfWork access in application handlers.
See [SQL object execution](sql-object-execution.md) for named SP, view and TVF usage.

## Namespace migration

| Previous namespace | New namespace |
| --- | --- |
| Core.Common.Repositories | Core.Persistence.Repositories |
| Core.Common.Database.* | Core.Persistence.Database.* |
| Core.Common.SqlExecutor | Core.Persistence.SqlExecutor |
| CoreEngine.CQRS | Core.Application.CQRS |
| Core.Common.FluentValidation | Core.Application.Validation |
| Core.Common.CQRS | Core.Web.CQRS |
| Core.Common.Cookie | Core.Web.Cookies |
| Core.Common.Entity.HRKCookieOptions | Core.Web.Cookies.HRKCookieOptions |
| Core.Common.JwtHandler.* | Core.AUTH.Jwt.* |
| Core.Common.Helpers.TokenHelper | Core.AUTH.Tokens.TokenHelper |
| Core.Common.Caching / CoreEngine.RedisCache | Core.Caching |
| Core.Common.Logging | Core.Logging |

The transaction behavior fixed earlier is retained: commit does not save; the transaction
wrapper rejects nested ownership and rebuilds tracked state on retry; uncertain commit
outcomes are not blindly replayed. EF6's unreachable validation catch and package were
removed; EF Core persistence exceptions propagate unchanged.

## Validation

`Tests/Core.Modules.Tests` checks module isolation and DI resolution without SQL/Redis servers.
The GAME suite includes transaction regression tests. Dockerfiles copy the complete referenced
project graph before restore. Changes to that graph must also update the relevant Dockerfile.
