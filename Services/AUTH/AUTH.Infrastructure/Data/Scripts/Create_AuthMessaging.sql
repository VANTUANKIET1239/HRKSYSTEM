USE HRK_AUTH;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
-- Preserve legacy names; installations without this optional profile field need it for registration.
IF COL_LENGTH(N'dbo.HRK_Users',N'FullName') IS NULL
    ALTER TABLE dbo.HRK_Users ADD FullName nvarchar(256) NULL;
IF OBJECT_ID(N'dbo.HRK_AuthOutboxMessages',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_AuthOutboxMessages
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_HRK_AuthOutboxMessages PRIMARY KEY,
        EventName nvarchar(200) NOT NULL, EventVersion int NOT NULL DEFAULT(1),
        PublisherName nvarchar(100) NOT NULL, RoutingKey nvarchar(200) NOT NULL,
        Payload nvarchar(max) NOT NULL, Status tinyint NOT NULL DEFAULT(1), RetryCount int NOT NULL DEFAULT(0),
        CreatedAt datetime2 NOT NULL DEFAULT(SYSUTCDATETIME()), NextAttemptAt datetime2 NULL,
        LastAttemptAt datetime2 NULL, PublishedAt datetime2 NULL, LockToken nvarchar(64) NULL,
        LockedUntil datetime2 NULL, LastError nvarchar(2000) NULL, PartitionKey nvarchar(200) NULL,
        Sequence bigint NULL, CorrelationId nvarchar(128) NULL, TraceParent nvarchar(128) NULL, TraceState nvarchar(512) NULL,
        CONSTRAINT CK_HRK_AuthOutboxMessages_Status CHECK(Status IN(1,2,3,4))
    );
    CREATE INDEX IX_HRK_AuthOutboxMessages_Dispatch ON dbo.HRK_AuthOutboxMessages(Status,NextAttemptAt,CreatedAt);
    CREATE INDEX IX_HRK_AuthOutboxMessages_LockedUntil ON dbo.HRK_AuthOutboxMessages(LockedUntil);
    CREATE INDEX IX_HRK_AuthOutboxMessages_PartitionSequence ON dbo.HRK_AuthOutboxMessages(PartitionKey,Sequence);
END;
IF OBJECT_ID(N'dbo.HRK_AuthProcessedMessages',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_AuthProcessedMessages
    (
        ConsumerName nvarchar(150) NOT NULL, MessageId nvarchar(100) NOT NULL,
        ProcessedAt datetime2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT PK_HRK_AuthProcessedMessages PRIMARY KEY(ConsumerName,MessageId)
    );
    CREATE INDEX IX_HRK_AuthProcessedMessages_ProcessedAt ON dbo.HRK_AuthProcessedMessages(ProcessedAt);
END;
COMMIT;
