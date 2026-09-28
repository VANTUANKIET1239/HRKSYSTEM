namespace GAME.Domain.Battle.Effects;

public sealed class DispelDebuffEffectHandler : IBattleEffectHandler
{
    public string EffectTypeCode => BattleCodes.DispelDebuff;

    private static readonly HashSet<string> StandardDebuffCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        BattleCodes.StatDebuff,
        BattleCodes.Stun,
        BattleCodes.Silence,
        BattleCodes.Bleed,
        BattleCodes.Panic,
        BattleCodes.Taunt,
        BattleCodes.ShieldBlock,
        "BURN",
        "POISON",
        "FREEZE"
    };

    private static readonly HashSet<string> UndispellableMythicCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        BattleCodes.LossOfConfidence,
        BattleCodes.FullAuraFarming,
        BattleCodes.PrimeFortitude,
        BattleCodes.PrimeGuardian,
        BattleCodes.PrimePressure,
        BattleCodes.PrimeStagger,
        BattleCodes.PrimeBrokenMorale,
        BattleCodes.Ricardo
    };

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        var target = context.Target;
        if (!target.IsAlive) return [];

        var dispelCount = context.Effect.GetInt("DISPEL_COUNT", 1);
        var dispellableOnly = context.Effect.GetBool("DISPELLABLE_ONLY", true);
        var selectionMode = context.Effect.GetString("SELECTION_MODE", "RANDOM")?.ToUpperInvariant() ?? "RANDOM";

        var eligible = target.StatusEffects
            .Where(s => s.IsActive && !s.IsPermanent && s.RemainingTurns > 0)
            .Where(s => !dispellableOnly || s.Dispellable)
            .Where(s => IsDispellableDebuff(s))
            .ToList();

        if (eligible.Count == 0 || dispelCount <= 0)
        {
            return [];
        }

        var removedDebuffs = new List<BattleStatusEffect>();
        for (var i = 0; i < dispelCount && eligible.Count > 0; i++)
        {
            int index = selectionMode == "RANDOM" ? context.Random.Next(0, eligible.Count) : 0;
            var chosen = eligible[index];
            eligible.RemoveAt(index);
            target.StatusEffects.Remove(chosen);
            removedDebuffs.Add(chosen);
        }

        return removedDebuffs.Select(chosen => new PendingBattleEvent
        {
            EventType = BattleCodes.StatusRemoved,
            ActorId = context.Actor.Id,
            TargetId = target.Id,
            SkillId = context.Skill.Id,
            EffectTypeCode = chosen.EffectTypeCode,
            StatusInstanceId = chosen.InstanceId,
            ReasonCode = "DISPEL"
        }).ToList();
    }

    public static bool IsDispellableDebuff(BattleStatusEffect status)
    {
        if (UndispellableMythicCodes.Contains(status.EffectTypeCode)) return false;
        if (!string.IsNullOrEmpty(status.StatusGroup) && status.StatusGroup.Equals("AURA", StringComparison.OrdinalIgnoreCase)) return false;
        return StandardDebuffCodes.Contains(status.EffectTypeCode);
    }
}
