-- GAME database. Rerunnable, does not create calendars or reset any player state.
-- Also deploy Create_GameOperationProcedures.sql (shared wallet locking).
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.HRK_CheckInCalendars', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_CheckInCalendars (
        Id BIGINT IDENTITY PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        MonthKey NVARCHAR(7) NOT NULL,
        TimeZoneId NVARCHAR(128) NOT NULL,
        ResetTime TIME(0) NOT NULL,
        StartAtUtc DATETIME2 NOT NULL,
        EndAtUtc DATETIME2 NOT NULL,
        MinPlayerLevel INT NOT NULL,
        ConfigVersion INT NOT NULL,
        IsPublished BIT NOT NULL,
        IsActive BIT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        CONSTRAINT UQ_CheckInCalendars_Month UNIQUE(Code, MonthKey),
        CONSTRAINT CK_CheckInCalendars_Month CHECK(LEN(MonthKey) = 7 AND TRY_CONVERT(DATE, MonthKey + N'-01', 23) IS NOT NULL),
        CONSTRAINT CK_CheckInCalendars_Bounds CHECK(StartAtUtc < EndAtUtc AND ConfigVersion > 0 AND MinPlayerLevel > 0)
    );
END;
IF OBJECT_ID(N'dbo.HRK_CheckInRewardConfigs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_CheckInRewardConfigs (
        Id BIGINT IDENTITY PRIMARY KEY,
        CalendarId BIGINT NOT NULL REFERENCES dbo.HRK_CheckInCalendars(Id),
        DayOfMonth INT NOT NULL,
        RewardsJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT UQ_CheckInRewardConfigs_Day UNIQUE(CalendarId, DayOfMonth),
        CONSTRAINT CK_CheckInRewardConfigs_Day CHECK(DayOfMonth BETWEEN 1 AND 31),
        CONSTRAINT CK_CheckInRewardConfigs_Json CHECK(ISJSON(RewardsJson) = 1 AND LEFT(LTRIM(RewardsJson), 1) = N'[')
    );
END;
IF OBJECT_ID(N'dbo.HRK_PlayerCheckInClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerCheckInClaims (
        Id BIGINT IDENTITY PRIMARY KEY,
        PlayerId BIGINT NOT NULL REFERENCES dbo.HRK_Players(Id),
        CalendarId BIGINT NOT NULL REFERENCES dbo.HRK_CheckInCalendars(Id),
        CheckInDate DATE NOT NULL,
        RequestId UNIQUEIDENTIFIER NOT NULL,
        ConfigVersion INT NOT NULL,
        ClaimedAtUtc DATETIME2 NOT NULL,
        ResultJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT UQ_PlayerCheckInClaims_Day UNIQUE(PlayerId, CalendarId, CheckInDate),
        CONSTRAINT UQ_PlayerCheckInClaims_Request UNIQUE(PlayerId, RequestId),
        CONSTRAINT CK_PlayerCheckInClaims_Result CHECK(ISJSON(ResultJson) = 1)
    );
END;
COMMIT;
GO

CREATE OR ALTER TRIGGER dbo.TR_HRK_CheckInCalendars_Published ON dbo.HRK_CheckInCalendars
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    -- A published month is immutable, including deletion/unpublishing; operators may only pause/resume IsActive.
    IF EXISTS (
        SELECT 1 FROM deleted d WHERE d.IsPublished = 1 AND NOT EXISTS (
            SELECT 1 FROM inserted i WHERE i.Id = d.Id AND i.IsPublished = 1
                AND i.Code = d.Code AND i.Name = d.Name AND i.MonthKey = d.MonthKey
                AND i.TimeZoneId = d.TimeZoneId AND i.ResetTime = d.ResetTime
                AND i.StartAtUtc = d.StartAtUtc AND i.EndAtUtc = d.EndAtUtc
                AND i.MinPlayerLevel = d.MinPlayerLevel AND i.ConfigVersion = d.ConfigVersion
                AND i.CreatedAtUtc = d.CreatedAtUtc))
        THROW 51020, 'Published check-in calendars are immutable; prepare changes for another month.', 1;
    IF EXISTS (
        SELECT 1 FROM inserted i WHERE i.IsPublished = 1 AND (
            (SELECT COUNT(*) FROM dbo.HRK_CheckInRewardConfigs r WHERE r.CalendarId = i.Id) <> DAY(EOMONTH(CONVERT(DATE, i.MonthKey + N'-01', 23)))
            OR EXISTS (SELECT 1 FROM dbo.HRK_CheckInRewardConfigs r WHERE r.CalendarId = i.Id
                AND r.DayOfMonth NOT BETWEEN 1 AND DAY(EOMONTH(CONVERT(DATE, i.MonthKey + N'-01', 23))))))
        THROW 51021, 'Publish exactly one reward configuration for every day of the month.', 1;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.HRK_CheckInCalendars c ON c.Code = i.Code AND c.Id <> i.Id
        WHERE i.IsPublished = 1 AND c.IsPublished = 1 AND c.StartAtUtc < i.EndAtUtc AND c.EndAtUtc > i.StartAtUtc)
        THROW 51022, 'Published check-in calendars cannot overlap.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_HRK_CheckInRewardConfigs_Published ON dbo.HRK_CheckInRewardConfigs
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM dbo.HRK_CheckInCalendars c JOIN
            (SELECT CalendarId FROM inserted UNION SELECT CalendarId FROM deleted) changed ON c.Id = changed.CalendarId
        WHERE c.IsPublished = 1)
        THROW 51023, 'Rewards of a published check-in calendar cannot be changed.', 1;
END;
GO
