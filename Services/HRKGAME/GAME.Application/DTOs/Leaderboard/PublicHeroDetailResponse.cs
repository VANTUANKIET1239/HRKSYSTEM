namespace GAME.Application.DTOs.Leaderboard;

/// <summary>A public hero detail restricted to the target player's default formation.</summary>
public sealed record PublicHeroDetailResponse(long Id, string Name, string? Avatar, string RarityCode,
    int Level, int Stars, long Power, string FactionName, string ClassName, CalculatedStatsDto Stats,
    IReadOnlyList<SkillTemplateDto> Skills, IReadOnlyList<PublicEquipmentResponse> Equipment);
