using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Skills.ThanhThaiAura;

public sealed class ThanhThaiAuraResourceHandler
{
    public const int DefaultMaxAura = 100;
    public const int DefaultFullAuraThreshold = 100;
    public const decimal DefaultDamageBonusPercentPerAura = 0.25m;

    public static int GetAuraTier(int aura) => aura switch
    {
        >= 100 => 4,
        >= 75 => 3,
        >= 50 => 2,
        >= 25 => 1,
        _ => 0
    };

    public static IReadOnlyList<PendingBattleEvent> GainAura(
        BattleCombatant hero,
        int amount,
        string reasonCode,
        string? actionId,
        string? skillId = null,
        long? targetId = null,
        int maxAura = DefaultMaxAura,
        int fullAuraThreshold = DefaultFullAuraThreshold,
        int timelineOffsetMs = 0,
        string? phaseCode = null)
    {
        var prevValue = hero.GetResource(ThanhThaiAuraSkillCodes.ResourceAura, 0);
        var newValue = Math.Clamp(prevValue + amount, 0, maxAura);
        hero.SetResource(ThanhThaiAuraSkillCodes.ResourceAura, newValue);

        var events = new List<PendingBattleEvent>();

        // Always emit resource events even if delta is 0, but value represents delta
        var delta = newValue - prevValue;

        events.Add(new PendingBattleEvent
        {
            EventType = BattleCodes.AuraGained,
            ActorId = hero.Id,
            TargetId = targetId,
            SkillId = skillId,
            ResourceCode = ThanhThaiAuraSkillCodes.ResourceAura,
            Value = amount,
            PreviousValue = prevValue,
            CurrentValue = newValue,
            ReasonCode = reasonCode,
            ActionId = actionId,
            TimelineOffsetMs = timelineOffsetMs,
            PhaseCode = phaseCode,
            StatModifiers = BuildDamageBonusModifiers(newValue)
        });

        events.Add(new PendingBattleEvent
        {
            EventType = BattleCodes.ResourceChanged,
            ActorId = hero.Id,
            TargetId = targetId,
            SkillId = skillId,
            ResourceCode = ThanhThaiAuraSkillCodes.ResourceAura,
            Value = delta,
            PreviousValue = prevValue,
            CurrentValue = newValue,
            ReasonCode = reasonCode,
            ActionId = actionId,
            TimelineOffsetMs = timelineOffsetMs,
            PhaseCode = phaseCode,
            StatModifiers = BuildDamageBonusModifiers(newValue)
        });

        // Check Full Aura transition
        if (newValue >= fullAuraThreshold && prevValue < fullAuraThreshold)
        {
            ApplyFullAuraFarming(hero, skillId ?? ThanhThaiAuraSkillCodes.Basic);
            events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.FullAuraActivated,
                ActorId = hero.Id,
                TargetId = targetId,
                SkillId = skillId,
                EffectTypeCode = ThanhThaiAuraSkillCodes.StatusFullAura,
                ResourceCode = ThanhThaiAuraSkillCodes.ResourceAura,
                Value = newValue,
                PreviousValue = prevValue,
                CurrentValue = newValue,
                ReasonCode = reasonCode,
                ActionId = actionId,
                TimelineOffsetMs = timelineOffsetMs,
                PhaseCode = phaseCode
            });
        }

        return events;
    }

    public static IReadOnlyList<PendingBattleEvent> ConsumeAura(
        BattleCombatant hero,
        int amount,
        string reasonCode,
        string? actionId,
        string? skillId = null,
        long? targetId = null,
        int maxAura = DefaultMaxAura,
        int fullAuraThreshold = DefaultFullAuraThreshold,
        int timelineOffsetMs = 0,
        string? phaseCode = null)
    {
        var prevValue = hero.GetResource(ThanhThaiAuraSkillCodes.ResourceAura, 0);
        var newValue = Math.Clamp(prevValue - amount, 0, maxAura);
        hero.SetResource(ThanhThaiAuraSkillCodes.ResourceAura, newValue);

        var events = new List<PendingBattleEvent>();

        events.Add(new PendingBattleEvent
        {
            EventType = BattleCodes.AuraConsumed,
            ActorId = hero.Id,
            TargetId = targetId,
            SkillId = skillId,
            ResourceCode = ThanhThaiAuraSkillCodes.ResourceAura,
            Value = amount,
            PreviousValue = prevValue,
            CurrentValue = newValue,
            ReasonCode = reasonCode,
            ActionId = actionId,
            TimelineOffsetMs = timelineOffsetMs,
            PhaseCode = phaseCode,
            StatModifiers = BuildDamageBonusModifiers(newValue)
        });

        events.Add(new PendingBattleEvent
        {
            EventType = BattleCodes.ResourceChanged,
            ActorId = hero.Id,
            TargetId = targetId,
            SkillId = skillId,
            ResourceCode = ThanhThaiAuraSkillCodes.ResourceAura,
            Value = -amount,
            PreviousValue = prevValue,
            CurrentValue = newValue,
            ReasonCode = reasonCode,
            ActionId = actionId,
            TimelineOffsetMs = timelineOffsetMs,
            PhaseCode = phaseCode,
            StatModifiers = BuildDamageBonusModifiers(newValue)
        });

        if (prevValue >= fullAuraThreshold && newValue < fullAuraThreshold)
        {
            RemoveFullAuraFarming(hero);
            events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.FullAuraRemoved,
                ActorId = hero.Id,
                TargetId = targetId,
                SkillId = skillId,
                EffectTypeCode = ThanhThaiAuraSkillCodes.StatusFullAura,
                ResourceCode = ThanhThaiAuraSkillCodes.ResourceAura,
                Value = newValue,
                PreviousValue = prevValue,
                CurrentValue = newValue,
                ReasonCode = reasonCode,
                ActionId = actionId,
                TimelineOffsetMs = timelineOffsetMs,
                PhaseCode = phaseCode
            });
        }

        return events;
    }

    public static void ApplyFullAuraFarming(BattleCombatant hero, string sourceSkillId)
    {
        var existing = hero.StatusEffects.FirstOrDefault(x =>
            x.EffectTypeCode.Equals(ThanhThaiAuraSkillCodes.StatusFullAura, StringComparison.OrdinalIgnoreCase));
        if (existing != null) return;

        hero.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = Guid.NewGuid().ToString("N"),
            EffectTypeCode = ThanhThaiAuraSkillCodes.StatusFullAura,
            SourceSkillId = sourceSkillId,
            SourceHeroId = hero.Id,
            RemainingTurns = -1, // Permanent until removed
            Stacks = 1,
            MaxStacks = 1,
            StatModifiers = []
        });
    }

    public static void RemoveFullAuraFarming(BattleCombatant hero)
    {
        hero.StatusEffects.RemoveAll(x =>
            x.EffectTypeCode.Equals(ThanhThaiAuraSkillCodes.StatusFullAura, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<BattleStatModifier> BuildDamageBonusModifiers(int aura) =>
    [
        new("ATK", "PERCENT", aura * DefaultDamageBonusPercentPerAura, "Sát thương vật lý"),
        new("MAGIC_DAMAGE", "PERCENT", aura * DefaultDamageBonusPercentPerAura, "Sát thương phép")
    ];
}
