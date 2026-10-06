SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.HRK_TowerQuickClimbJobs', N'Version') IS NULL
BEGIN
    ALTER TABLE dbo.HRK_TowerQuickClimbJobs
        ADD Version BIGINT NOT NULL
            CONSTRAINT DF_HRK_TowerQuickClimbJobs_Version DEFAULT (0);
END;

IF OBJECT_ID(N'dbo.HRK_OutboxMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_OutboxMessages
    (
        Id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_HRK_OutboxMessages PRIMARY KEY,
        EventName NVARCHAR(200) NOT NULL,
        EventVersion INT NOT NULL
            CONSTRAINT DF_HRK_OutboxMessages_EventVersion DEFAULT (1),
        PublisherName NVARCHAR(100) NOT NULL,
        RoutingKey NVARCHAR(200) NOT NULL,
        Payload NVARCHAR(MAX) NOT NULL,
        Status TINYINT NOT NULL
            CONSTRAINT DF_HRK_OutboxMessages_Status DEFAULT (1),
        RetryCount INT NOT NULL
            CONSTRAINT DF_HRK_OutboxMessages_RetryCount DEFAULT (0),
        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_HRK_OutboxMessages_CreatedAt DEFAULT (SYSUTCDATETIME()),
        NextAttemptAt DATETIME2 NULL,
        LastAttemptAt DATETIME2 NULL,
        PublishedAt DATETIME2 NULL,
        LockToken NVARCHAR(64) NULL,
        LockedUntil DATETIME2 NULL,
        LastError NVARCHAR(2000) NULL,
        PartitionKey NVARCHAR(200) NULL,
        Sequence BIGINT NULL,
        CONSTRAINT CK_HRK_OutboxMessages_Status
            CHECK (Status IN (1, 2, 3, 4))
    );

    CREATE INDEX IX_HRK_OutboxMessages_Dispatch
        ON dbo.HRK_OutboxMessages(Status, NextAttemptAt, CreatedAt);
    CREATE INDEX IX_HRK_OutboxMessages_LockedUntil
        ON dbo.HRK_OutboxMessages(LockedUntil);
    CREATE INDEX IX_HRK_OutboxMessages_PartitionSequence
        ON dbo.HRK_OutboxMessages(PartitionKey, Sequence);
END;

IF OBJECT_ID(N'dbo.HRK_ProcessedMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_ProcessedMessages
    (
        ConsumerName NVARCHAR(150) NOT NULL,
        MessageId NVARCHAR(100) NOT NULL,
        ProcessedAt DATETIME2 NOT NULL
            CONSTRAINT DF_HRK_ProcessedMessages_ProcessedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_HRK_ProcessedMessages
            PRIMARY KEY (ConsumerName, MessageId)
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.HRK_ProcessedMessages')
      AND name = N'IX_HRK_ProcessedMessages_ProcessedAt'
)
BEGIN
    CREATE INDEX IX_HRK_ProcessedMessages_ProcessedAt
        ON dbo.HRK_ProcessedMessages(ProcessedAt);
END;

UPDATE dbo.HRK_TowerQuickClimbJobs
SET Version = 1
WHERE Version = 0
  AND Status IN (N'QUEUED', N'PROCESSING');

INSERT INTO dbo.HRK_OutboxMessages
(
    Id, EventName, EventVersion, PublisherName, RoutingKey, Payload,
    Status, RetryCount, CreatedAt, PartitionKey, Sequence
)
SELECT
    eventIds.EventId,
    N'game.tower.quick-climb.floor.requested.v1',
    1,
    N'GameEvents',
    N'game.tower.quick-climb.floor.requested.v1',
    CONCAT(
        N'{"eventId":"', CONVERT(NVARCHAR(36), eventIds.EventId),
        N'","jobId":"', STRING_ESCAPE(j.JobId, 'json'),
        N'","userId":"', STRING_ESCAPE(p.UserId, 'json'),
        N'","expectedFloor":', j.CurrentFloor,
        N',"version":', j.Version, N'}'),
    1,
    0,
    SYSUTCDATETIME(),
    j.JobId,
    j.Version
FROM dbo.HRK_TowerQuickClimbJobs j
INNER JOIN dbo.HRK_Players p ON p.Id = j.PlayerId
CROSS APPLY (SELECT NEWID() AS EventId WHERE j.JobId IS NOT NULL) eventIds
WHERE j.Status IN (N'QUEUED', N'PROCESSING')
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.HRK_OutboxMessages o
      WHERE o.PartitionKey = j.JobId
        AND o.EventName = N'game.tower.quick-climb.floor.requested.v1'
        AND o.Status IN (1, 2)
  );

COMMIT TRANSACTION;
