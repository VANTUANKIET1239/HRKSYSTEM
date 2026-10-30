namespace GAME.Domain.Battle.Targets;

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
