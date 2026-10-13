-- Apply to the GAME database before deploying API instances using these procedures.
-- Requires dbo.HRK_PlayerWallets. Rerunnable on SQL Server 2016 SP1 or later.
-- Run with a batch-aware tool (SSMS/sqlcmd): GO separates procedure definitions.
-- Transactions belong to the caller; neither procedure begins, commits or rolls back one.

CREATE OR ALTER PROCEDURE dbo.usp_HRK_AcquireTowerPlayerLock
    @Resource NVARCHAR(255),
    @LockTimeoutMilliseconds INT = 15000
AS
BEGIN
    SET NOCOUNT ON;

    IF @@TRANCOUNT = 0
        THROW 51001, 'Tower player locking requires an active caller transaction.', 1;

    DECLARE @result INT;
    EXEC @result = sys.sp_getapplock
        @Resource = @Resource,
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = @LockTimeoutMilliseconds;

    IF @result < 0
        THROW 51000, 'Tower operation is busy. Please retry.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_HRK_LockPlayerWallet
    @PlayerId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF @@TRANCOUNT = 0
        THROW 51001, 'Player wallet locking requires an active caller transaction.', 1;

    -- Consume the row without returning a result set. Keep the lock until the caller finishes.
    -- Preserve the missing-wallet behavior: the application still performs its normal lookup.
    DECLARE @lockedPlayerId BIGINT;
    SELECT @lockedPlayerId = PlayerId
    FROM dbo.HRK_PlayerWallets WITH (UPDLOCK, HOLDLOCK)
    WHERE PlayerId = @PlayerId;
END;
GO
