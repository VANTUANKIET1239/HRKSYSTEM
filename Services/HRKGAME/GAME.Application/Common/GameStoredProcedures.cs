namespace GAME.Application.Common;

/// <summary>Database contracts deployed by Create_GameOperationProcedures.sql.</summary>
public static class GameStoredProcedures
{
    public const string AcquireTransactionLock = "dbo.usp_HRK_AcquireTransactionLock";
    public const string AcquireTowerPlayerLock = "dbo.usp_HRK_AcquireTowerPlayerLock";
    public const string LockPlayerWallet = "dbo.usp_HRK_LockPlayerWallet";
}
