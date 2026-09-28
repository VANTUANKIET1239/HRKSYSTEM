namespace GAME.Domain.Battle.Skills.ThanhThaiAura;

public sealed class ThanhThaiAuraSkillSelectionStrategy : IBattleSkillSelectionStrategy
{
    public bool CanHandle(BattleCombatant actor) =>
        actor.BasicSkill.Id.Equals(ThanhThaiAuraSkillCodes.Basic, StringComparison.OrdinalIgnoreCase) ||
        (actor.EnergySkill != null && actor.EnergySkill.Id.Equals(ThanhThaiAuraSkillCodes.Ultimate, StringComparison.OrdinalIgnoreCase));

    public BattleSkill SelectSkill(BattleCombatant actor, IReadOnlyList<BattleCombatant> combatants)
    {
        if (actor.StatusEffects.Any(x => x.RemainingTurns > 0 && x.EffectTypeCode.Equals(BattleCodes.Silence, StringComparison.OrdinalIgnoreCase)))
            return actor.BasicSkill;

        if (actor.EnergySkill == null || actor.Energy < actor.EnergySkill.EnergyCost)
            return actor.BasicSkill;

        var currentAura = actor.GetResource(ThanhThaiAuraSkillCodes.ResourceAura, 0);
        var hasFullAura = actor.StatusEffects.Any(x =>
            (x.RemainingTurns > 0 || x.RemainingTurns == -1) &&
            x.EffectTypeCode.Equals(ThanhThaiAuraSkillCodes.StatusFullAura, StringComparison.OrdinalIgnoreCase));

        // Auto battle rule: If Aura == 100, has FULL_AURA_FARMING, and NO enemy has LOSS_OF_CONFIDENCE,
        // prioritize basic attack at least once before ultimate.
        if (currentAura >= 100 && hasFullAura)
        {
            var enemies = combatants.Where(x => x.Team != actor.Team && x.IsAlive);
            var anyEnemyHasLossOfConfidence = enemies.Any(e => e.StatusEffects.Any(s =>
                (s.RemainingTurns > 0 || s.RemainingTurns == -1) &&
                s.EffectTypeCode.Equals(ThanhThaiAuraSkillCodes.StatusLossOfConfidence, StringComparison.OrdinalIgnoreCase)));

            if (!anyEnemyHasLossOfConfidence)
            {
                return actor.BasicSkill;
            }
        }

        return actor.EnergySkill;
    }
}
