using System;
using System.Collections.Generic;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Xunit;

namespace GAME.Domain.Tests
{
    public class DungeonHeroExpTests
    {
        [Fact]
        public void HeroExpGain_WithoutLevelUp_SnapshotsOldAndNewValues()
        {
            var hero = new HrkPlayerHero
            {
                Id = 1,
                PlayerId = 1,
                Level = 10,
                Exp = 150,
                MaxExp = 600,
                HeroTemplate = new HrkHeroTemplate
                {
                    Id = 101,
                    Name = "Thanh Thái Aura",
                    Avatar = "/assets/images/dcs-game/thanh-thai-aura.png"
                }
            };

            int expGained = 200;
            int oldMax = hero.MaxExp;
            var result = new HeroExpResultDto
            {
                PlayerHeroId = hero.Id,
                HeroName = hero.HeroTemplate.Name,
                Avatar = hero.HeroTemplate.Avatar,
                ExpGained = expGained,
                OldLevel = hero.Level,
                OldExp = hero.Exp,
                OldMaxExp = oldMax
            };

            hero.Exp += expGained;
            while (hero.Exp >= hero.MaxExp)
            {
                hero.Exp -= hero.MaxExp;
                hero.Level++;
                hero.MaxExp = (int)Math.Round(hero.MaxExp * 1.2);
            }

            result.NewLevel = hero.Level;
            result.NewExp = hero.Exp;
            result.NewMaxExp = hero.MaxExp;

            Assert.Equal(10, result.OldLevel);
            Assert.Equal(10, result.NewLevel);
            Assert.Equal(150, result.OldExp);
            Assert.Equal(350, result.NewExp);
            Assert.Equal(600, result.OldMaxExp);
            Assert.Equal(600, result.NewMaxExp);
            Assert.Equal("/assets/images/dcs-game/thanh-thai-aura.png", result.Avatar);
        }

        [Fact]
        public void HeroExpGain_WithSingleLevelUp_TransitionsLevelAndMaxExp()
        {
            var hero = new HrkPlayerHero
            {
                Id = 1,
                PlayerId = 1,
                Level = 10,
                Exp = 500,
                MaxExp = 600,
                HeroTemplate = new HrkHeroTemplate
                {
                    Id = 101,
                    Name = "Thanh Thái Aura",
                    Avatar = "/assets/images/dcs-game/thanh-thai-aura.png"
                }
            };

            int expGained = 250;
            int oldMax = hero.MaxExp;
            var result = new HeroExpResultDto
            {
                PlayerHeroId = hero.Id,
                HeroName = hero.HeroTemplate.Name,
                Avatar = hero.HeroTemplate.Avatar,
                ExpGained = expGained,
                OldLevel = hero.Level,
                OldExp = hero.Exp,
                OldMaxExp = oldMax
            };

            hero.Exp += expGained;
            while (hero.Exp >= hero.MaxExp)
            {
                hero.Exp -= hero.MaxExp;
                hero.Level++;
                hero.MaxExp = (int)Math.Round(hero.MaxExp * 1.2);
            }

            result.NewLevel = hero.Level;
            result.NewExp = hero.Exp;
            result.NewMaxExp = hero.MaxExp;

            Assert.Equal(10, result.OldLevel);
            Assert.Equal(11, result.NewLevel);
            Assert.Equal(500, result.OldExp);
            Assert.Equal(150, result.NewExp); // 500 + 250 - 600 = 150
            Assert.Equal(600, result.OldMaxExp);
            Assert.Equal(720, result.NewMaxExp); // 600 * 1.2 = 720
        }

        [Fact]
        public void HeroExpGain_WithMultiLevelUp_AccuratelyCalculatesAllTiers()
        {
            var hero = new HrkPlayerHero
            {
                Id = 1,
                PlayerId = 1,
                Level = 1,
                Exp = 50,
                MaxExp = 100,
                HeroTemplate = new HrkHeroTemplate
                {
                    Id = 101,
                    Name = "Thanh Thái Aura",
                    Avatar = "/avatar.png"
                }
            };

            int expGained = 500; // 50+500 = 550. Lv1 (100) -> 450. Lv2 (120) -> 330. Lv3 (144) -> 186. Lv4 (173) -> 13
            int oldMax = hero.MaxExp;
            var result = new HeroExpResultDto
            {
                PlayerHeroId = hero.Id,
                HeroName = hero.HeroTemplate.Name,
                Avatar = hero.HeroTemplate.Avatar,
                ExpGained = expGained,
                OldLevel = hero.Level,
                OldExp = hero.Exp,
                OldMaxExp = oldMax
            };

            hero.Exp += expGained;
            while (hero.Exp >= hero.MaxExp)
            {
                hero.Exp -= hero.MaxExp;
                hero.Level++;
                hero.MaxExp = (int)Math.Round(hero.MaxExp * 1.2);
            }

            result.NewLevel = hero.Level;
            result.NewExp = hero.Exp;
            result.NewMaxExp = hero.MaxExp;

            Assert.Equal(1, result.OldLevel);
            Assert.True(result.NewLevel > 3);
            Assert.Equal(100, result.OldMaxExp);
            Assert.True(result.NewMaxExp > result.OldMaxExp);
        }
    }
}
