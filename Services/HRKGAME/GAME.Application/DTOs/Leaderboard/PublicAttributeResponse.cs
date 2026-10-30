namespace GAME.Application.DTOs.Leaderboard;

/// <summary>A displayed equipment or hero attribute.</summary>
public sealed record PublicAttributeResponse(string Code, string Name, decimal Value, bool IsPercentage);
