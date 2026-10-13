# Hero recruitment

The lobby's `SUMMON` action opens `/dcs-game/summon`. The page has ordinary and premium chests, configured prices, a hero/rate dialog and ×1/×10 buttons. Single draws use a 5.2-second horizontal reel with a center marker and a skip button. Ten draws show a result grid directly. Reduced motion skips the reel. Narrow layouts stack the chests and use two columns for rewards.

## Apply the database migration before starting the updated game API

Run `Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_HeroSummoning.sql` against the intended **game database**, after the hero star/stone and level experience migrations. The script is transactional and rerunnable. It creates the summon pool, pool entries, batch history and result item tables; adds wallet `RowVersion`; backfills missing hero stone definitions; and enables the lobby action. It preserves existing pool prices and entry weights on rerun.

The implementation was tested against a separate generated LocalDB database. The application database has not been migrated automatically.

Deploy the updated game API instances together: all EF wallet writers now check `RowVersion`, preventing stale writes from another feature from overwriting a recruitment debit. The game API maps a failed concurrency check to HTTP 409 / `RESOURCE_CHANGED` through its game-specific exception filter. EF-specific handling remains outside Core.Web.

Existing duplicate player heroes are preserved. New recruitment checks ownership under a per-player wallet lock and converts later acquisitions to stones; it does not add a unique ownership constraint that might reject existing data.

All recruitment queries and staged writes use UnitOfWork repositories. Player and wallet
lookups reuse `IGamePlayerService`; the parameterized wallet lock runs through
`IUnitOfWork.Sql.ExecuteProcedureAsync` (`dbo.usp_HRK_LockPlayerWallet`) on the same connection and transaction, before loading the
tracked wallet. Reward granting never saves independently. Tower locking, event-period
race recovery, activity publication tracking and the database health check also use
UnitOfWork rather than injecting GameDbContext directly.

Also apply `Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Create_GameOperationProcedures.sql`
before deploying the updated API. This rerunnable script creates/updates the wallet lock
and Tower application-lock procedures. See [GAME operation procedures](game-operation-procedures.md).

## Initial balance settings

| Pool | Currency | ×1 | ×10 | Approximate Rare / Epic / Legendary weights |
| --- | --- | ---: | ---: | --- |
| NORMAL | Gold | 10,000 | 100,000 | 80% / 19% / 1% |
| PREMIUM | Diamonds | 200 | 2,000 | 65% / 30% / 5% |

These are editable starting values. No ×10 discount or pity guarantee is enabled. Draws are independent, with replacement. The initial seed includes existing Rare, Epic and Legendary templates. Actual probabilities are calculated from enabled entry weights and displayed to the player; missing rarity groups or integer rounding affect totals. Adding a template requires adding/rebalancing its pool entry deliberately. Disable a pool while changing its configuration.

Edit `HRK_SummonPools.SingleCost/TenCost/IsEnabled` and `HRK_SummonPoolEntries.Weight/IsEnabled`. Each weight must be positive, and the sum of enabled weights must fit an Int32. All eligible heroes must have progression, first-star attribute and active duplicate-conversion configuration. The server validates initialization and inventory space before rolling.

## Transactions and rewards

The controller sends CQRS requests to handlers in `GAME.Application/Features`. `SummonCommandHandler` owns the draw use case; configuration and receipt reads have separate query handlers. `ExecuteInTransactionAsync` owns one final save and commit. Each transaction locks the player's wallet row, checks for an existing `(PlayerId, RequestId)` receipt, validates the pool/payment/capacity, selects heroes using cryptographic random numbers, stages rewards and saves the immutable result snapshot and individual draw records. See [the GAME CQRS structure](game-cqrs.md) for shared components and transaction ownership.

New heroes start at level 1 and one star, with configured first-star bonus attributes, level experience requirements and calculated combat power. Existing heroes and repeats inside the same batch become stones according to `HrkHeroStoneConfig.DuplicateConversionQuantity`. Stones fill available stacks and respect `MaxStackSize` and bag capacity. Capacity is reserved for the worst possible draw sequence before randomness, so a full bag cannot selectively reject duplicate outcomes and bias successful draws.

The existing acquisition history remains one record per hero acquisition, with a separate GUID and a batch/draw source reference. The new batch records own the client request ID. A failed reward grant or save rolls back payment, heroes, stones and history together.

## API and recovery

All endpoints require authentication and use the existing unified response envelope:

- `GET /api/summon/configuration`: current player ID, wallet, enabled pools and per-hero probabilities.
- `POST /api/summon`: `{ requestId, poolCode, count: 1 | 10 }`.
- `GET /api/summon/results/{requestId}`: the authenticated player's committed result.

The frontend uses the existing gateway catch-all route at `/gateway/dcs-game/...`; no new gateway route is necessary. Client payloads never select a reward or set a price.

Reusing the same request ID and normalized pool/count returns the original receipt without another debit or roll. A different payload with that ID returns `SUMMON_REQUEST_CONFLICT`. Receipts are scoped to the authenticated player.

The page stores its pending request in session storage under `hrk.summon.pending:<playerId>` before sending it. It keeps that request until the result is acknowledged, including while the reel plays. Reloads and unknown network/system failures look up the existing receipt; if the server has no receipt, the original request ID is reused. New requests are blocked until the pending result is resolved. HTTP calls time out after 30 seconds. Storage failures leave in-memory recovery available and prompt the player to keep the page open.

Business failures use the shared toast warning path. Result rewards use the result modal. The reel contains cosmetic filler cards; its target is always the committed server reward, and resizing the viewport completes the reveal safely.

## Verification

```powershell
$env:HRK_SUMMON_SQL_TESTS = '1'
dotnet test Tests/dcs-game/GAME.Domain.Tests/GAME.Domain.Tests.csproj --no-restore
```

The SQL tests create and clean up their own `HRK_SummonTests_<guid>` LocalDB database. They verify migration creation/reruns, independent and concurrent replay, overspending, stale wallet writes, duplicate stacking, full-bag rejection before randomness, private receipts and rollback after a database save failure. Without the environment flag, SQL-specific tests are skipped; pure policy tests still run.

Angular tests cover pending-request recovery, uncertain responses, duplicate clicks, real page/button integration, ten-card rendering, reduced motion, and actual browser reel alignment at 320px/680px widths. Build the frontend with `ng build --configuration development`.

## Artwork

Both chest PNGs were generated using the built-in image generation tool with transparent backgrounds and copied into `HrkUi/src/assets/images/dcs-game/summon/`. See that directory's `README.md` for the exact prompts and saved paths. Motion, glows and the reel are implemented in CSS/Web Animations; no generated animation video is required.
