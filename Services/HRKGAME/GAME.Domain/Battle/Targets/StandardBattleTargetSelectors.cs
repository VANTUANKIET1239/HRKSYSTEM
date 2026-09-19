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
    public override string TargetTypeCode => BattleCodes.EnemySingle;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) =>
        context.Enemies.Take(1).ToList();
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
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) =>
        context.Enemies.Where(x => x.Position is 1 or 3 or 5).ToList();
}

public sealed class EnemyBackRowTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyBackRow;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) =>
        context.Enemies.Where(x => x.Position is 2 or 4).ToList();
}

public sealed class EnemySameLaneBackRowTargetSelector : BattleTargetSelectorBase
{
    private static readonly IReadOnlyDictionary<int, int[]> BackRowByActorPosition =
        new Dictionary<int, int[]>
        {
            [1] = [2], [2] = [2], [3] = [2, 4], [4] = [4], [5] = [4]
        };

    public override string TargetTypeCode => BattleCodes.EnemySameLaneBackRow;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) =>
        context.Enemies.Where(x => GetBackRowPositions(context.Actor.Position).Contains(x.Position)).Take(1).ToList();

    private static IReadOnlyList<int> GetBackRowPositions(int actorPosition) =>
        BackRowByActorPosition.TryGetValue(actorPosition, out var positions) ? positions : [2, 4];
}
