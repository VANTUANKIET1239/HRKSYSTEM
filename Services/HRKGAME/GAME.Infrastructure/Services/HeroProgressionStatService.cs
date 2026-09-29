using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;

namespace GAME.Infrastructure.Services;

public class HeroProgressionStatService : IHeroProgressionStatService
{
    public CalculatedStatsDto Calculate(
        HrkHeroTemplate template,
        int level,
        decimal rarityGrowthRate,
        decimal starGrowthBonusPercent,
        IEnumerable<(string Code, decimal Value)>? bonuses = null)
    {
        var effectiveGrowth = rarityGrowthRate * (1m + starGrowthBonusPercent);
        var multiplier = 1m + Math.Max(0, level - 1) * effectiveGrowth;
        var result = new CalculatedStatsDto
        {
            Hp = Round(template.BaseHp * multiplier),
            Atk = Round(template.BaseAtk * multiplier),
            Def = Round(template.BaseDef * multiplier),
            Spd = Round(template.BaseSpd * multiplier),
            MagicDamage = Round(template.BaseMagicDamage * multiplier),
            MagicResistance = Round(template.BaseMagicResistance * multiplier),
            Crit = template.BaseCrit,
            CritDmg = template.BaseCritDmg,
            Lifesteal = template.BaseLifesteal,
            Accuracy = template.BaseAccuracy,
            Resistance = template.BaseResistance
        };

        foreach (var bonus in bonuses ?? Enumerable.Empty<(string Code, decimal Value)>())
        {
            Apply(result, bonus.Code, bonus.Value);
        }

        return result;
    }

    public CalculatedStatsDto Subtract(
        CalculatedStatsDto next,
        CalculatedStatsDto current)
    {
        return new CalculatedStatsDto
        {
            Hp = next.Hp - current.Hp,
            Atk = next.Atk - current.Atk,
            Def = next.Def - current.Def,
            Spd = next.Spd - current.Spd,
            MagicDamage = next.MagicDamage - current.MagicDamage,
            MagicResistance = next.MagicResistance - current.MagicResistance,
            Crit = next.Crit - current.Crit,
            CritDmg = next.CritDmg - current.CritDmg,
            Lifesteal = next.Lifesteal - current.Lifesteal,
            Accuracy = next.Accuracy - current.Accuracy,
            Resistance = next.Resistance - current.Resistance
        };
    }

    private static int Round(decimal value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
    private static void Apply(CalculatedStatsDto stats, string code, decimal value)
    {
        switch (code.ToUpperInvariant())
        {
            case "CRIT_RATE":
                stats.Crit += value;
                break;
            case "CRIT_DAMAGE":
                stats.CritDmg += value;
                break;
            case "LIFESTEAL":
                stats.Lifesteal += value;
                break;
            case "ACCURACY":
                stats.Accuracy += value;
                break;
            case "RESISTANCE":
                stats.Resistance += value;
                break;
        }
    }
}
