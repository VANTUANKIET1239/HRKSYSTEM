using System;
using System.Collections.Generic;
using GAME.Domain.Entities;
using GAME.Infrastructure.Services;
using Xunit;

namespace GAME.Domain.Tests
{
    public class EquipmentInstanceTests
    {
        [Fact]
        public void CalculateCurrentStats_UsesLinearFormula_WithBaseRolledValue()
        {
            // Arrange
            var calc = new ItemStatCalculationService();
            var item = new HrkPlayerInventory
            {
                Id = 1,
                PlayerId = 10,
                Enhancement = 3,
                EnhancementGrowthPercent = 12.0m, // 12% per level
                Attributes = new List<HrkPlayerInventoryAttribute>
                {
                    new()
                    {
                        AttributeTypeId = 1,
                        BaseRolledValue = 200m,
                        CurrentValue = 200m,
                        AttributeType = new HrkAttributeType { Id = 1, Code = "ATK", Name = "Tấn công", IsPercentage = false }
                    }
                }
            };

            // Act
            var stats = calc.CalculateCurrentStats(item);

            // Assert
            // 200 * (1 + 3 * 0.12) = 200 * 1.36 = 272
            Assert.True(stats.ContainsKey("ATK"));
            Assert.Equal(272m, stats["ATK"]);
        }

        [Fact]
        public void TwoItemsWithDifferentGrowth_HaveDifferentStatsAtSameLevel()
        {
            // Arrange
            var calc = new ItemStatCalculationService();
            var item1 = new HrkPlayerInventory
            {
                Id = 1,
                Enhancement = 3,
                EnhancementGrowthPercent = 6.2m,
                Attributes = new List<HrkPlayerInventoryAttribute>
                {
                    new()
                    {
                        AttributeTypeId = 1,
                        BaseRolledValue = 185m,
                        AttributeType = new HrkAttributeType { Id = 1, Code = "ATK", Name = "Tấn công", IsPercentage = false }
                    }
                }
            };

            var item2 = new HrkPlayerInventory
            {
                Id = 2,
                Enhancement = 3,
                EnhancementGrowthPercent = 13.7m,
                Attributes = new List<HrkPlayerInventoryAttribute>
                {
                    new()
                    {
                        AttributeTypeId = 1,
                        BaseRolledValue = 232m,
                        AttributeType = new HrkAttributeType { Id = 1, Code = "ATK", Name = "Tấn công", IsPercentage = false }
                    }
                }
            };

            // Act
            var stats1 = calc.CalculateCurrentStats(item1);
            var stats2 = calc.CalculateCurrentStats(item2);

            // Assert
            // item 1: 185 * (1 + 3 * 0.062) = 185 * 1.186 = 219.41
            // item 2: 232 * (1 + 3 * 0.137) = 232 * 1.411 = 327.35
            Assert.NotEqual(stats1["ATK"], stats2["ATK"]);
            Assert.Equal(219.41m, stats1["ATK"]);
            Assert.Equal(327.35m, stats2["ATK"]);
        }

        [Fact]
        public void FailureLevelDown_CalculatesDirectlyFromBaseRolledValue()
        {
            // Arrange
            var calc = new ItemStatCalculationService();
            var item = new HrkPlayerInventory
            {
                Id = 1,
                Enhancement = 4, // Was +4
                EnhancementGrowthPercent = 10.0m,
                Attributes = new List<HrkPlayerInventoryAttribute>
                {
                    new()
                    {
                        AttributeTypeId = 1,
                        BaseRolledValue = 100m,
                        AttributeType = new HrkAttributeType { Id = 1, Code = "ATK", Name = "Tấn công", IsPercentage = false }
                    }
                }
            };

            // Act: Fail and drop to +3
            item.Enhancement = 3;
            var statsAt3 = calc.CalculateCurrentStats(item);

            // Assert
            // 100 * (1 + 3 * 0.10) = 130
            Assert.Equal(130m, statsAt3["ATK"]);
        }
    }
}
