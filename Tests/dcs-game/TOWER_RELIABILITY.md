# Tower reliability changes

## Deployment

1. Stop game API/worker instances before migration (avoid running the old worker during rollout).
2. If this is a new database, run Migration_EventTowerClimbSystem.sql once.
3. Run Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_TowerClimbReliability.sql.
4. Deploy backend and frontend together, then restart API/worker.

The new migration adds ResultJson to battle history and filtered unique indexes.
It stops with a diagnostic if multiple active jobs or duplicate pending milestone rewards already exist.
Review such records manually; the migration never deletes rewards or chooses which live job to discard.
Old battles without a stored result cannot be replayed; the API returns an explicit error instead of fabricated data.
Do not rerun the original seed on a tuned database: its MERGE overwrites existing settings.

## Configuration sources

- HRK_GameEvents: InitialLives, MinPlayerLevel, IsOpen, StartTimeUtc, EndTimeUtc, MaxDailyRuns.
- ResetTime and TimeZoneId control the daily boundary. DAILY is currently the supported ResetType;
  other values fail validation rather than being silently treated as DAILY.
- LifeConsumeMode: ON_DEFEAT or ON_ENTRY. A winning ON_ENTRY battle that exhausts lives ends the run unless it completes the tower.
- RulesJson: maxFloor (optional; falls back to the highest active configured floor), hpPerLevel,
  attackPerLevel, defensePerLevel, speedPerFloor, maxSpeedBonus, secondaryPerFloor, maxCrit, maxResistance.
  Every floor from 1 through maxFloor must exist and be active.
- HRK_TowerFloors and HRK_TowerFloorEnemies: actual enemies and multiplicative stats.
  StatMultiplier is global; HpMultiplier/AtkMultiplier/DefMultiplier are additional multipliers.
  Existing difficulty may need tuning now that StatMultiplier actually participates in combat.
- HRK_TowerChestConfigs: actual chest milestones; there is no competing milestone list in RulesJson.
- RewardsJson is the authoritative reward list, including PLAYER_EXP and HERO_EXP on floors.
  Legacy GoldReward/PlayerExpReward/HeroExpReward columns are not the tuning source.
  Use itemTemplateId or code to reference arbitrary inventory materials. Legacy stone/charm aliases remain supported.
  EXP in non-battle reward contexts is rejected because no participant snapshot exists.
- HRK_BattleConfigs: both manual and quick climb use the same energy/round limits.
- App configuration section TowerWorker: BetweenFloorsMs (150), IdlePollMs (1000), ErrorBackoffMs (2000).

## Concurrency model

Each public tower operation owns an execution-strategy transaction and a SQL Server transaction-owned
application lock keyed by authenticated player identity. A worker processes one floor per transaction.
Other API instances/workers use the same lock; no expiring 30-second lease is needed.
The candidate floor is rechecked inside the lock, protecting duplicate worker picks and uncertain commit retries.
Progress, actual rewards, job position and persisted battle result commit together.
A process crash releases the SQL lock and rolls back the floor; the durable active job is picked up again.

This player lock covers tower operations, not unrelated inventory/wallet systems. Existing inventory and
equipment services must retain their own transaction/concurrency guarantees.

## Verification

- dotnet test Tests/dcs-game/GAME.Domain.Tests/GAME.Domain.Tests.csproj --no-restore
- node --test Tests/dcs-game/tower-contracts.test.cjs (uses TypeScript from HrkUi/node_modules)
- Angular development build.

TowerReliabilityTests call actual reward, experience, configuration and policy code through an in-memory
async repository double. These are NOT SQL Server concurrency tests.

Before release, verify on a disposable SQL Server database:
- Concurrent quick-start requests produce one active job.
- Two workers cannot commit the same floor twice; a second claim of pending rewards does not duplicate grants.
- Restart/kill a worker during a floor; only committed floors/rewards survive.
- Simulate 11:59:59 / 12:00; an accepted old-period battle never modifies new-period progress.
- Full bag, partial stack, equipment roll and retry claim.
- Manual battle timeline and result, selected formation, replay ownership and browser reload.
- Test UI on desktop/mobile; no live browser verification is claimed by automated build tests.
