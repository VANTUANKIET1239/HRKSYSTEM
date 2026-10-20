-- Explicitly register ONE job on the GAME database after Migration_EventScheduling.sql.
-- Change @EventCode to the specific event being registered. Existing job settings are preserved.
SET XACT_ABORT ON;
DECLARE @EventCode nvarchar(100) = N'TOWER_CLIMB';
DECLARE @EventId int;
SELECT @EventId = Id FROM dbo.HRK_GameEvents
WHERE Code = @EventCode AND ResetType = 'DAILY' AND EventType = 'TOWER';
IF @EventId IS NULL
    THROW 51000, 'The specified daily tower event does not exist.', 1;

BEGIN TRANSACTION;
INSERT INTO dbo.HRK_JobDefinitions
    (JobCode, Name, Description, HandlerKey, ScopeKey, CronExpression, TimeZoneId, ParametersJson,
     IsEnabled, MisfirePolicy, MaxRetryAttempts, RetryDelaySeconds, TimeoutSeconds,
     RecoveryIntervalSeconds, ConfigVersion, CreatedAtUtc, UpdatedAtUtc, UpdatedBy)
SELECT CONCAT('EVENT_RESET:', e.Id), CONCAT('Daily reset: ', e.Code),
    'Create current period and settle every unclosed expired player period.',
    'EVENT_PERIOD_RESET', CONVERT(nvarchar(100), e.Id),
    CONCAT(DATEPART(SECOND, e.ResetTime), ' ', DATEPART(MINUTE, e.ResetTime), ' ', DATEPART(HOUR, e.ResetTime), ' * * ?'),
    e.TimeZoneId, '{"batchSize":100}', 1, 'FireOnceNow', 2, 5, 300, 300, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 'EXPLICIT_REGISTRATION'
FROM dbo.HRK_GameEvents e
WHERE e.Id = @EventId
  AND NOT EXISTS (SELECT 1 FROM dbo.HRK_JobDefinitions j WITH (UPDLOCK, HOLDLOCK)
                  WHERE j.HandlerKey = 'EVENT_PERIOD_RESET' AND j.ScopeKey = CONVERT(nvarchar(100), @EventId));
COMMIT;
