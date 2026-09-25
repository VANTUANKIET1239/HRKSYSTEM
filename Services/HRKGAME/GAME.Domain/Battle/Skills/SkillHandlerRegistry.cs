namespace GAME.Domain.Battle.Skills;

public sealed class SkillHandlerRegistry
{
    private readonly IReadOnlyList<ISkillHandler> _customHandlers;
    private readonly DefaultSkillHandler _defaultHandler;

    public SkillHandlerRegistry(IEnumerable<ISkillHandler> customHandlers, DefaultSkillHandler defaultHandler)
    {
        _customHandlers = customHandlers.ToList();
        _defaultHandler = defaultHandler;
    }

    public ISkillHandler Resolve(BattleSkill skill) =>
        _customHandlers.FirstOrDefault(handler => handler.CanHandle(skill)) ?? _defaultHandler;
}
