namespace GAME.Domain.Battle;

public sealed class BattleSimulationResult
{
    public required string Winner { get; init; }
    public int Rounds { get; init; }
    public required IReadOnlyList<BattleEvent> Events { get; init; }
    public required IReadOnlyList<BattleCombatant> FinalCombatants { get; init; }
}
