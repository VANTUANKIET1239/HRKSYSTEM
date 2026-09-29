namespace GAME.Domain.Battle.Targets;

public sealed class EnemySameVerticalLaneTargetSelector : BattleTargetSelectorBase
{
    private static readonly EnemySingleTargetSelector SingleSelector = new();

    public override string TargetTypeCode => BattleCodes.EnemySameVerticalLane;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var primaryTarget = SingleSelector.Select(context).FirstOrDefault();
        if (primaryTarget == null) return [];

        var lanePositions = GetLanePositions(primaryTarget.Position);

        return context.Enemies
            .Where(e => e.IsAlive && lanePositions.Contains(e.Position))
            .OrderBy(e => Array.IndexOf(lanePositions, e.Position))
            .ThenBy(e => e.Id)
            .ToList();
    }

    public static int[] GetLanePositions(int position) => position switch
    {
        1 or 2 => [1, 2],
        3 => [3],
        5 or 4 => [5, 4],
        _ => [position]
    };
}
