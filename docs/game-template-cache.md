# GAME template cache

Each configured static table has one data entry in HybridCache: process memory (L1), then the existing Redis backend (L2). Cache loading uses a fresh scoped `IUnitOfWork`; it does not use the caller's transaction or tracked entities. There is no aggregate serialized snapshot and no separate source copy in Redis.

## Configuration

`Services/HRKGAME/HRK.GAME/appsettings.json` contains the complete list under `GameTemplateCache:Tables`. The shipped configuration contains the same 41 static entities as the previous implementation. Adding an already mapped entity requires adding its CLR name (for example `HrkHeroTemplate`) or full CLR name to this array, then restarting the app. No C# whitelist needs updating.

```json
{
  "GameTemplateCache": {
    "Enabled": true,
    "DataVersion": "2",
    "WarmupOnStartup": true,
    "AllowManualRefresh": false,
    "LocalExpirationMinutes": 30,
    "DistributedExpirationMinutes": 1440,
    "OperationTimeoutSeconds": 60,
    "MaximumTableBytes": 8387584,
    "Tables": ["HrkHeroTemplate", "HrkRarity", "HrkHeroFaction", "HrkHeroClass"]
  }
}
```

The abbreviated example above enables only those four entities; keep the full deployed list for existing game features. Configure only static data. Player, wallet, inventory, live dungeon definitions and other runtime state remain SQL reads.

The registry resolves names against EF model metadata at startup, before warmup. Missing, ambiguous or duplicate entities, owned entities, keyless entities and entities with shadow/field-only columns fail validation. TTLs must be positive. The per-table payload limit leaves 1 KiB below the shared `HybridCache:MaximumPayloadBytes` limit for serialization metadata.

`Enabled=false` bypasses both cache tiers while preserving configured relation hydration. Startup warmup is optional; an initial cold miss loads only the requested table and its configured relation group. Redis entries that remain valid are reused on restart.

## Using templates

Inject `IGameTemplateCatalog`:

```csharp
var heroes = await templates.GetAllAsync<HrkHeroTemplate>(ct);
var hero = heroes.FirstOrDefault(h => h.Id == heroId);
var rareHeroes = heroes.Where(h => h.RarityId == rarityId).ToList();
```

The collection is an `IReadOnlyList<T>`. Filtering and enumeration use ordinary LINQ in memory; there is no custom `IQueryable`, async query provider, `Include`, or EF async terminal operation on cached data. Entity objects themselves are mutable, so each scope deserializes its own copies. A caller's changes cannot alter globally cached data or another request's graph.

The registry derives static relation groups from EF foreign keys. Requesting heroes also retrieves their configured related tables and connects navigation properties such as rarity, faction, skills, effects and animations automatically. Handlers do not repeat joins. Disconnected tables, such as modal configuration, are not retrieved or materialized by a hero request. A request for a highly connected table can load many related tables; retaining complete navigation graphs has this cost.

The scoped catalog holds only relation groups actually requested. Its small type-to-list map supports relationship assembly and identity within a request; it is not a globally cached dictionary containing all tables. Only scalar columns are serialized into the individual HybridCache entries. Relations to unconfigured entities are excluded. Missing non-null parent rows inside a configured group cause validation failure.

Production DI always injects the configured catalog. `DatabaseTemplateCatalog` is a scalar-only fallback for direct construction of simple handlers/test doubles; relation-dependent consumers must inject the configured catalog even if caching is disabled.

Never attach cached copies to write repositories or tracked navigation properties. Commands persist IDs and use separate calculation views; existing `TemplateHydration` helpers hydrate detached player read models.

## Keys and coordinated refresh

Keys, before the Redis instance prefix, look like:

```text
templates:Development:tables-v1:d2:<table-set-hash>:revision
templates:Development:tables-v1:d2:<table-set-hash>:<revision>:GAME.Domain.Entities.HrkHeroTemplate
templates:Development:tables-v1:d2:<table-set-hash>:<revision>:GAME.Domain.Entities.HrkSkillTemplate
```

Each table has one data key per revision, shared by L1/L2. The only extra key is a small revision marker. It is needed so fresh scopes on other servers stop using old RAM entries and related tables can be published together. There are no `source` and `snapshot` copies. Old revisions expire through the configured TTL. The namespace includes a hash of the configured entity set, so different configurations cannot reuse incompatible relation groups. Increment `DataVersion` for incompatible entity/payload changes.

The Home button continues to call the authenticated CQRS refresh endpoint. Refresh loads all configured tables sequentially through one fresh UnitOfWork, validates relations and per-table sizes, writes each table through HybridCache, verifies that Redis actually holds every entry, then publishes the revision marker. Failed table writes/validation do not switch the active revision. Requests already using a revision retain it; new requests read the new revision. A lost acknowledgement of the final Redis write can still make a caller's result uncertain, as with any remote write.

`IGameTemplateRefresh.RefreshAsync<T>()` reloads the configured relation group containing T from SQL. Unrelated tables are copied from the previous cache revision into the new revision without querying them again. This avoids updating one side of a relationship independently. `RefreshAsync()` reloads all configured tables and remains the Home button's operation.

Before the first explicit publication, revision `initial` supports normal per-table cache-aside loading. Its independently expiring entries do not represent a historical database snapshot. Once a revision is explicitly published, missing entries are never rebuilt from current SQL under that old revision. A new scope encountering an expired published entry renews the complete generation. A scope already holding other groups does not silently switch revision midway through an operation; if an old, not-yet-loaded group has expired and is unavailable locally, that operation fails rather than mixing versions.

On a Redis outage, a server can use tables it already holds for the selected revision. An initial cold process can fall back to SQL. An explicitly published revision unavailable both locally and in Redis cannot be reconstructed as historical data. Manual refresh never reports success unless persistence was verified. Warmup failures are logged and later reads can retry.

Request coalescing and refresh serialization operate per process. There is no distributed refresh lock: concurrent nodes may duplicate loads; the last completed revision publication wins. SQL export is sequential, not snapshot-isolated. Commit related database edits before clicking refresh and avoid editing them during export. Refresh does not recalculate persisted player stats or rewrite battle logs/receipts.

## Home button and endpoints

```text
GET  /api/game-config/templates/capabilities
POST /api/game-config/templates/refresh
```

The shared response contract and Home confirmation/toast flows remain unchanged. Authenticated users in Development may refresh; outside Development, `AllowManualRefresh=true` and the Admin role are required. The capability is disabled when template caching is disabled. Overlapping manual refreshes on one node return `TEMPLATE_REFRESH_BUSY`.

No SQL migration is required. Deploy the backend and restart it to read the configured entity list. Old snapshot payload keys expire naturally; their former non-expiring revision pointer is unused.

## Validation

Tests cover per-table keys, cold coalescing, cross-node L2 reuse, config-only entity selection, invalid config, disconnected table loading, scalar serialization, automatic relations, request isolation, published revision pinning, partial/all refresh, failed L2 persistence, expired revision recovery, disabled caching, cancellation and endpoint access. SQL tests run against the uniquely named LocalDB fixture (`HRK_SUMMON_SQL_TESTS=1`), including recruitment, rollback, concurrent spending and detached cache graphs. They do not use the application database.
