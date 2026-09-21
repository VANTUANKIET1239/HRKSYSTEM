namespace GAME.Domain.Battle.Targets;

public sealed class SelfTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.Self;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) => [context.Actor];
}

public sealed class AllyAllTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.AllyAll;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) => context.Allies;
}

public sealed class AllyRandomTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.AllyRandom;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) =>
        RandomTargets(context.Allies, 1, context.Random);
}

public sealed class AllyRandom2TargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.AllyRandom2;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) =>
        RandomTargets(context.Allies, 2, context.Random);
}

public sealed class EnemySingleTargetSelector : BattleTargetSelectorBase
{
    private static readonly IReadOnlyDictionary<int, int[]> TargetPriorityByActorPosition =
        new Dictionary<int, int[]>
        {
            // Left lane: opposite front, remaining front row, then left/right back row.
            [1] = [1, 3, 5, 2, 4],
            [2] = [1, 3, 5, 2, 4],
            // Center lane can fall back to either back-row position.
            [3] = [3, 1, 5, 2, 4],
            // Right lane mirrors the left lane.
            [4] = [5, 3, 1, 4, 2],
            [5] = [5, 3, 1, 4, 2]
        };

    public override string TargetTypeCode => BattleCodes.EnemySingle;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var priority = TargetPriorityByActorPosition.TryGetValue(context.Actor.Position, out var positions)
            ? positions
            : TargetPriorityByActorPosition[3];

        var target = context.Enemies
            .OrderBy(enemy => Array.IndexOf(priority, enemy.Position) is var index && index >= 0
                ? index
                : int.MaxValue)
            .ThenBy(enemy => enemy.Position)
            .ThenBy(enemy => enemy.Id)
            .FirstOrDefault();

        return target == null ? [] : [target];
    }
}

public sealed class EnemyAllTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyAll;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) => context.Enemies;
}

public sealed class EnemyRandomTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyRandom;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) =>
        RandomTargets(context.Enemies, 1, context.Random);
}

public sealed class EnemyRandom4TargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyRandom4;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) =>
        RandomTargets(context.Enemies, 4, context.Random);
}

public sealed class EnemyFrontRowTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyFrontRow;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var frontRow = context.Enemies.Where(x => x.Position is 1 or 3 or 5).ToList();
        return frontRow.Count > 0
            ? frontRow
            : context.Enemies.Where(x => x.Position is 2 or 4).ToList();
    }
}

public sealed class EnemyBackRowTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyBackRow;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var backRow = context.Enemies.Where(x => x.Position is 2 or 4).ToList();
        return backRow.Count > 0
            ? backRow
            : context.Enemies.Where(x => x.Position is 1 or 3 or 5).ToList();
    }
}

public sealed class EnemySameLaneBackRowTargetSelector : BattleTargetSelectorBase
{
    private static readonly IReadOnlyDictionary<int, int[]> TargetPriorityByActorPosition =
        new Dictionary<int, int[]>
        {
            [1] = [2, 4, 1, 3, 5],
            [2] = [2, 4, 1, 3, 5],
            [3] = [2, 4, 3, 1, 5],
            [4] = [4, 2, 5, 3, 1],
            [5] = [4, 2, 5, 3, 1]
        };

    public override string TargetTypeCode => BattleCodes.EnemySameLaneBackRow;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var priority = TargetPriorityByActorPosition.TryGetValue(context.Actor.Position, out var positions)
            ? positions
            : TargetPriorityByActorPosition[3];
        var target = context.Enemies
            .OrderBy(enemy => Array.IndexOf(priority, enemy.Position) is var index && index >= 0
                ? index
                : int.MaxValue)
            .ThenBy(enemy => enemy.Position)
            .ThenBy(enemy => enemy.Id)
            .FirstOrDefault();

        return target == null ? [] : [target];
    }
}
