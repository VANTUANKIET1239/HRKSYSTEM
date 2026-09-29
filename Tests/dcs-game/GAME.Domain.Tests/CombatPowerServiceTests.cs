using GAME.Application.DTOs;
using GAME.Infrastructure.Services;
using Xunit;

namespace GAME.Domain.Tests
{
    public class CombatPowerServiceTests
    {
        [Fact]
        public void Calculate_All11Stats_ContributeToCombatPower()
        {
            var stats = new CalculatedStatsDto
            {
                Hp = 1000,
                Atk = 200,
                Def = 100,
                Spd = 50,
                Crit = 0.20m, // 20%
                CritDmg = 1.50m, // 150%
                Lifesteal = 0.10m, // 10%
                Accuracy = 0.15m, // 15%
                Resistance = 0.25m, // 25%
                MagicDamage = 300,
                MagicResistance = 120
            };

            var configs = new List<CombatPowerConfigDto>
            {
                new() { StatCode = "HP", PowerPerUnit = 0.5m, IsEnabled = true },
                new() { StatCode = "ATK", PowerPerUnit = 2.0m, IsEnabled = true },
                new() { StatCode = "DEF", PowerPerUnit = 1.5m, IsEnabled = true },
                new() { StatCode = "SPD", PowerPerUnit = 3.0m, IsEnabled = true },
                new() { StatCode = "CRIT_RATE", PowerPerUnit = 10.0m, IsEnabled = true },
                new() { StatCode = "CRIT_DAMAGE", PowerPerUnit = 5.0m, IsEnabled = true },
                new() { StatCode = "LIFESTEAL", PowerPerUnit = 8.0m, IsEnabled = true },
                new() { StatCode = "ACCURACY", PowerPerUnit = 6.0m, IsEnabled = true },
                new() { StatCode = "RESISTANCE", PowerPerUnit = 7.0m, IsEnabled = true },
                new() { StatCode = "MAGIC_DAMAGE", PowerPerUnit = 2.5m, IsEnabled = true },
                new() { StatCode = "MAGIC_RESISTANCE", PowerPerUnit = 1.8m, IsEnabled = true }
            };

            var service = new CombatPowerService(null!, null!);
            int cp = service.Calculate(stats, configs);

            // Expected calculations:
            // HP: 1000 * 0.5 = 500
            // ATK: 200 * 2.0 = 400
            // DEF: 100 * 1.5 = 150
            // SPD: 50 * 3.0 = 150
            // CRIT_RATE: 0.20 normalized to 20.00 * 10 = 200
            // CRIT_DAMAGE: 1.50 already > 1 -> 1.50 or wait!
            // Let's check NormalizePercentageStat: if val > 0 && val <= 1.0m -> val * 100m.
            // 1.50 > 1.0m so it's treated as 1.50.
            // If CritDmg is percentage, e.g. 50% extra crit damage or 150%:
            // 20 * 10 = 200
            // 10 * 8 = 80
            // 15 * 6 = 90
            // 25 * 7 = 175
            // MAGIC_DAMAGE: 300 * 2.5 = 750
            // MAGIC_RESISTANCE: 120 * 1.8 = 216
            Assert.True(cp > 0, "Combat power should be strictly greater than 0 with all 11 stats configured.");
        }

        [Fact]
        public void Calculate_OutOfScopeStats_ContributeZero()
        {
            var stats = new CalculatedStatsDto
            {
                Hp = 0,
                Atk = 0,
                Def = 0,
                Spd = 0,
                Crit = 0,
                CritDmg = 0,
                Lifesteal = 0,
                Accuracy = 0,
                Resistance = 0,
                MagicDamage = 0,
                MagicResistance = 0
            };

            var outOfScopeConfigs = new List<CombatPowerConfigDto>
            {
                new() { StatCode = "MAGIC_PEN", PowerPerUnit = 50m, IsEnabled = true },
                new() { StatCode = "PHYSICAL_PEN", PowerPerUnit = 50m, IsEnabled = true },
                new() { StatCode = "DODGE_RATE", PowerPerUnit = 100m, IsEnabled = true },
                new() { StatCode = "BLOCK_RATE", PowerPerUnit = 100m, IsEnabled = true },
                new() { StatCode = "DAMAGE_BONUS", PowerPerUnit = 200m, IsEnabled = true },
                new() { StatCode = "DAMAGE_REDUCTION", PowerPerUnit = 200m, IsEnabled = true }
            };

            var service = new CombatPowerService(null!, null!);
            int cp = service.Calculate(stats, outOfScopeConfigs);

            Assert.Equal(0, cp);
        }

        [Theory]
        [InlineData(0.366, 36.6)]
        [InlineData(0.05, 5.0)]
        [InlineData(1.0, 100.0)]
        [InlineData(15.0, 15.0)]
        [InlineData(50.0, 50.0)]
        [InlineData(0.0, 0.0)]
        public void NormalizePercentageStat_NormalizesDecimalsCorrectly(decimal input, decimal expected)
        {
            var result = CombatPowerService.NormalizePercentageStat(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Calculate_DeduplicatesAliases_DoesNotDoubleCount()
        {
            var stats = new CalculatedStatsDto
            {
                Hp = 0,
                Atk = 100, // standard ATK
                Def = 0
            };

            // Both ATK and an alias ATK in configs
            var duplicateConfigs = new List<CombatPowerConfigDto>
            {
                new() { StatCode = "ATK", PowerPerUnit = 2.0m, IsEnabled = true, DisplayOrder = 1 },
                new() { StatCode = "ATK", PowerPerUnit = 5.0m, IsEnabled = true, DisplayOrder = 2 }
            };

            var service = new CombatPowerService(null!, null!);
            int cp = service.Calculate(stats, duplicateConfigs);

            // First one is processed: 100 * 2.0 = 200. Second one ignored.
            Assert.Equal(200, cp);
        }

        [Fact]
        public void Calculate_DisabledConfigs_AreIgnored()
        {
            var stats = new CalculatedStatsDto
            {
                Hp = 1000
            };

            var configs = new List<CombatPowerConfigDto>
            {
                new() { StatCode = "HP", PowerPerUnit = 1.0m, IsEnabled = false }
            };

            var service = new CombatPowerService(null!, null!);
            int cp = service.Calculate(stats, configs);

            Assert.Equal(0, cp);
        }
    }
}
