-- Run AFTER Migration_EventTowerClimbSystem.sql. Does not overwrite tuned gameplay values.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.HRK_PlayerTowerBattles', 'ResultJson') IS NULL
    ALTER TABLE dbo.HRK_PlayerTowerBattles ADD ResultJson NVARCHAR(MAX) NULL;

-- Existing duplicates require operator review, never delete player data automatically.
IF EXISTS (
    SELECT PlayerId FROM dbo.HRK_TowerQuickClimbJobs
    WHERE Status IN ('QUEUED', 'PROCESSING') GROUP BY PlayerId HAVING COUNT(*) > 1
)
    THROW 51001, 'Multiple active tower jobs exist for a player; resolve them before applying this migration.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TowerJob_ActivePlayer'
    AND object_id = OBJECT_ID('dbo.HRK_TowerQuickClimbJobs'))
    CREATE UNIQUE INDEX UX_TowerJob_ActivePlayer ON dbo.HRK_TowerQuickClimbJobs(PlayerId)
    WHERE Status IN ('QUEUED', 'PROCESSING');

IF EXISTS (
    SELECT PlayerId, EventPeriodId, SourceRefId FROM dbo.HRK_PlayerPendingRewards
    WHERE SourceType = 'UNCLAIMED_MILESTONE_CHEST'
    GROUP BY PlayerId, EventPeriodId, SourceRefId HAVING COUNT(*) > 1
)
    THROW 51002, 'Duplicate pending milestone rewards exist; review before applying this migration.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TowerPending_Milestone'
    AND object_id = OBJECT_ID('dbo.HRK_PlayerPendingRewards'))
    CREATE UNIQUE INDEX UX_TowerPending_Milestone
    ON dbo.HRK_PlayerPendingRewards(PlayerId, EventPeriodId, SourceRefId)
    WHERE SourceType = 'UNCLAIMED_MILESTONE_CHEST';

-- Fill absent keys only; retain all administrator tuning.
UPDATE dbo.HRK_GameEvents SET RulesJson = '{}' WHERE Code = 'TOWER_CLIMB' AND RulesJson IS NULL;
IF EXISTS (SELECT 1 FROM dbo.HRK_GameEvents WHERE Code = 'TOWER_CLIMB' AND ISJSON(RulesJson) <> 1)
    THROW 51003, 'Invalid tower RulesJson.', 1;
DECLARE @Defaults TABLE ([Key] NVARCHAR(50), [Value] DECIMAL(12,4));
INSERT @Defaults VALUES ('hpPerLevel',35),('attackPerLevel',6),('defensePerLevel',3),
    ('speedPerFloor',0.8),('maxSpeedBonus',50),('secondaryPerFloor',0.2),('maxCrit',35),('maxResistance',40);
DECLARE @Key NVARCHAR(50), @Value DECIMAL(12,4);
DECLARE rules_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT [Key], [Value] FROM @Defaults;
OPEN rules_cursor;
FETCH NEXT FROM rules_cursor INTO @Key, @Value;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF @Value = FLOOR(@Value)
        UPDATE dbo.HRK_GameEvents
        SET RulesJson = JSON_MODIFY(RulesJson, '$.' + @Key, CONVERT(INT, @Value))
        WHERE Code = 'TOWER_CLIMB' AND JSON_VALUE(RulesJson, '$.' + @Key) IS NULL;
    ELSE
    UPDATE dbo.HRK_GameEvents
    SET RulesJson = JSON_MODIFY(RulesJson, '$.' + @Key, @Value)
    WHERE Code = 'TOWER_CLIMB' AND JSON_VALUE(RulesJson, '$.' + @Key) IS NULL;
    FETCH NEXT FROM rules_cursor INTO @Key, @Value;
END;
CLOSE rules_cursor;
DEALLOCATE rules_cursor;
-- Milestones are represented by actual floor/chest records, not a second competing list.
UPDATE dbo.HRK_GameEvents
SET RulesJson = JSON_MODIFY(JSON_MODIFY(RulesJson, '$.bossMilestones', NULL), '$.eliteMilestones', NULL)
WHERE Code = 'TOWER_CLIMB';
INSERT dbo.HRK_PlayerTowerRuns
    (PlayerId, EventPeriodId, RunNumber, StartFloor, EndFloor, LivesRemaining, Status, StartAtUtc)
SELECT p.PlayerId, p.EventPeriodId, p.CurrentRunNumber, 1, p.CurrentFloor, p.RemainingLives,
    CASE WHEN p.IsCompleted = 1 THEN 'COMPLETED' WHEN p.RemainingLives = 0 THEN 'DEFEATED' ELSE 'IN_PROGRESS' END,
    SYSUTCDATETIME()
FROM dbo.HRK_PlayerEventPeriodProgresses p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.HRK_PlayerTowerRuns r
    WHERE r.PlayerId = p.PlayerId AND r.EventPeriodId = p.EventPeriodId AND r.RunNumber = p.CurrentRunNumber
);
COMMIT TRANSACTION;
