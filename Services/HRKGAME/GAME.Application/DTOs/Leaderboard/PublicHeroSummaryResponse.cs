namespace GAME.Application.DTOs.Leaderboard;

/// <summary>A hero placed in the inspected player's current default formation.</summary>
public sealed record PublicHeroSummaryResponse(long Id, string Name, string? Avatar, string RarityCode,
    int Level, int Stars, long Power, int Slot);
