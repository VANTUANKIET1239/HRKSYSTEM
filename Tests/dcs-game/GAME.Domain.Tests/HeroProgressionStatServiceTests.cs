using GAME.Domain.Entities;
using GAME.Infrastructure.Services;
using Xunit;

namespace GAME.Domain.Tests;

public class HeroProgressionStatServiceTests
{
    private static readonly HrkHeroTemplate Template = new() { BaseHp = 1000, BaseAtk = 100, BaseDef = 80, BaseSpd = 100, BaseMagicDamage = 120, BaseMagicResistance = 90, BaseCrit = 5, BaseCritDmg = 150, BaseLifesteal = 2, BaseAccuracy = 80, BaseResistance = 10, Name = "Test", Avatar = "test" };

    [Fact]
    public void Level40_StarGrowth_RecalculatesFromTemplateBase()
    {
        var sut = new HeroProgressionStatService();
        var stats = sut.Calculate(Template, 40, .05m, .20m);
        Assert.Equal(3340, stats.Hp); // 1000 * (1 + 39 * .05 * 1.20)
        Assert.Equal(334, stats.Atk);
        Assert.Equal(334, stats.Spd); // SPD follows the same configured base growth formula
    }

    [Fact]
    public void PercentageStats_DoNotReceiveLevelOrStarMultiplier()
    {
        var sut = new HeroProgressionStatService();
        var stats = sut.Calculate(Template, 40, .05m, .50m, new[] { ("CRIT_RATE", 2m), ("RESISTANCE", 3m) });
        Assert.Equal(7m, stats.Crit);
        Assert.Equal(13m, stats.Resistance);
        Assert.Equal(150m, stats.CritDmg);
    }
}
