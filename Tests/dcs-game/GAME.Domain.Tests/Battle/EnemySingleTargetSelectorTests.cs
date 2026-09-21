using GAME.Domain.Battle;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class EnemySingleTargetSelectorTests
{
    private readonly EnemySingleTargetSelector _selector = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 3)]
    [InlineData(4, 5)]
    [InlineData(5, 5)]
    public void Selects_opposite_front_position_first(int actorPosition, int expectedTargetPosition)
    {
        var selected = Select(actorPosition, 1, 2, 3, 4, 5);
        Assert.Equal(expectedTargetPosition, selected.Single().Position);
    }

    [Fact]
    public void Left_lane_moves_across_front_row_before_back_row()
    {
        Assert.Equal(3, Select(1, 2, 3, 4, 5).Single().Position);
        Assert.Equal(5, Select(1, 2, 4, 5).Single().Position);
        Assert.Equal(2, Select(1, 2, 4).Single().Position);
        Assert.Equal(4, Select(1, 4).Single().Position);
    }

    [Fact]
    public void Right_lane_mirrors_front_and_back_priority()
    {
        Assert.Equal(3, Select(5, 1, 2, 3, 4).Single().Position);
        Assert.Equal(1, Select(5, 1, 2, 4).Single().Position);
        Assert.Equal(4, Select(5, 2, 4).Single().Position);
    }

    private IReadOnlyList<BattleCombatant> Select(int actorPosition, params int[] enemyPositions)
    {
        var actor = Hero(100, 0, actorPosition);
        return _selector.Select(new BattleTargetContext
        {
            Actor = actor,
            Allies = [actor],
            Enemies = enemyPositions.Select((position, index) => Hero(index + 1, 1, position)).ToList(),
            Random = new Random(1)
        });
    }

    private static BattleCombatant Hero(long id, int team, int position) => new()
    {
        Id = id, SourceHeroId = id, Team = team, Position = position, Name = $"Hero {id}",
        MaxHp = 1000, Hp = 1000, Atk = 100, Def = 0, Spd = 100,
        MagicDamage = 100, MagicResistance = 0,
        BasicSkill = new BattleSkill
        {
            Id = "BASIC", Name = "Basic", SkillTypeCode = BattleCodes.Normal, Effects = []
        }
    };
}
