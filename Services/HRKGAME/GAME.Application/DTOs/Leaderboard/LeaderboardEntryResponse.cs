namespace GAME.Application.DTOs.Leaderboard;

/// <summary>A player's globally numbered position in a leaderboard.</summary>
public sealed record LeaderboardEntryResponse(long Rank, RankedPlayerResponse Player, bool IsMe);
