using System;
using System.Collections.Generic;
using System.Linq;
using GAME.Application.Common.Mappings;
using GAME.Domain.Entities;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class HeroStarAuraConfigTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Hero_With_Zero_Or_One_Star_Has_No_Star_Aura(int stars)
    {
        var hero = new HrkPlayerHero
        {
            Id = 1,
            HeroTemplateId = 1,
            Stars = stars,
            AuraTier = 3, // Legacy tier should not affect star aura
            HeroTemplate = new HrkHeroTemplate
            {
                Id = 1,
                Name = "K Cởi Trần",
                Avatar = "/assets/images/dcs-game/kiet.png",
                StarAuraConfigs = new List<HrkHeroStarAuraConfig>
                {
                    new() { HeroTemplateId = 1, StarLevel = 2, VisualKey = "kiet-red-lightning", AuraCode = "KIET_STAR_2", Name = "Xích Lôi Khởi Phát", IsActive = true },
                    new() { HeroTemplateId = 1, StarLevel = 3, VisualKey = "kiet-red-lightning", AuraCode = "KIET_STAR_3", Name = "Hồng Lôi Cuồng Nộ", IsActive = true }
                }
            }
        };

        var dto = GameDtoMapper.MapPlayerHero(hero);

        Assert.NotNull(dto);
        Assert.Equal(stars, dto.Stars);
        Assert.Null(dto.StarAura);
        Assert.Equal(3, dto.AuraTier);
    }

    [Theory]
    [InlineData(2, "KIET_STAR_2")]
    [InlineData(3, "KIET_STAR_3")]
    [InlineData(4, "KIET_STAR_4")]
    [InlineData(5, "KIET_STAR_5")]
    public void Hero_With_2_To_5_Stars_Receives_Correct_Star_Aura(byte starLevel, string expectedCode)
    {
        var hero = new HrkPlayerHero
        {
            Id = 1,
            HeroTemplateId = 1,
            Stars = starLevel,
            AuraTier = 1,
            HeroTemplate = new HrkHeroTemplate
            {
                Id = 1,
                Name = "K Cởi Trần",
                Avatar = "/assets/images/dcs-game/kiet.png",
                StarAuraConfigs = new List<HrkHeroStarAuraConfig>
                {
                    new() { HeroTemplateId = 1, StarLevel = 2, VisualKey = "kiet-red-lightning", AuraCode = "KIET_STAR_2", Name = "Xích Lôi Khởi Phát", IsActive = true },
                    new() { HeroTemplateId = 1, StarLevel = 3, VisualKey = "kiet-red-lightning", AuraCode = "KIET_STAR_3", Name = "Hồng Lôi Cuồng Nộ", IsActive = true },
                    new() { HeroTemplateId = 1, StarLevel = 4, VisualKey = "kiet-red-lightning", AuraCode = "KIET_STAR_4", Name = "Xích Lôi Thần Khí", IsActive = true },
                    new() { HeroTemplateId = 1, StarLevel = 5, VisualKey = "kiet-red-lightning", AuraCode = "KIET_STAR_5", Name = "Chí Tôn Lôi Thần", IsActive = true }
                }
            }
        };

        var dto = GameDtoMapper.MapPlayerHero(hero);

        Assert.NotNull(dto);
        Assert.NotNull(dto.StarAura);
        Assert.Equal(starLevel, dto.StarAura.StarLevel);
        Assert.Equal(expectedCode, dto.StarAura.AuraCode);
        Assert.Equal("kiet-red-lightning", dto.StarAura.VisualKey);
    }

    [Fact]
    public void Inactive_StarAura_Is_Not_Mapped()
    {
        var hero = new HrkPlayerHero
        {
            Id = 1,
            HeroTemplateId = 1,
            Stars = 3,
            HeroTemplate = new HrkHeroTemplate
            {
                Id = 1,
                Name = "K Cởi Trần",
                Avatar = "/assets/images/dcs-game/kiet.png",
                StarAuraConfigs = new List<HrkHeroStarAuraConfig>
                {
                    new() { HeroTemplateId = 1, StarLevel = 3, VisualKey = "kiet-red-lightning", AuraCode = "KIET_STAR_3", Name = "Hồng Lôi Cuồng Nộ", IsActive = false }
                }
            }
        };

        var dto = GameDtoMapper.MapPlayerHero(hero);

        Assert.NotNull(dto);
        Assert.Null(dto.StarAura);
    }

    [Fact]
    public void Missing_StarAura_Config_Does_Not_Fail_Mapping()
    {
        var hero = new HrkPlayerHero
        {
            Id = 2,
            HeroTemplateId = 2,
            Stars = 4,
            HeroTemplate = new HrkHeroTemplate
            {
                Id = 2,
                Name = "Nam Deadline",
                Avatar = "/assets/images/dcs-game/trg-kiet-covid.png",
                StarAuraConfigs = new List<HrkHeroStarAuraConfig>() // Empty configs
            }
        };

        var dto = GameDtoMapper.MapPlayerHero(hero);

        Assert.NotNull(dto);
        Assert.Equal(4, dto.Stars);
        Assert.Null(dto.StarAura);
    }

    [Fact]
    public void Stars_Greater_Than_Five_Are_Clamped_To_Five()
    {
        var hero = new HrkPlayerHero
        {
            Id = 1,
            HeroTemplateId = 1,
            Stars = 7, // Out of standard bounds
            HeroTemplate = new HrkHeroTemplate
            {
                Id = 1,
                Name = "K Cởi Trần",
                Avatar = "/assets/images/dcs-game/kiet.png",
                StarAuraConfigs = new List<HrkHeroStarAuraConfig>
                {
                    new() { HeroTemplateId = 1, StarLevel = 5, VisualKey = "kiet-red-lightning", AuraCode = "KIET_STAR_5", Name = "Chí Tôn Lôi Thần", IsActive = true }
                }
            }
        };

        var dto = GameDtoMapper.MapPlayerHero(hero);

        Assert.NotNull(dto);
        Assert.NotNull(dto.StarAura);
        Assert.Equal(5, dto.StarAura.StarLevel);
        Assert.Equal("KIET_STAR_5", dto.StarAura.AuraCode);
    }
}
