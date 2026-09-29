using GAME.Application.DTOs;
using GAME.Domain.Battle;
using GAME.Infrastructure.Services;
using Xunit;

namespace GAME.Domain.Tests
{
    public class BattleStatisticsCalculatorTests
    {
        private readonly BattleInitialStateDto _initialState = new()
        {
            LeftTeam = new List<PlayerHeroDto>
            {
                new() { Id = 1, Name = "Hero Left 1", Avatar = "left1.png" },
                new() { Id = 2, Name = "Hero Left 2", Avatar = "left2.png" }
            },
            RightTeam = new List<PlayerHeroDto>
            {
                new() { Id = 10, Name = "Hero Right 1", Avatar = "right1.png" },
                new() { Id = 20, Name = "Hero Right 2", Avatar = "right2.png" }
            }
        };

        [Fact]
        public void Calculate_PhysicalDamage_CreditedCorrectly()
        {
            var events = new List<BattleEvent>
            {
                new()
                {
                    EventType = BattleCodes.Damage,
                    ActorId = 1,
                    TargetId = -10, // right team hero 10
                    DamageSchoolCode = BattleCodes.Physical,
                    HpBefore = 1000,
                    HpAfter = 850 // actual damage = 150
                }
            };

            var stats = BattleStatisticsCalculator.Calculate(_initialState, events);

            var hero1 = stats.First(s => s.CombatantId == 1);
            var hero10 = stats.First(s => s.CombatantId == -10);

            Assert.Equal(150, hero1.PhysicalDamageDealt);
            Assert.Equal(0, hero1.MagicDamageDealt);
            Assert.Equal(150, hero10.PhysicalDamageTaken);
            Assert.Equal(0, hero10.MagicDamageTaken);
        }

        [Fact]
        public void Calculate_MagicDamage_CreditedCorrectly()
        {
            var events = new List<BattleEvent>
            {
                new()
                {
                    EventType = BattleCodes.Damage,
                    ActorId = 2,
                    TargetId = -20,
                    DamageSchoolCode = BattleCodes.Magic,
                    HpBefore = 2000,
                    HpAfter = 1600 // actual damage = 400
                }
            };

            var stats = BattleStatisticsCalculator.Calculate(_initialState, events);

            var hero2 = stats.First(s => s.CombatantId == 2);
            var hero20 = stats.First(s => s.CombatantId == -20);

            Assert.Equal(400, hero2.MagicDamageDealt);
            Assert.Equal(0, hero2.PhysicalDamageDealt);
            Assert.Equal(400, hero20.MagicDamageTaken);
            Assert.Equal(0, hero20.PhysicalDamageTaken);
        }

        [Fact]
        public void Calculate_TrueDamage_MappedToPhysical()
        {
            var events = new List<BattleEvent>
            {
                new()
                {
                    EventType = BattleCodes.Damage,
                    ActorId = 1,
                    TargetId = -10,
                    DamageSchoolCode = BattleCodes.True,
                    HpBefore = 1000,
                    HpAfter = 700 // actual damage = 300
                }
            };

            var stats = BattleStatisticsCalculator.Calculate(_initialState, events);

            var hero1 = stats.First(s => s.CombatantId == 1);
            var hero10 = stats.First(s => s.CombatantId == -10);

            Assert.Equal(300, hero1.PhysicalDamageDealt);
            Assert.Equal(0, hero1.MagicDamageDealt);
            Assert.Equal(300, hero10.PhysicalDamageTaken);
            Assert.Equal(0, hero10.MagicDamageTaken);
        }

        [Fact]
        public void Calculate_Healing_NoOverheal_CountsActualHpRecovered()
        {
            var events = new List<BattleEvent>
            {
                new()
                {
                    EventType = BattleCodes.Heal,
                    ActorId = 2,
                    TargetId = 1,
                    HpBefore = 500,
                    HpAfter = 800 // actual healed = 300
                },
                new()
                {
                    EventType = BattleCodes.Heal,
                    ActorId = 2,
                    TargetId = 1,
                    HpBefore = 1000,
                    HpAfter = 1000 // overheal on full HP = 0
                }
            };

            var stats = BattleStatisticsCalculator.Calculate(_initialState, events);

            var hero2 = stats.First(s => s.CombatantId == 2);
            Assert.Equal(300, hero2.HealingDone);
        }

        [Fact]
        public void Calculate_ShieldAbsorption_DoesNotCountAsDamageTaken()
        {
            // When damage is completely absorbed by shield, HpBefore == HpAfter
            var events = new List<BattleEvent>
            {
                new()
                {
                    EventType = BattleCodes.Damage,
                    ActorId = -10,
                    TargetId = 1,
                    DamageSchoolCode = BattleCodes.Physical,
                    HpBefore = 1000,
                    HpAfter = 1000 // 0 actual HP lost due to shield
                }
            };

            var stats = BattleStatisticsCalculator.Calculate(_initialState, events);

            var hero1 = stats.First(s => s.CombatantId == 1);
            var hero10 = stats.First(s => s.CombatantId == -10);

            Assert.Equal(0, hero10.PhysicalDamageDealt);
            Assert.Equal(0, hero1.PhysicalDamageTaken);
        }

        [Fact]
        public void Calculate_DamageOverTime_CreditedToSourceHeroId()
        {
            // DoT trigger: ActorId might be system/turn, but SourceHeroId is the hero who applied the DoT
            var events = new List<BattleEvent>
            {
                new()
                {
                    EventType = BattleCodes.Damage,
                    ActorId = 0, // system
                    SourceHeroId = 1, // original applier Hero Left 1
                    TargetId = -10,
                    DamageSchoolCode = BattleCodes.Magic,
                    HpBefore = 800,
                    HpAfter = 650 // 150 damage
                }
            };

            var stats = BattleStatisticsCalculator.Calculate(_initialState, events);

            var hero1 = stats.First(s => s.CombatantId == 1);
            Assert.Equal(150, hero1.MagicDamageDealt);
        }

        [Fact]
        public void Calculate_ZeroEvents_ReturnsValidStatsWithZeros()
        {
            var stats = BattleStatisticsCalculator.Calculate(_initialState, new List<BattleEvent>());

            Assert.Equal(4, stats.Count);
            foreach (var s in stats)
            {
                Assert.Equal(0, s.PhysicalDamageDealt);
                Assert.Equal(0, s.MagicDamageDealt);
                Assert.Equal(0, s.HealingDone);
                Assert.Equal(0, s.PhysicalDamageTaken);
                Assert.Equal(0, s.MagicDamageTaken);
            }
        }
    }
}
