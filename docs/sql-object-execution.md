# Calling database objects through UnitOfWork

SPs, views and table-valued functions must already exist in the SQL Server database.
Application code supplies an object name, arguments when needed, and a result DTO.
The executor builds the command; it does not create or migrate database objects.
Keep object definitions and result DTOs in the service that owns the feature.

## Registration and calls

```csharp
services.AddDatabase<GameDbContext>(configuration);
services.AddRepositoryUOW<GameDbContext>();
```

Inject `IUnitOfWork`. No separate connection or executor registration is necessary.

```csharp
var catalog = await unitOfWork.Sql.QueryViewAsync<HeroSkillDto>(
    "dbo.vw_HRK_HeroSkillCatalog", cancellationToken: ct);

// Illustrative names: deploy the corresponding SP/TVF in the owning service first.
var rows = await unitOfWork.Sql.QueryProcedureAsync<HeroSkillDto>(
    "dbo.usp_GetHeroSkills", new { HeroTemplateId = heroId }, cancellationToken: ct);

var skills = await unitOfWork.Sql.QueryFunctionAsync<HeroSkillDto>(
    "dbo.fn_GetHeroSkills", new { HeroTemplateId = heroId }, cancellationToken: ct);

await unitOfWork.Sql.ExecuteProcedureAsync(
    "dbo.usp_UpdateHero", new { HeroId = heroId, Level = level }, cancellationToken: ct);
```

DTO property names should match returned column names/aliases, with compatible CLR types
and nullability. Results are buffered and untracked. `QueryViewAsync` reads all columns/rows;
it does not guarantee ordering, filtering or pagination. Use a parameterized SP/TVF for
those query contracts. A procedure called with `QueryProcedureAsync` may also write data.
`ExecuteProcedureAsync` returns the affected-row count reported by the provider, which may
be `-1` with `SET NOCOUNT ON`; it is not a success flag or the SP's integer `RETURN` value.
`QueryStoredProcAsync` remains available as a compatibility alias.

Named-object APIs support SQL Server only. Raw `QueryAsync` and `ExecuteAsync` remain
available for trusted parameterized SQL on the configured relational provider.
The `ISqlExecutor` contract lives in `Core.Persistence.Abstractions` under the existing
`Core.Persistence.SqlExecutor` namespace, with no Dapper or EF dependency.

## Names and TVF arguments

Names are developer-controlled constants, not arbitrary client input. Accepted formats
are `ObjectName` (defaults to `dbo`) or `schema.ObjectName`, using ASCII letters, digits
and underscores, starting with a letter or underscore, up to 128 characters per part.
Bracketed names, SQL fragments and cross-database names are rejected. Each identifier
part is quoted separately; values are always parameters.

TVF parameter names/order are discovered from `sys.objects`, `sys.schemas` and
`sys.parameters`, ordered by `parameter_id`. The DB user needs permission to query the
function and visibility of its metadata. A missing/invisible function produces a clear error.
Argument properties are matched case-insensitively by name, not reflection/property order.
Anonymous objects and string/object dictionaries are accepted; dictionary names may start
with `@`. Duplicate, missing or unexpected arguments are rejected. Supply every TVF argument,
including explicit null values; SQL default arguments are not inferred. Dapper-specific
dynamic parameter bags are not accepted by the TVF API.

Metadata is cached only for the lifetime of that UnitOfWork/executor. After changing a TVF
signature within the same scope, call `unitOfWork.Sql.ClearFunctionMetadataCache()`.
New scopes discover metadata again. No global cache mixes databases or tenants.

## Transactions, retries and EF state

```csharp
await unitOfWork.ExecuteInTransactionAsync(async () =>
{
    // Load/change entities inside this delegate so retries can rebuild their state.
    // Save here if the SP must read pending EF changes.
    await unitOfWork.SaveChangesAsync(ct);
    await unitOfWork.Sql.ExecuteProcedureAsync(
        "dbo.usp_GrantReward", new { PlayerId = playerId }, cancellationToken: ct);
    // Throw a business exception here if a returned result indicates failure.
}, ct);
```

The UOW owns begin/commit/rollback and the final EF save. The executor never starts its own
transaction, saves EF state, retries individual commands, or disposes the borrowed connection.
Each command reads the current transaction, including after an execution-strategy retry.
Without an active UOW transaction, SQL writes follow the database's normal autocommit behavior.
Do not use `Task.WhenAll` on a shared context/connection; use independent scopes for independent
parallel work. Timeout comes from EF's configured command timeout (`Database:CommandTimeout`
through `AddDatabase`), with a 30-second fallback. Cancellation is passed to Dapper commands,
including TVF metadata queries. A provider error reported after the supplied token is
cancelled is surfaced as `OperationCanceledException`, retaining the original error as its cause.

SP writes execute immediately; `SaveChangesAsync` is needed for pending EF writes before a
SP can observe them. Tracked entities are not automatically refreshed after SQL changes;
reload affected entities when needed. Do not clear all tracking and discard pending changes.
Database exceptions propagate to the existing application/API error handling. A normal
failure result does not automatically roll back the UOW wrapper: inspect it inside the
delegate and throw the appropriate business exception. The executor does not produce HTTP
responses or interpret database-specific business codes.

Output/return parameter abstractions, TVPs and multiple result sets are outside this API's
initial scope. Infrastructure callers of existing raw/SP methods may still use Dapper bags,
but application contracts do not require Dapper types.

## Verification

Run the normal Core module suite for DI, identifier and cancellation checks:

```powershell
dotnet test Tests/Core.Modules.Tests/Core.Modules.Tests.csproj
```

Opt into SQL Server LocalDB integration tests:

```powershell
$env:HRK_SQL_EXECUTOR_TESTS = '1'
dotnet test Tests/Core.Modules.Tests/Core.Modules.Tests.csproj
```

The fixture creates a uniquely named `HRK_SqlExecutorTests_*` scratch database and removes
only that database after testing. Tests cover mapping, TVF parameter order/cache refresh,
EF/SP atomic rollback and retry/commit, connection ownership, timeout and cancellation.
No application database or production SQL definitions are changed.
