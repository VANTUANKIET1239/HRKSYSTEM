namespace GAME.Application.DTOs.Leaderboard;

/// <summary>A numbered leaderboard page and the requesting player's position.</summary>
public sealed record LeaderboardResponse(string Type, int Page, int PageSize, long TotalCount,
    IReadOnlyList<LeaderboardEntryResponse> Items, LeaderboardEntryResponse? MyEntry, DateTimeOffset GeneratedAtUtc);
