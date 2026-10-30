namespace GAME.Domain.Battle.Targets;

public interface IBattleTargetSelector
{
    string TargetTypeCode { get; }
    IReadOnlyList<BattleCombatant> Select(BattleTargetContext context);
}
