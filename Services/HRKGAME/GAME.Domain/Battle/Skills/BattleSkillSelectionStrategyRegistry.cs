using GAME.Domain.Battle.Skills.ThanhThaiAura;

namespace GAME.Domain.Battle.Skills;

public sealed class BattleSkillSelectionStrategyRegistry
{
    private readonly List<IBattleSkillSelectionStrategy> _strategies;

    public BattleSkillSelectionStrategyRegistry(IEnumerable<IBattleSkillSelectionStrategy> strategies)
    {
        _strategies = strategies.ToList();
    }

    public BattleSkill SelectSkill(BattleCombatant actor, IReadOnlyList<BattleCombatant> combatants)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(actor));
        if (strategy != null)
            return strategy.SelectSkill(actor, combatants);

        return !actor.StatusEffects.Any(x => x.RemainingTurns > 0 && x.EffectTypeCode.Equals(BattleCodes.Silence, StringComparison.OrdinalIgnoreCase)) &&
               actor.EnergySkill != null && actor.Energy >= actor.EnergySkill.EnergyCost
            ? actor.EnergySkill
            : actor.BasicSkill;
    }

    public static BattleSkillSelectionStrategyRegistry CreateDefault() =>
        new([new ThanhThaiAuraSkillSelectionStrategy()]);
}
