using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Reactions;

namespace GAME.Domain.Battle.Skills.ThanhThaiAura;

public sealed class ThanhThaiAuraReactionHandler : IBattleCombatantReactionHandler
{
    public IReadOnlyList<PendingBattleEvent> OnTargeted(BattleCombatant target, BattleTargetedReactionContext context)
    {
        // 1. Target must be alive
        if (!target.IsAlive) return [];

        // 2. Target must be Thanh Thai Aura (has Aura resource or Thanh Thai skill)
        if (!IsThanhThaiAura(target)) return [];

        // 3. Actor must be an enemy (not ally, not self)
        if (context.Actor == null || context.Actor.Team == target.Team || context.Actor.Id == target.Id)
            return [];

        // 4. Multi-hit action / AoE guard: exactly once per ActionId
        var actionKey = $"TARGETED_{context.ActionId}";
        if (!string.IsNullOrEmpty(context.ActionId) && target.ProcessedActionIds.Contains(actionKey))
        {
            return [];
        }
        target.ProcessedActionIds.Add(actionKey);

        // 5. Read parameter from skill effect or fallback 5
        var auraGain = GetConfiguredParam(target, "AURA_GAIN_TARGETED", 5);

        return ThanhThaiAuraResourceHandler.GainAura(
            target,
            auraGain,
            ThanhThaiAuraSkillCodes.ReasonTargetedByEnemy,
            context.ActionId,
            context.Skill.Id,
            target.Id,
            timelineOffsetMs: context.TimelineOffsetMs,
            phaseCode: context.PhaseCode ?? "CAST");
    }

    public IReadOnlyList<PendingBattleEvent> OnCombatantDefeated(BattleCombatant observer, BattleCombatantDefeatedReactionContext context)
    {
        // 1. Observer must be alive
        if (!observer.IsAlive) return [];

        // 2. Observer must be Thanh Thai Aura
        if (!IsThanhThaiAura(observer)) return [];

        // 3. Exactly once per defeated combatant
        if (observer.ProcessedDefeatedCombatantIds.Contains(context.DefeatedCombatant.Id))
        {
            return [];
        }
        observer.ProcessedDefeatedCombatantIds.Add(context.DefeatedCombatant.Id);

        // 4. Read parameter from skill effect or fallback 15
        var auraGain = GetConfiguredParam(observer, "AURA_GAIN_DEFEAT", 15);

        return ThanhThaiAuraResourceHandler.GainAura(
            observer,
            auraGain,
            ThanhThaiAuraSkillCodes.ReasonCombatantDefeated,
            context.ActionId,
            context.Skill.Id,
            context.DefeatedCombatant.Id,
            timelineOffsetMs: context.TimelineOffsetMs,
            phaseCode: context.PhaseCode ?? "RECOVERY");
    }

    private static bool IsThanhThaiAura(BattleCombatant hero)
    {
        return hero.Resources.ContainsKey(ThanhThaiAuraSkillCodes.ResourceAura) ||
               hero.BasicSkill.Id.Equals(ThanhThaiAuraSkillCodes.Basic, StringComparison.OrdinalIgnoreCase) ||
               (hero.EnergySkill != null && hero.EnergySkill.Id.Equals(ThanhThaiAuraSkillCodes.Ultimate, StringComparison.OrdinalIgnoreCase));
    }

    private static int GetConfiguredParam(BattleCombatant hero, string paramCode, int fallback)
    {
        // Try reading from basic skill effects
        foreach (var effect in hero.BasicSkill.Effects)
        {
            if (effect.Parameters.TryGetValue(paramCode, out var p) && p.IntValue.HasValue)
                return p.IntValue.Value;
        }

        // Try reading from energy skill effects if available
        if (hero.EnergySkill != null)
        {
            foreach (var effect in hero.EnergySkill.Effects)
            {
                if (effect.Parameters.TryGetValue(paramCode, out var p) && p.IntValue.HasValue)
                    return p.IntValue.Value;
            }
        }

        return fallback;
    }
}
