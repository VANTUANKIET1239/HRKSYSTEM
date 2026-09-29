using GAME.Application.DTOs;
using GAME.Domain.Entities;

namespace GAME.Application.Interfaces;

public interface IHeroProgressionStatService
{
    CalculatedStatsDto Calculate(HrkHeroTemplate template, int level, decimal rarityGrowthRate, decimal starGrowthBonusPercent, IEnumerable<(string Code, decimal Value)>? bonusAttributes = null);
    CalculatedStatsDto Subtract(CalculatedStatsDto next, CalculatedStatsDto current);
}
