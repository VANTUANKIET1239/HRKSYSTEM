using GAME.Domain.Battle;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class RowTargetSelectorTests
{
    [Fact]
    public void Front_row_target_falls_back_to_all_living_back_row_enemies()
    {
        var selected = Select(new EnemyFrontRowTargetSelector(), actorPosition: 1, 2, 4);

        Assert.Equal([2, 4], selected.Select(x => x.Position));
    }

    [Fact]
    public void Back_row_target_falls_back_to_all_living_front_row_enemies()
    {
        var selected = Select(new EnemyBackRowTargetSelector(), actorPosition: 1, 1, 3, 5);

        Assert.Equal([1, 3, 5], selected.Select(x => x.Position));
    }

    [Fact]
    public void Same_lane_back_target_uses_other_back_slot_then_front_row()
    {
        var selector = new EnemySameLaneBackRowTargetSelector();

        Assert.Equal(4, Select(selector, actorPosition: 1, 1, 3, 4).Single().Position);
        Assert.Equal(1, Select(selector, actorPosition: 1, 1, 3, 5).Single().Position);
    }

    private static IReadOnlyList<BattleCombatant> Select(
        IBattleTargetSelector selector, int actorPosition, params int[] enemyPositions)
    {
        var actor = Hero(100, 0, actorPosition);
        return selector.Select(new BattleTargetContext
        {
            Actor = actor,
            Allies = [actor],
            Enemies = enemyPositions.Select((position, index) => Hero(index + 1, 1, position)).ToList(),
            Random = new Random(1)
        });
    }

    private static BattleCombatant Hero(long id, int team, int position) => new()
    {
        Id = id,
        SourceHeroId = id,
        Team = team,
        Position = position,
        Name = $"Hero {id}",
        MaxHp = 1000,
        Hp = 1000,
        Atk = 100,
        Def = 0,
        Spd = 100,
        MagicDamage = 100,
        MagicResistance = 0,
        BasicSkill = new BattleSkill
        {
            Id = "BASIC",
            Name = "Basic",
            SkillTypeCode = BattleCodes.Normal,
            Effects = []
        }
    };
}
