SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.HRK_PlayerEventPeriodProgresses', N'QuickClimbRunsUsed') IS NULL
BEGIN
    ALTER TABLE dbo.HRK_PlayerEventPeriodProgresses
    ADD QuickClimbRunsUsed INT NOT NULL
        CONSTRAINT DF_HRK_PlayerEventPeriodProgresses_QuickClimbRunsUsed DEFAULT (0);
END;

IF COL_LENGTH(N'dbo.HRK_TowerQuickClimbJobs', N'DailyRunNumber') IS NULL
BEGIN
    ALTER TABLE dbo.HRK_TowerQuickClimbJobs
    ADD DailyRunNumber INT NOT NULL
        CONSTRAINT DF_HRK_TowerQuickClimbJobs_DailyRunNumber DEFAULT (0);
END;

UPDATE dbo.HRK_GameEvents
SET RulesJson = JSON_MODIFY(
        CASE WHEN ISJSON(RulesJson) = 1 THEN RulesJson ELSE N'{}' END,
        N'$.quickClimbDailyLimit',
        3),
    UpdatedOn = SYSUTCDATETIME()
WHERE Code = N'TOWER_CLIMB'
  AND JSON_VALUE(
        CASE WHEN ISJSON(RulesJson) = 1 THEN RulesJson ELSE N'{}' END,
        N'$.quickClimbDailyLimit') IS NULL;

-- Jobs created with the old rule may start above floor 1 and cannot safely continue
-- under the new repeatable-reward rule. Stop only active legacy jobs; historical jobs
-- remain untouched and do not consume the newly introduced daily allowance.
UPDATE dbo.HRK_TowerQuickClimbJobs
SET Status = N'EXPIRED',
    StopReason = N'RULES_CHANGED',
    CompletedOnUtc = COALESCE(CompletedOnUtc, SYSUTCDATETIME()),
    UpdatedOnUtc = SYSUTCDATETIME()
WHERE Status IN (N'QUEUED', N'PROCESSING')
  AND DailyRunNumber = 0;

COMMIT TRANSACTION;

PRINT N'Đã thêm giới hạn leo nhanh theo ngày và bộ đếm theo kỳ.';
