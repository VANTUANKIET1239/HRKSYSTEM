using GAME.Domain.Battle.Reactions;

namespace GAME.Domain.Battle.Effects;

public sealed class DamageEffectExecutionResult
{
    public int ActualDamage { get; init; }
    public int ShieldAbsorbed { get; init; }
    public bool WasHit { get; init; }
    public bool WasCrit { get; init; }
    public bool WasKilled { get; init; }
    public IReadOnlyList<PendingBattleEvent> EmittedEvents { get; init; } = [];
}
