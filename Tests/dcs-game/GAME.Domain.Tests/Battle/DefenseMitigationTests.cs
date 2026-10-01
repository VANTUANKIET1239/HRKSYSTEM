using GAME.Domain.Battle;
using GAME.Infrastructure.Services;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class DefenseMitigationTests
{
    [Theory]
    [InlineData(100, "PHYSICAL", 458)]
    [InlineData(1000, "PHYSICAL", 2619)]
    [InlineData(100, "MAGIC", 917)]
    [InlineData(1000, "MAGIC", 3667)]
    [InlineData(100, "TRUE", 5500)]
    [InlineData(1000, "TRUE", 5500)]
    public void Simulation_uses_configured_constant_and_correct_defense(int constant, string school, int expected)
    {
        var skill = new BattleSkill
        {
            Id = "TEST", Name = "Test", SkillTypeCode = BattleCodes.Normal,
            Effects = [new BattleSkillEffect
            {
                EffectTypeCode = BattleCodes.Damage, TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = school, BaseValue = 5500
            }]
        };
        var attacker = new BattleCombatant
        {
            Id = 1, Team = 0, Position = 1, Name = "Attacker", MaxHp = 100000,
            Hp = 100000, Spd = 200, BasicSkill = skill
        };
        var target = new BattleCombatant
        {
            Id = 2, Team = 1, Position = 1, Name = "Target", MaxHp = 100000,
            Hp = 100000, Spd = 100, Def = 1100, MagicResistance = 500, BasicSkill = skill
        };
        var result = new BattleSimulationEngine().Simulate(new BattleSimulationRequest
        {
            RandomSeed = 42, MaxRounds = 1,
            DefenseMitigationConstant = BattleMitigationConfig.Read(new Dictionary<string, decimal>
            {
                [BattleMitigationConfig.Code] = constant
            }),
            Combatants = [attacker, target]
        });
        Assert.Equal(expected, result.Events.First(e => e.EventType == "DAMAGE" && e.ActorId == 1).Value);
        Assert.Equal(100000, target.Hp); // Simulation must not mutate the input snapshot.
        Assert.Equal(1000m, target.DefenseMitigationConstant);
    }

    [Fact]
    public void Missing_config_defaults_to_1000() =>
        Assert.Equal(1000m, BattleMitigationConfig.Read(new Dictionary<string, decimal>()));

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Invalid_config_is_rejected(int value)
    {
        Assert.Throws<InvalidOperationException>(() => BattleMitigationConfig.Read(
            new Dictionary<string, decimal> { [BattleMitigationConfig.Code] = value }));
        Assert.Throws<ArgumentOutOfRangeException>(() => BattleStatCalculator.MitigateDamage(5500, 1100, value));
    }
}
