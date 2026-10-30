namespace GAME.Domain.Battle;

public sealed class BattleSimulationRequest
{
    public decimal DefenseMitigationConstant { get; init; } = 1000m;
    public int RandomSeed { get; init; }
    public int MaxRounds { get; init; } = 100;
    public int BasicAttackEnergyGain { get; init; } = 25;
    public int BasicAttackHitEnergyGain { get; init; } = 25;
    public required IReadOnlyList<BattleCombatant> Combatants { get; init; }
}
