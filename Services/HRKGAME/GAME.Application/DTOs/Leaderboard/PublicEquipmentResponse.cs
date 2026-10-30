namespace GAME.Application.DTOs.Leaderboard;

/// <summary>Publicly visible equipped item, without inventory ownership flags or wallet information.</summary>
public sealed record PublicEquipmentResponse(string Slot, string Name, string? ImagePath, string? RarityCode,
    int Enhancement, int Stars, IReadOnlyList<PublicAttributeResponse> Attributes);
