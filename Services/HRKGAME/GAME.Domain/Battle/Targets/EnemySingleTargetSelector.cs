namespace GAME.Domain.Battle.Targets;

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
