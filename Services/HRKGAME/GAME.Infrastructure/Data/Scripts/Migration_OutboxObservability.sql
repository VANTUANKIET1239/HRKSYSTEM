-- Run after Migration_TransactionalOutboxAndQuickClimbMessaging.sql, before new workers start.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.HRK_OutboxMessages', 'CorrelationId') IS NULL
    ALTER TABLE dbo.HRK_OutboxMessages ADD CorrelationId nvarchar(128) NULL;
IF COL_LENGTH('dbo.HRK_OutboxMessages', 'TraceParent') IS NULL
    ALTER TABLE dbo.HRK_OutboxMessages ADD TraceParent nvarchar(128) NULL;
IF COL_LENGTH('dbo.HRK_OutboxMessages', 'TraceState') IS NULL
    ALTER TABLE dbo.HRK_OutboxMessages ADD TraceState nvarchar(512) NULL;
COMMIT;
