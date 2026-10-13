# GAME operation stored procedures

Apply `Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Create_GameOperationProcedures.sql`
to the intended GAME database before starting the updated API/workers. Run it with SSMS or
sqlcmd, which understand `GO` batch separators. SQL Server 2016 SP1 or later and the existing
`dbo.HRK_PlayerWallets` table are required. The script uses `CREATE OR ALTER` and can be rerun.
If the application's DB role uses explicit object grants, grant it `EXECUTE` on both procedures.
Application database deployment is manual; tests only apply the script to isolated LocalDB databases.

| Procedure | Caller | Behavior |
| --- | --- | --- |
| `dbo.usp_HRK_AcquireTowerPlayerLock` | `TowerOperationRunner.LockAsync` | Acquires an exclusive transaction-owned `sp_getapplock` for the supplied resource; default wait remains 15,000 ms |
| `dbo.usp_HRK_LockPlayerWallet` | `SummonCommandHandler` | Locks the player's wallet row/key range with `UPDLOCK, HOLDLOCK` before loading the tracked wallet and checking receipts |

Application code only passes the procedure name and argument object through
`unitOfWork.Sql.ExecuteProcedureAsync`. Names are centralized in
`GAME.Application/Common/GameStoredProcedures.cs`. The previous inline SQL batches have
been removed from both GAME callers.

Both procedures require an existing caller transaction (`51001` if absent). They never
begin, commit or roll back a transaction. UnitOfWork remains the sole transaction owner;
locks live until that transaction commits or rolls back. Tower lock acquisition failures
retain SQL error `51000` and the existing busy message. `@LockTimeoutMilliseconds` allows a
bounded wait when explicitly supplied; production callers use the unchanged 15-second default.
Wallet absence is still handled by the existing player/wallet lookup in Application.
Neither procedure returns entities or modifies wallet balances/RowVersion.

Do not use the affected-row count as a success flag: these procedures use `SET NOCOUNT ON`.
Cancellation, retry boundaries, wallet concurrency checks and summon receipt semantics remain
managed by the existing executor/UnitOfWork/handlers.

## Verification

```powershell
$env:HRK_SUMMON_SQL_TESTS = '1'
dotnet test Tests/dcs-game/GAME.Domain.Tests/GAME.Domain.Tests.csproj --no-restore
```

The fixture deploys the real script twice to verify reruns, then exercises the updated summon
handlers, concurrent requests and rollback. Procedure integration tests verify caller-owned
transaction requirements, same-resource Tower exclusion, different-resource independence,
wallet exclusion, unchanged wallet values and lock release after commit/rollback.
Raw SQL in test setup (schema, triggers, session settings and cleanup) remains test infrastructure.
