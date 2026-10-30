namespace GAME.Application.DTOs.Leaderboard;

/// <summary>A public player identity without authentication or wallet data.</summary>
public sealed record RankedPlayerResponse(long PlayerId, string PlayerName, int Level, string AvatarType,
    string? AvatarUrl, long AvatarVersion, long Score, DateTimeOffset AchievedAtUtc);
