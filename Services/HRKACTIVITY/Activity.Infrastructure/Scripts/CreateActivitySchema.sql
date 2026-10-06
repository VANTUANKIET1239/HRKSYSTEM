-- Execute on a dedicated Activity database. No Game tables are moved.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.PlayerActivities', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlayerActivities (
        Id bigint IDENTITY PRIMARY KEY,
        ActivityId uniqueidentifier NOT NULL,
        ActivityType nvarchar(100) NOT NULL,
        PlayerId bigint NULL,
        UserId nvarchar(128) NULL,
        EntityType nvarchar(100) NOT NULL,
        EntityId nvarchar(200) NOT NULL,
        CorrelationId nvarchar(128) NULL,
        TraceId nvarchar(32) NULL,
        SourceService nvarchar(100) NOT NULL,
        PayloadJson nvarchar(max) NOT NULL CHECK (ISJSON(PayloadJson) = 1),
        OccurredAt datetimeoffset NOT NULL,
        RecordedAt datetimeoffset NOT NULL
    );
    CREATE UNIQUE INDEX IX_PlayerActivities_ActivityId ON dbo.PlayerActivities(ActivityId);
    CREATE INDEX IX_PlayerActivities_PlayerId_OccurredAt ON dbo.PlayerActivities(PlayerId, OccurredAt DESC);
    CREATE INDEX IX_PlayerActivities_ActivityType_OccurredAt ON dbo.PlayerActivities(ActivityType, OccurredAt DESC);
    CREATE INDEX IX_PlayerActivities_CorrelationId ON dbo.PlayerActivities(CorrelationId);
    CREATE INDEX IX_PlayerActivities_EntityType_EntityId ON dbo.PlayerActivities(EntityType, EntityId);
END;
IF OBJECT_ID('dbo.Activity_ProcessedMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Activity_ProcessedMessages (
        ConsumerName nvarchar(150) NOT NULL,
        MessageId nvarchar(100) NOT NULL,
        ProcessedAt datetime2 NOT NULL,
        CONSTRAINT PK_Activity_ProcessedMessages PRIMARY KEY (ConsumerName, MessageId)
    );
    CREATE INDEX IX_Activity_ProcessedMessages_ProcessedAt ON dbo.Activity_ProcessedMessages(ProcessedAt);
END;
COMMIT;
