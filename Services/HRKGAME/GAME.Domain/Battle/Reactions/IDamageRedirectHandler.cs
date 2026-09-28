using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Skills.NghiaPhucPrime;

namespace GAME.Domain.Battle.Reactions;

public sealed class DamageRedirectContext
{
    public required BattleCombatant Actor { get; init; }
    public required BattleCombatant Target { get; init; }
    public required BattleSkill Skill { get; init; }
    public required BattleSkillEffect Effect { get; init; }
    public int IncomingDamage { get; init; }
    public string DamageSchoolCode { get; init; } = BattleCodes.Physical;
    public required string ActionId { get; init; }
    public int Round { get; init; }
    public int Turn { get; init; }
    public required IReadOnlyList<BattleCombatant> Combatants { get; init; }
    public required Random Random { get; init; }
    public int TimelineOffsetMs { get; init; }
    public string? PhaseCode { get; init; }
    public bool IsRedirect { get; init; }
}

public sealed class DamageRedirectResult
{
    public int TargetDamage { get; init; }
    public int RedirectedDamage { get; init; }
    public IReadOnlyList<PendingBattleEvent> EmittedEvents { get; init; } = [];
}

public interface IDamageRedirectHandler
{
    DamageRedirectResult? HandleRedirect(DamageRedirectContext context);
}

public sealed class DamageRedirectHandlerRegistry
{
    private readonly List<IDamageRedirectHandler> _handlers;

    public DamageRedirectHandlerRegistry(IEnumerable<IDamageRedirectHandler> handlers)
    {
        _handlers = handlers.ToList();
    }

    public IReadOnlyList<IDamageRedirectHandler> Handlers => _handlers;

    public static DamageRedirectHandlerRegistry CreateDefault() => new([
        new NghiaPhucPrimeReactionHandler()
    ]);
}
