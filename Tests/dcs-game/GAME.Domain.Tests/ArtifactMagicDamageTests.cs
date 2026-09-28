using System;
using System.Collections.Generic;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using GAME.Infrastructure.Services;
using Xunit;

namespace GAME.Domain.Tests
{
    public class ArtifactMagicDamageTests
    {
        [Fact]
        public void Artifact_WithMagicDamage_CalculatesCorrectCurrentStats()
        {
            var itemCalculator = new ItemStatCalculationService();
            var artifact = new HrkPlayerInventory
            {
                Id = 15,
                PlayerId = 1,
                Enhancement = 5,
                EnhancementGrowthPercent = 10.0m, // 10% per level
                Stars = 2, // 20% bonus from stars
                Attributes = new List<HrkPlayerInventoryAttribute>
                {
                    new()
                    {
                        AttributeTypeId = 25,
                        BaseRolledValue = 120m, // Epic artifact roll (90-150)
                        AttributeType = new HrkAttributeType
                        {
                            Id = 25,
                            Code = "MAGIC_DAMAGE",
                            Name = "Sát thương phép",
                            IsPercentage = false
                        }
                    }
                }
            };

            var stats = itemCalculator.CalculateCurrentStats(artifact);

            Assert.True(stats.ContainsKey("MAGIC_DAMAGE"));
            // Formula: 120 * (1 + 5 * 0.10) * (1 + 2 * 0.10) = 120 * 1.5 * 1.2 = 216.0
            Assert.Equal(216.0m, stats["MAGIC_DAMAGE"]);
        }

        [Fact]
        public void HeroFinalStats_IncludeArtifactMagicDamage()
        {
            var itemCalculator = new ItemStatCalculationService();
            var heroCalculator = new HeroStatCalculationService(itemCalculator);

            var hero = new HrkPlayerHero
            {
                Id = 1,
                PlayerId = 1,
                Level = 1,
                Stars = 1,
                HeroTemplate = new HrkHeroTemplate
                {
                    Id = 101,
                    Name = "Thanh Thái Aura",
                    Avatar = "/avatar.png",
                    BaseHp = 500,
                    BaseAtk = 100,
                    BaseDef = 50,
                    BaseSpd = 100,
                    BaseMagicDamage = 150,
                    BaseMagicResistance = 50
                }
            };

            var artifact = new HrkPlayerInventory
            {
                Id = 20,
                PlayerId = 1,
                IsActive = true,
                IsEquipped = true,
                EquippedHeroId = hero.Id,
                Enhancement = 0,
                Stars = 0,
                Attributes = new List<HrkPlayerInventoryAttribute>
                {
                    new()
                    {
                        AttributeTypeId = 25,
                        BaseRolledValue = 100m,
                        AttributeType = new HrkAttributeType
                        {
                            Id = 25,
                            Code = "MAGIC_DAMAGE",
                            Name = "Sát thương phép",
                            IsPercentage = false
                        }
                    }
                }
            };

            var equipment = new HrkPlayerEquipment
            {
                PlayerId = hero.PlayerId,
                HeroId = hero.Id,
                ArtifactId = artifact.Id,
                Artifact = artifact
            };

            var result = heroCalculator.CalculateStats(hero, equipment);

            // Base 150 + Artifact 100 = 250
            Assert.Equal(250, result.FinalStats.MagicDamage);
        }

        [Fact]
        public void HeroFinalStats_LegacyMagicAtk_MappedToMagicDamage()
        {
            var itemCalculator = new ItemStatCalculationService();
            var heroCalculator = new HeroStatCalculationService(itemCalculator);

            var hero = new HrkPlayerHero
            {
                Id = 1,
                PlayerId = 1,
                Level = 1,
                Stars = 1,
                HeroTemplate = new HrkHeroTemplate
                {
                    Id = 101,
                    Name = "Thanh Thái Aura",
                    Avatar = "/avatar.png",
                    BaseMagicDamage = 100
                }
            };

            var artifact = new HrkPlayerInventory
            {
                Id = 21,
                PlayerId = 1,
                IsActive = true,
                IsEquipped = true,
                EquippedHeroId = hero.Id,
                Attributes = new List<HrkPlayerInventoryAttribute>
                {
                    new()
                    {
                        AttributeTypeId = 3,
                        BaseRolledValue = 75m,
                        AttributeType = new HrkAttributeType
                        {
                            Id = 3,
                            Code = "MAGIC_ATK",
                            Name = "Sát thương phép cũ",
                            IsPercentage = false
                        }
                    }
                }
            };

            var equipment = new HrkPlayerEquipment
            {
                PlayerId = hero.PlayerId,
                HeroId = hero.Id,
                ArtifactId = artifact.Id,
                Artifact = artifact
            };

            var result = heroCalculator.CalculateStats(hero, equipment);

            // 100 + 75 = 175
            Assert.Equal(175, result.FinalStats.MagicDamage);
        }

        [Fact]
        public void CombatPower_CalculatesWithMagicDamage()
        {
            var combatPowerService = new CombatPowerService(null!, null!);
            var configs = new List<CombatPowerConfigDto>
            {
                new() { StatCode = "HP", PowerPerUnit = 0.5m, IsEnabled = true },
                new() { StatCode = "ATK", PowerPerUnit = 2.0m, IsEnabled = true },
                new() { StatCode = "MAGIC_DAMAGE", PowerPerUnit = 1.5m, IsEnabled = true }
            };

            var stats = new CalculatedStatsDto
            {
                Hp = 1000,          // 1000 * 0.5 = 500
                Atk = 200,          // 200 * 2.0 = 400
                MagicDamage = 300   // 300 * 1.5 = 450
            };

            int power = combatPowerService.Calculate(stats, configs);

            // 500 + 400 + 450 = 1350
            Assert.Equal(1350, power);
        }
    }
}
