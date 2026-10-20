-- Run on the ACTIVITY database before deploying the updated consumer.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.PlayerActivities', 'GameEventId') IS NULL
    ALTER TABLE dbo.PlayerActivities ADD GameEventId int NULL;
IF COL_LENGTH('dbo.PlayerActivities', 'EventPeriodId') IS NULL
    ALTER TABLE dbo.PlayerActivities ADD EventPeriodId bigint NULL;
IF COL_LENGTH('dbo.PlayerActivities', 'PeriodKey') IS NULL
    ALTER TABLE dbo.PlayerActivities ADD PeriodKey nvarchar(50) NULL;
IF COL_LENGTH('dbo.PlayerActivities', 'JobExecutionId') IS NULL
    ALTER TABLE dbo.PlayerActivities ADD JobExecutionId uniqueidentifier NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.PlayerActivities') AND name = 'IX_PlayerActivities_PlayerId_GameEventId_PeriodKey_OccurredAt')
    EXEC(N'CREATE INDEX IX_PlayerActivities_PlayerId_GameEventId_PeriodKey_OccurredAt ON dbo.PlayerActivities(PlayerId, GameEventId, PeriodKey, OccurredAt)');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.PlayerActivities') AND name = 'IX_PlayerActivities_EventPeriodId')
    EXEC(N'CREATE INDEX IX_PlayerActivities_EventPeriodId ON dbo.PlayerActivities(EventPeriodId)');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.PlayerActivities') AND name = 'IX_PlayerActivities_JobExecutionId')
    EXEC(N'CREATE INDEX IX_PlayerActivities_JobExecutionId ON dbo.PlayerActivities(JobExecutionId)');
COMMIT;
