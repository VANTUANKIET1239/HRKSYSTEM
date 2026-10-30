using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GAME.Application.Common;

/// <summary>Shared lock identities. Existing names are preserved to coordinate with older GAME instances.</summary>
public static class GameLockResources
{
    // The legacy "ranking" name now coordinates all player-state mutations that affect rankings or spending.
    public static string PlayerState(long playerId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playerId);
        return "game:ranking:" + playerId.ToString(CultureInfo.InvariantCulture);
    }

    public static string TowerPlayer(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return "tower:player:" + userId;
    }

    public static string AccountInitialization(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return "player-initialize:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId)));
    }
}
