using GAME.Application.Common;
using GAME.Application.DTOs;
using Xunit;

namespace GAME.Domain.Tests;

public class HeroStarMaterialPolicyTests
{
    private static StarMaterialRequirementDto Stone(int owned, int required = 10) =>
        new() { Owned = owned, Required = required };

    [Theory]
    [InlineData(10, 0, 100, true)]
    [InlineData(0, 10, 100, true)]
    [InlineData(10, 10, 100, true)]
    [InlineData(5, 5, 100, false)] // Cannot mix two insufficient alternatives.
    [InlineData(0, 0, 100, false)]
    [InlineData(10, 10, 99, false)]
    public void Preview_RequiresGoldAndEitherStone(int universal, int hero, long gold, bool expected)
    {
        Assert.Equal(expected, HeroStarMaterialPolicy.CanUpgrade(gold, 100, Stone(universal), Stone(hero)));
    }

    [Theory]
    [InlineData("HERO", 0, 10, true)]
    [InlineData("UNIVERSAL", 10, 0, false)]
    [InlineData("UNIVERSAL", 10, 10, false)]
    [InlineData(null, 10, 10, true)]
    [InlineData(null, 10, 0, false)]
    public void SelectsOnlyRequestedAlternative(string? choice, int universal, int hero, bool expected)
    {
        Assert.Equal(expected, HeroStarMaterialPolicy.UseHeroStone(choice, Stone(universal), Stone(hero)));
    }

    [Theory]
    [InlineData("HERO", 10, 0)]
    [InlineData("UNIVERSAL", 0, 10)]
    [InlineData("INVALID", 10, 10)]
    [InlineData(null, 5, 5)]
    public void RejectsInvalidOrInsufficientSelection(string? choice, int universal, int hero)
    {
        Assert.Throws<InvalidOperationException>(() =>
            HeroStarMaterialPolicy.UseHeroStone(choice, Stone(universal), Stone(hero)));
    }

    [Fact]
    public void ZeroRequirementDoesNotEnableFreeUpgrades()
    {
        Assert.False(HeroStarMaterialPolicy.CanUpgrade(100, 100, Stone(0, 0), Stone(0, 0)));
    }
}
