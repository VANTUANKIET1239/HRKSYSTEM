namespace GAME.Application.DTOs;
public sealed record SummonHeroDto(int HeroTemplateId, string Name, string Avatar, string RarityCode,
    string RarityName, string? RarityColorHex, decimal ProbabilityPercent);
