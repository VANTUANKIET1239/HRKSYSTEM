using System.Collections.Concurrent;
using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Reactions;

public static class CatScratchTracker
{
    private static readonly ConcurrentDictionary<string, (int NormalDmg, int DeepDmg)> DamageByActionAndActor = new();

    public static void RecordDamage(string? actionId, long actorId, int damage, bool isDeep)
    {
        if (string.IsNullOrEmpty(actionId) || damage <= 0) return;
        var key = $"{actionId}_{actorId}";
        DamageByActionAndActor.AddOrUpdate(
            key,
            isDeep ? (0, damage) : (damage, 0),
            (_, cur) => isDeep ? (cur.NormalDmg, cur.DeepDmg + damage) : (cur.NormalDmg + damage, cur.DeepDmg));
    }

    public static bool TryConsumeDamage(string? actionId, long actorId, out int normalDmg, out int deepDmg)
    {
        normalDmg = 0;
        deepDmg = 0;
        if (string.IsNullOrEmpty(actionId)) return false;
        var key = $"{actionId}_{actorId}";
        if (DamageByActionAndActor.TryRemove(key, out var val))
        {
            normalDmg = val.NormalDmg;
            deepDmg = val.DeepDmg;
            return true;
        }
        return false;
    }
}

public sealed class CatScratchReactionHandler : IBattleStatusReactionHandler
{
    private readonly string _effectTypeCode;

    public CatScratchReactionHandler(string effectTypeCode = BattleCodes.CatScratch)
    {
        _effectTypeCode = effectTypeCode;
    }

    public string EffectTypeCode => _effectTypeCode;

    public IReadOnlyList<PendingBattleEvent> OnDamaged(BattleStatusEffect status, BattleDamagedContext context)
    {
        // 1. Must be a direct damage effect (not DoT, not reflection)
        if (!context.Effect.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
            return [];
        if (context.Effect.GetBool("IS_DOT", false) || context.Effect.GetBool("IS_REFLECTION", false))
            return [];

        // 2. Attacker must be from the opposing team
        if (context.Actor == null || context.Actor.Team == context.Target.Team || context.Actor.Id == context.Target.Id)
            return [];

        // 3. Must have dealt actual HP damage
        if (context.ActualHpDamage <= 0 || !context.WasHit)
            return [];
            
        // 4. Record actual damage dealt to this marked target for the action
        var isDeep = status.EffectTypeCode.Equals(BattleCodes.DeepCatScratch, StringComparison.OrdinalIgnoreCase);
        CatScratchTracker.RecordDamage(context.ActionId, context.Actor.Id, context.ActualHpDamage, isDeep);

        return [];
    }
}
