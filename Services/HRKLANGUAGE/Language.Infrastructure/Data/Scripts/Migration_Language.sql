-- Run against the dedicated HRK_LANGUAGE database. Does not create the database.
-- Supports both a new installation and the former TranslationsJson schema.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.HRK_LanguageCultures', N'U') IS NULL
        CREATE TABLE dbo.HRK_LanguageCultures (
            Code NVARCHAR(20) NOT NULL CONSTRAINT PK_LanguageCultures PRIMARY KEY,
            Name NVARCHAR(100) NOT NULL, IsActive BIT NOT NULL DEFAULT 1);
    IF OBJECT_ID(N'dbo.HRK_LanguageRevisions', N'U') IS NULL
        CREATE TABLE dbo.HRK_LanguageRevisions (
            Id BIGINT IDENTITY NOT NULL CONSTRAINT PK_LanguageRevisions PRIMARY KEY,
            [Group] NVARCHAR(40) NOT NULL, Culture NVARCHAR(20) NOT NULL,
            -- Set only after all text rows have been inserted, in the same transaction.
            PublishedAtUtc DATETIME2 NULL, PublishedBy NVARCHAR(128) NOT NULL,
            CONSTRAINT AK_LanguageRevisions_Identity UNIQUE (Id, [Group], Culture),
            CONSTRAINT FK_LanguageRevisions_Culture FOREIGN KEY (Culture) REFERENCES dbo.HRK_LanguageCultures(Code));
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.HRK_LanguageRevisions')
        AND name = N'PublishedAtUtc' AND is_nullable = 0)
        ALTER TABLE dbo.HRK_LanguageRevisions ALTER COLUMN PublishedAtUtc DATETIME2 NULL;
    IF OBJECT_ID(N'dbo.HRK_LanguageKeys', N'U') IS NULL
        CREATE TABLE dbo.HRK_LanguageKeys (
            Id BIGINT IDENTITY NOT NULL CONSTRAINT PK_LanguageKeys PRIMARY KEY,
            [Group] NVARCHAR(40) NOT NULL,
            [Key] NVARCHAR(256) COLLATE Latin1_General_100_BIN2 NOT NULL,
            CONSTRAINT AK_LanguageKeys_GroupKey UNIQUE ([Group], [Key]),
            CONSTRAINT CK_LanguageKeys_Key CHECK (LEN([Key]) > 0 AND DATALENGTH([Key]) = DATALENGTH(LTRIM(RTRIM([Key])))));
    IF OBJECT_ID(N'dbo.HRK_LanguageRevisionTexts', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.HRK_LanguageRevisionTexts (
            RevisionId BIGINT NOT NULL, LanguageKeyId BIGINT NOT NULL,
            [Value] NVARCHAR(MAX) NOT NULL,
            CONSTRAINT PK_LanguageRevisionTexts PRIMARY KEY (RevisionId, LanguageKeyId),
            CONSTRAINT FK_LanguageRevisionTexts_Revision FOREIGN KEY (RevisionId) REFERENCES dbo.HRK_LanguageRevisions(Id),
            CONSTRAINT FK_LanguageRevisionTexts_Key FOREIGN KEY (LanguageKeyId) REFERENCES dbo.HRK_LanguageKeys(Id),
            CONSTRAINT CK_LanguageRevisionTexts_Value CHECK (LEN([Value]) > 0));
        CREATE INDEX IX_LanguageRevisionTexts_LanguageKeyId ON dbo.HRK_LanguageRevisionTexts(LanguageKeyId);
    END;
    IF OBJECT_ID(N'dbo.HRK_LanguagePublications', N'U') IS NULL
        CREATE TABLE dbo.HRK_LanguagePublications (
            [Group] NVARCHAR(40) NOT NULL, Culture NVARCHAR(20) NOT NULL, RevisionId BIGINT NOT NULL,
            CONSTRAINT PK_LanguagePublications PRIMARY KEY ([Group], Culture),
            CONSTRAINT FK_LanguagePublications_Revision FOREIGN KEY (RevisionId, [Group], Culture)
                REFERENCES dbo.HRK_LanguageRevisions(Id, [Group], Culture));

    -- Dynamic SQL keeps fresh installations independent of the removed column.
    -- All historical revisions are converted, not just the current publications.
    IF COL_LENGTH(N'dbo.HRK_LanguageRevisions', N'TranslationsJson') IS NOT NULL
        EXEC sys.sp_executesql N'
            IF EXISTS (SELECT 1 FROM dbo.HRK_LanguageRevisions
                WHERE ISJSON(TranslationsJson) <> 1 OR LEFT(LTRIM(TranslationsJson), 1) <> ''{'')
                THROW 51006, ''Cannot migrate invalid language JSON.'', 1;
            IF EXISTS (SELECT 1 FROM dbo.HRK_LanguageRevisions r CROSS APPLY OPENJSON(r.TranslationsJson) j
                WHERE j.type <> 1 OR LEN(j.[key]) = 0 OR LEN(j.[value]) = 0 OR DATALENGTH(j.[key]) > 512
                    OR DATALENGTH(j.[key]) <> DATALENGTH(LTRIM(RTRIM(j.[key]))))
                OR EXISTS (SELECT r.Id, j.[key] COLLATE Latin1_General_100_BIN2
                    FROM dbo.HRK_LanguageRevisions r CROSS APPLY OPENJSON(r.TranslationsJson) j
                    GROUP BY r.Id, j.[key] COLLATE Latin1_General_100_BIN2 HAVING COUNT(*) > 1)
                THROW 51007, ''Cannot migrate invalid, duplicate or oversized translation keys/values.'', 1;
            INSERT dbo.HRK_LanguageKeys ([Group], [Key])
                SELECT DISTINCT r.[Group], j.[key] COLLATE Latin1_General_100_BIN2
                FROM dbo.HRK_LanguageRevisions r CROSS APPLY OPENJSON(r.TranslationsJson) j
                WHERE NOT EXISTS (SELECT 1 FROM dbo.HRK_LanguageKeys k
                    WHERE k.[Group] = r.[Group] AND k.[Key] = j.[key] COLLATE Latin1_General_100_BIN2);
            INSERT dbo.HRK_LanguageRevisionTexts (RevisionId, LanguageKeyId, [Value])
                SELECT r.Id, k.Id, j.[value]
                FROM dbo.HRK_LanguageRevisions r CROSS APPLY OPENJSON(r.TranslationsJson) j
                JOIN dbo.HRK_LanguageKeys k ON k.[Group] = r.[Group] AND k.[Key] = j.[key] COLLATE Latin1_General_100_BIN2
                WHERE NOT EXISTS (SELECT 1 FROM dbo.HRK_LanguageRevisionTexts t WHERE t.RevisionId = r.Id AND t.LanguageKeyId = k.Id);
            IF EXISTS (
                SELECT r.Id, j.[key] COLLATE Latin1_General_100_BIN2, j.[value] COLLATE Latin1_General_100_BIN2, DATALENGTH(j.[value])
                FROM dbo.HRK_LanguageRevisions r CROSS APPLY OPENJSON(r.TranslationsJson) j
                EXCEPT
                SELECT t.RevisionId, k.[Key], t.[Value] COLLATE Latin1_General_100_BIN2, DATALENGTH(t.[Value])
                FROM dbo.HRK_LanguageRevisionTexts t JOIN dbo.HRK_LanguageKeys k ON k.Id = t.LanguageKeyId)
                OR EXISTS (
                SELECT t.RevisionId, k.[Key], t.[Value] COLLATE Latin1_General_100_BIN2, DATALENGTH(t.[Value])
                FROM dbo.HRK_LanguageRevisionTexts t JOIN dbo.HRK_LanguageKeys k ON k.Id = t.LanguageKeyId
                EXCEPT
                SELECT r.Id, j.[key] COLLATE Latin1_General_100_BIN2, j.[value] COLLATE Latin1_General_100_BIN2, DATALENGTH(j.[value])
                FROM dbo.HRK_LanguageRevisions r CROSS APPLY OPENJSON(r.TranslationsJson) j)
                THROW 51008, ''Language migration verification failed. No data has been removed.'', 1;
            ALTER TABLE dbo.HRK_LanguageRevisions DROP CONSTRAINT CK_LanguageRevisions_Json;
            ALTER TABLE dbo.HRK_LanguageRevisions DROP COLUMN TranslationsJson;';

    IF OBJECT_ID(N'dbo.Language_OutboxMessages', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Language_OutboxMessages (
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LanguageOutbox PRIMARY KEY,
            EventName NVARCHAR(200) NOT NULL, EventVersion INT NOT NULL DEFAULT 1,
            PublisherName NVARCHAR(100) NOT NULL, RoutingKey NVARCHAR(200) NOT NULL,
            Payload NVARCHAR(MAX) NOT NULL, Status TINYINT NOT NULL DEFAULT 1,
            RetryCount INT NOT NULL DEFAULT 0, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            NextAttemptAt DATETIME2 NULL, LastAttemptAt DATETIME2 NULL, PublishedAt DATETIME2 NULL,
            LockToken NVARCHAR(64) NULL, LockedUntil DATETIME2 NULL, LastError NVARCHAR(2000) NULL,
            PartitionKey NVARCHAR(200) NULL, Sequence BIGINT NULL,
            CorrelationId NVARCHAR(128) NULL, TraceParent NVARCHAR(128) NULL, TraceState NVARCHAR(512) NULL,
            CONSTRAINT CK_LanguageOutbox_Status CHECK (Status IN (1, 2, 3, 4)));
        CREATE INDEX IX_LanguageOutbox_Dispatch ON dbo.Language_OutboxMessages(Status, NextAttemptAt, CreatedAt);
        CREATE INDEX IX_LanguageOutbox_LockedUntil ON dbo.Language_OutboxMessages(LockedUntil);
        CREATE INDEX IX_LanguageOutbox_PartitionSequence ON dbo.Language_OutboxMessages(PartitionKey, Sequence);
    END;
    IF OBJECT_ID(N'dbo.Language_ProcessedMessages', N'U') IS NULL
        CREATE TABLE dbo.Language_ProcessedMessages (
            ConsumerName NVARCHAR(150) NOT NULL, MessageId NVARCHAR(100) NOT NULL,
            ProcessedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            CONSTRAINT PK_LanguageProcessed PRIMARY KEY (ConsumerName, MessageId));
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_LanguageCultures WHERE Code = N'vi-VN')
        INSERT dbo.HRK_LanguageCultures VALUES (N'vi-VN', N'Tiếng Việt', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_LanguageCultures WHERE Code = N'en-US')
        INSERT dbo.HRK_LanguageCultures VALUES (N'en-US', N'English', 1);
    IF TYPE_ID(N'dbo.HRK_LanguageTextTable') IS NULL
        EXEC(N'CREATE TYPE dbo.HRK_LanguageTextTable AS TABLE (
            [Key] NVARCHAR(256) COLLATE Latin1_General_100_BIN2 NOT NULL PRIMARY KEY,
            [Value] NVARCHAR(MAX) NOT NULL,
            CHECK (LEN([Key]) > 0 AND DATALENGTH([Key]) = DATALENGTH(LTRIM(RTRIM([Key])))),
            CHECK (LEN([Value]) > 0));');
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
CREATE OR ALTER TRIGGER dbo.TR_LanguageKey_Immutable ON dbo.HRK_LanguageKeys
AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51009, 'Language key identities are immutable. Create a new key.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_LanguageRevision_Immutable ON dbo.HRK_LanguageRevisions
AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    -- Only allow the publication transaction to seal a newly created revision.
    IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id = d.Id
        WHERE d.PublishedAtUtc IS NOT NULL OR i.Id IS NULL OR i.PublishedAtUtc IS NULL
            OR i.[Group] <> d.[Group] OR i.Culture <> d.Culture OR i.PublishedBy <> d.PublishedBy)
        THROW 51000, 'Published language revisions are immutable. Publish a new revision.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_LanguageRevisionText_Immutable ON dbo.HRK_LanguageRevisionTexts
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted) OR EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.HRK_LanguageRevisions r ON r.Id = i.RevisionId
        JOIN dbo.HRK_LanguageKeys k ON k.Id = i.LanguageKeyId
        WHERE r.PublishedAtUtc IS NOT NULL OR r.[Group] <> k.[Group])
        THROW 51011, 'Text rows must belong to an unsealed revision of the same group.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_LanguagePublication_Sealed ON dbo.HRK_LanguagePublications
AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.HRK_LanguageRevisions r ON r.Id = i.RevisionId
        WHERE r.PublishedAtUtc IS NULL)
        THROW 51012, 'Only sealed language revisions may be published.', 1;
END;
GO
-- Deployment entry point. @Texts contains individual rows, not a JSON document.
CREATE OR ALTER PROCEDURE dbo.HRK_PublishLanguageBundle
    @Group NVARCHAR(40), @Culture NVARCHAR(20), @Texts dbo.HRK_LanguageTextTable READONLY,
    @PublishedBy NVARCHAR(128) = N'script', @OnlyIfMissing BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @Group IS NULL OR LEN(@Group) = 0 OR @Group COLLATE Latin1_General_100_BIN2 LIKE '%[^a-z0-9-]%'
        OR LEFT(@Group, 1) COLLATE Latin1_General_100_BIN2 NOT LIKE '[a-z]'
        THROW 51001, 'Invalid language group.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_LanguageCultures WHERE Code COLLATE Latin1_General_100_BIN2 = @Culture COLLATE Latin1_General_100_BIN2 AND IsActive = 1)
        THROW 51002, 'Unsupported language culture.', 1;
    IF @PublishedBy IS NULL OR LEN(@PublishedBy) = 0
        THROW 51003, 'PublishedBy is required.', 1;
    BEGIN TRY
        BEGIN TRANSACTION;
        -- Cultures in a group share key identities, so serialize key creation across cultures.
        DECLARE @LockResult INT, @Resource NVARCHAR(255) = CONCAT(N'language:', @Group);
        EXEC @LockResult = sys.sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @LockResult < 0 THROW 51005, 'Cannot acquire language publication lock.', 1;
        DECLARE @Current BIGINT = (SELECT RevisionId FROM dbo.HRK_LanguagePublications WITH (UPDLOCK, HOLDLOCK)
            WHERE [Group] = @Group AND Culture = @Culture);
        IF (@OnlyIfMissing = 1 AND @Current IS NOT NULL) OR (@Current IS NOT NULL
            AND NOT EXISTS (
                SELECT [Key], [Value] COLLATE Latin1_General_100_BIN2, DATALENGTH([Value]) FROM @Texts
                EXCEPT
                SELECT k.[Key], t.[Value] COLLATE Latin1_General_100_BIN2, DATALENGTH(t.[Value])
                FROM dbo.HRK_LanguageRevisionTexts t JOIN dbo.HRK_LanguageKeys k ON k.Id = t.LanguageKeyId WHERE t.RevisionId = @Current)
            AND NOT EXISTS (
                SELECT k.[Key], t.[Value] COLLATE Latin1_General_100_BIN2, DATALENGTH(t.[Value])
                FROM dbo.HRK_LanguageRevisionTexts t JOIN dbo.HRK_LanguageKeys k ON k.Id = t.LanguageKeyId WHERE t.RevisionId = @Current
                EXCEPT
                SELECT [Key], [Value] COLLATE Latin1_General_100_BIN2, DATALENGTH([Value]) FROM @Texts))
        BEGIN
            COMMIT;
            RETURN;
        END;
        INSERT dbo.HRK_LanguageKeys ([Group], [Key])
            SELECT @Group, t.[Key] FROM @Texts t
            WHERE NOT EXISTS (SELECT 1 FROM dbo.HRK_LanguageKeys k WHERE k.[Group] = @Group AND k.[Key] = t.[Key]);
        INSERT dbo.HRK_LanguageRevisions ([Group], Culture, PublishedAtUtc, PublishedBy)
            VALUES (@Group, @Culture, NULL, @PublishedBy);
        DECLARE @Revision BIGINT = SCOPE_IDENTITY();
        INSERT dbo.HRK_LanguageRevisionTexts (RevisionId, LanguageKeyId, [Value])
            SELECT @Revision, k.Id, t.[Value] FROM @Texts t
            JOIN dbo.HRK_LanguageKeys k ON k.[Group] = @Group AND k.[Key] = t.[Key];
        UPDATE dbo.HRK_LanguageRevisions SET PublishedAtUtc = SYSUTCDATETIME() WHERE Id = @Revision;
        IF @Current IS NULL
            INSERT dbo.HRK_LanguagePublications VALUES (@Group, @Culture, @Revision);
        ELSE UPDATE dbo.HRK_LanguagePublications SET RevisionId = @Revision WHERE [Group] = @Group AND Culture = @Culture;
        DECLARE @Payload NVARCHAR(MAX) = (SELECT @Group AS [group], @Culture AS culture, @Revision AS revision FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        DECLARE @PartitionKey NVARCHAR(200) = CONCAT(@Resource, N':', @Culture);
        INSERT dbo.Language_OutboxMessages (Id, EventName, PublisherName, RoutingKey, Payload, PartitionKey, Sequence)
            VALUES (NEWID(), N'language.published.v1', N'LanguageCache', N'language.published.v1', @Payload, @PartitionKey, @Revision);
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO
