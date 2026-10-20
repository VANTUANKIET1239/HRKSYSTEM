-- Run on the GAME database before deploying the scheduling-enabled binary.
-- Keeps the existing event reset times. Does not modify Quartz-owned tables.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.HRK_JobDefinitions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_JobDefinitions (
        Id bigint IDENTITY PRIMARY KEY,
        JobCode nvarchar(150) NOT NULL,
        Name nvarchar(200) NOT NULL,
        Description nvarchar(1000) NOT NULL,
        HandlerKey nvarchar(100) NOT NULL,
        ScopeKey nvarchar(100) NOT NULL,
        CronExpression nvarchar(150) NOT NULL,
        TimeZoneId nvarchar(100) NOT NULL,
        ParametersJson nvarchar(max) NOT NULL CHECK (ISJSON(ParametersJson) = 1),
        IsEnabled bit NOT NULL,
        MisfirePolicy nvarchar(30) NOT NULL,
        MaxRetryAttempts int NOT NULL CHECK (MaxRetryAttempts BETWEEN 0 AND 10),
        RetryDelaySeconds int NOT NULL CHECK (RetryDelaySeconds BETWEEN 0 AND 3600),
        TimeoutSeconds int NOT NULL CHECK (TimeoutSeconds BETWEEN 1 AND 86400),
        RecoveryIntervalSeconds int NOT NULL CHECK (RecoveryIntervalSeconds = 0 OR RecoveryIntervalSeconds BETWEEN 30 AND 86400),
        ConfigVersion bigint NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL,
        UpdatedBy nvarchar(128) NULL
    );
    CREATE UNIQUE INDEX IX_HRK_JobDefinitions_JobCode ON dbo.HRK_JobDefinitions(JobCode);
    CREATE UNIQUE INDEX IX_HRK_JobDefinitions_HandlerKey_ScopeKey ON dbo.HRK_JobDefinitions(HandlerKey, ScopeKey);
END;
IF OBJECT_ID('dbo.HRK_JobExecutions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_JobExecutions (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        JobDefinitionId bigint NOT NULL REFERENCES dbo.HRK_JobDefinitions(Id),
        ConfigVersion bigint NOT NULL,
        DefinitionSnapshotJson nvarchar(max) NOT NULL CHECK (ISJSON(DefinitionSnapshotJson) = 1),
        ScheduledAtUtc datetime2 NOT NULL,
        StartedAtUtc datetime2 NOT NULL,
        CompletedAtUtc datetime2 NULL,
        Status nvarchar(30) NOT NULL,
        Attempts int NOT NULL,
        ResultJson nvarchar(max) NULL,
        Error nvarchar(2000) NULL,
        InstanceId nvarchar(200) NOT NULL
    );
    CREATE INDEX IX_HRK_JobExecutions_JobDefinitionId_StartedAtUtc ON dbo.HRK_JobExecutions(JobDefinitionId, StartedAtUtc);
END;
IF COL_LENGTH('dbo.HRK_EventPeriods', 'ChestSnapshotJson') IS NULL
    ALTER TABLE dbo.HRK_EventPeriods ADD ChestSnapshotJson nvarchar(max) NULL;
IF COL_LENGTH('dbo.HRK_PlayerEventPeriodProgresses', 'ClosedAtUtc') IS NULL
    ALTER TABLE dbo.HRK_PlayerEventPeriodProgresses ADD ClosedAtUtc datetime2 NULL;
IF COL_LENGTH('dbo.HRK_PlayerEventPeriodProgresses', 'ResetExecutionId') IS NULL
    ALTER TABLE dbo.HRK_PlayerEventPeriodProgresses ADD ResetExecutionId uniqueidentifier NULL;
IF COL_LENGTH('dbo.HRK_PlayerEventPeriodProgresses', 'ResetSummaryJson') IS NULL
    ALTER TABLE dbo.HRK_PlayerEventPeriodProgresses ADD ResetSummaryJson nvarchar(max) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.HRK_PlayerEventPeriodProgresses') AND name = 'IX_HRK_PlayerEventPeriodProgresses_EventId_ClosedAtUtc_Id')
    EXEC(N'CREATE INDEX IX_HRK_PlayerEventPeriodProgresses_EventId_ClosedAtUtc_Id ON dbo.HRK_PlayerEventPeriodProgresses(EventId, ClosedAtUtc, Id)');

-- Job definitions are registered explicitly; see Register_TowerDailyResetJob.sql.
COMMIT;
