namespace GAME.Application.DTOs;
public sealed record SummonPoolDto(string Code, string Name, string Currency, long SingleCost,
    long TenCost, IReadOnlyList<SummonHeroDto> Heroes);
