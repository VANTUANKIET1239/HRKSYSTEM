using System.Collections.Concurrent;
using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Reactions;

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
