-- Explicit monthly publication, not an application/startup seeder. Run after Migration_MonthlyCheckIn.sql.
-- Set @MonthKey and tune the reward rule below BEFORE publishing. Reruns preserve existing published rewards.
SET XACT_ABORT ON;
DECLARE @MonthKey NVARCHAR(7) = N'2026-10';
DECLARE @TimeZoneId NVARCHAR(128) = N'Asia/Ho_Chi_Minh';
DECLARE @SqlTimeZone NVARCHAR(128) = N'SE Asia Standard Time'; -- SQL Server equivalent of TimeZoneId
DECLARE @ResetTime TIME(0) = '00:00:00';
DECLARE @Month DATE = TRY_CONVERT(DATE, @MonthKey + N'-01', 23);
IF @Month IS NULL OR LEN(@MonthKey) <> 7 THROW 51024, 'Invalid MonthKey.', 1;
DECLARE @StartLocal DATETIME2 = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @ResetTime), CAST(@Month AS DATETIME2));
DECLARE @EndLocal DATETIME2 = DATEADD(MONTH, 1, @StartLocal);
DECLARE @StartUtc DATETIME2 = CAST((@StartLocal AT TIME ZONE @SqlTimeZone) AT TIME ZONE 'UTC' AS DATETIME2);
DECLARE @EndUtc DATETIME2 = CAST((@EndLocal AT TIME ZONE @SqlTimeZone) AT TIME ZONE 'UTC' AS DATETIME2);
BEGIN TRANSACTION;
DECLARE @CalendarId BIGINT;
SELECT @CalendarId = Id FROM dbo.HRK_CheckInCalendars WITH (UPDLOCK, HOLDLOCK)
WHERE Code = N'MONTHLY_CHECKIN' AND MonthKey = @MonthKey;
IF @CalendarId IS NULL
BEGIN
    INSERT dbo.HRK_CheckInCalendars(Code, Name, MonthKey, TimeZoneId, ResetTime, StartAtUtc, EndAtUtc,
        MinPlayerLevel, ConfigVersion, IsPublished, IsActive, CreatedAtUtc)
    VALUES(N'MONTHLY_CHECKIN', N'Điểm danh tháng ' + @MonthKey, @MonthKey, @TimeZoneId, @ResetTime,
        @StartUtc, @EndUtc, 1, 1, 0, 1, SYSUTCDATETIME());
    SET @CalendarId = SCOPE_IDENTITY();
    DECLARE @Day INT = 1;
    WHILE @Day <= DAY(EOMONTH(@Month))
    BEGIN
        -- Example currency-only rewards: 1000 gold/day; days 7/14/21/28 also grant 50 diamonds.
        -- Replace these values with the approved game economy before first publication.
        DECLARE @Rewards NVARCHAR(MAX) = N'[{"Type":"GOLD","Quantity":1000}]';
        IF @Day IN (7,14,21,28) SET @Rewards = N'[{"Type":"GOLD","Quantity":1000},{"Type":"DIAMOND","Quantity":50}]';
        INSERT dbo.HRK_CheckInRewardConfigs(CalendarId, DayOfMonth, RewardsJson) VALUES(@CalendarId, @Day, @Rewards);
        SET @Day += 1;
    END;
    UPDATE dbo.HRK_CheckInCalendars SET IsPublished = 1 WHERE Id = @CalendarId;
END;
IF OBJECT_ID(N'dbo.HRK_GameFeatureConfigs', N'U') IS NOT NULL AND
   NOT EXISTS(SELECT 1 FROM dbo.HRK_GameFeatureConfigs WHERE ActionCode = N'CHECK_IN')
    INSERT dbo.HRK_GameFeatureConfigs(Code, Name, Icon, Placement, ActionCode, DisplayOrder, IsEnabled, IsLocked,
        HasNotification, CreatedOn, UpdatedOn)
    VALUES(N'MONTHLY_CHECKIN', N'Điểm danh', N'bi-calendar-check', N'TOP_EVENT', N'CHECK_IN', 5, 1, 0, 0,
        SYSUTCDATETIME(), SYSUTCDATETIME());
COMMIT;
