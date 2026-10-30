using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Skills;

public interface ISkillHandler
{
    bool CanHandle(BattleSkill skill);
    SkillExecutionResult Execute(SkillExecutionContext context);
}
