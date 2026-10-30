using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Skills.NghiaPhucPrime;

namespace GAME.Domain.Battle.Reactions;

public sealed class DamageRedirectResult
{
    public int TargetDamage { get; init; }
    public int RedirectedDamage { get; init; }
    public IReadOnlyList<PendingBattleEvent> EmittedEvents { get; init; } = [];
}
