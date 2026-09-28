using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle.Skills;

public sealed class DefaultSkillHandler : ISkillHandler
{
    private readonly BattleEffectHandlerRegistry _effectHandlers;
    private readonly BattleTargetSelectorRegistry _targetSelectors;

    public DefaultSkillHandler(BattleEffectHandlerRegistry effectHandlers, BattleTargetSelectorRegistry targetSelectors)
    {
        _effectHandlers = effectHandlers;
        _targetSelectors = targetSelectors;
    }

    public bool CanHandle(BattleSkill skill) => true;

    public SkillExecutionResult Execute(SkillExecutionContext context) => Execute(context, _ => true);

    public SkillExecutionResult Execute(SkillExecutionContext context, Func<BattleSkillEffect, bool> effectFilter)
    {
        var result = new SkillExecutionResult();
        var selectedTargetIdsByType = new Dictionary<string, IReadOnlyList<long>>(StringComparer.OrdinalIgnoreCase);

        foreach (var effect in context.Skill.Effects.Where(effectFilter))
        {
            if (!selectedTargetIdsByType.TryGetValue(effect.TargetTypeCode, out var selectedTargetIds))
            {
                selectedTargetIds = ResolveTargets(effect.TargetTypeCode, context, effect)
                    .Select(x => x.Id).ToList();
                selectedTargetIdsByType[effect.TargetTypeCode] = selectedTargetIds;
                foreach (var tid in selectedTargetIds)
                {
                    result.TargetedCombatantIds.Add(tid);
                }
            }

            var livingTargets = selectedTargetIds
                .Select(id => context.Combatants.FirstOrDefault(x => x.Id == id))
                .Where(x => x?.IsAlive == true).Cast<BattleCombatant>().ToList();
            var handler = _effectHandlers.GetRequired(effect.EffectTypeCode);
            var targetsToApply = handler.ApplyOncePerEffect ? livingTargets.Take(1) : livingTargets;

            var rollOnce = effect.GetBool("ROLL_ONCE_PER_EFFECT", false);
            if (rollOnce && effect.ChancePercent < 100m && (decimal)context.Random.NextDouble() * 100m > effect.ChancePercent)
            {
                continue;
            }

            foreach (var target in targetsToApply)
            {
                if (!rollOnce && effect.ChancePercent < 100m && (decimal)context.Random.NextDouble() * 100m > effect.ChancePercent)
                {
                    continue;
                }

                var wasAlive = target.IsAlive;
                var emittedEvents = handler.Apply(new BattleEffectContext
                {
                    Effect = effect, Skill = context.Skill, Actor = context.Actor, Target = target,
                    SelectedTargets = livingTargets, Combatants = context.Combatants,
                    Random = context.Random, Round = context.Round, Turn = context.Turn
                });
                result.Events.AddRange(emittedEvents);
                if (context.Skill.SkillTypeCode.Equals(BattleCodes.Normal, StringComparison.OrdinalIgnoreCase) &&
                    effect.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase) &&
                    emittedEvents.Any(x => x.EventType == "DAMAGE"))
                    result.BasicAttackHitTargetIds.Add(target.Id);
                if (effect.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase) &&
                    wasAlive && !target.IsAlive)
                    result.DefeatedTargetIds.Add(target.Id);
            }
            if (!context.Actor.IsAlive) break;
        }
        return result;
    }

    public IReadOnlyList<PendingBattleEvent> ApplyToActor(SkillExecutionContext context, BattleSkillEffect effect)
    {
        var handler = _effectHandlers.GetRequired(effect.EffectTypeCode);
        return handler.Apply(new BattleEffectContext
        {
            Effect = effect, Skill = context.Skill, Actor = context.Actor, Target = context.Actor,
            SelectedTargets = [context.Actor], Combatants = context.Combatants,
            Random = context.Random, Round = context.Round, Turn = context.Turn
        });
    }

    public IReadOnlyList<BattleCombatant> ResolveTargets(
        string targetCode, SkillExecutionContext context, BattleSkillEffect? effect = null)
    {
        if (IsTauntRedirectableTarget(targetCode, effect))
        {
            var taunt = context.Actor.StatusEffects
                .Where(x => x.RemainingTurns > 0 && x.EffectTypeCode.Equals(BattleCodes.Taunt, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.AppliedTurn).FirstOrDefault();
            if (taunt != null)
            {
                var taunter = context.Combatants.FirstOrDefault(x => x.Id == taunt.SourceHeroId && x.Team != context.Actor.Team && x.IsAlive);
                if (taunter != null) return [taunter];
            }
        }
        return _targetSelectors.GetRequired(targetCode).Select(new BattleTargetContext
        {
            Actor = context.Actor,
            Allies = context.Combatants.Where(x => x.Team == context.Actor.Team && x.IsAlive).OrderBy(x => x.Position).ThenBy(x => x.Id).ToList(),
            Enemies = context.Combatants.Where(x => x.Team != context.Actor.Team && x.IsAlive).OrderBy(x => x.Position).ThenBy(x => x.Id).ToList(),
            Random = context.Random,
            Effect = effect
        });
    }

    private static bool IsTauntRedirectableTarget(string targetCode, BattleSkillEffect? effect = null)
    {
        if (effect != null && (
            effect.EffectTypeCode.Equals(BattleCodes.Heal, StringComparison.OrdinalIgnoreCase) ||
            effect.EffectTypeCode.Equals(BattleCodes.Shield, StringComparison.OrdinalIgnoreCase) ||
            effect.EffectTypeCode.Equals(BattleCodes.StatBuff, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return targetCode.Equals(BattleCodes.EnemySingle, StringComparison.OrdinalIgnoreCase) ||
               targetCode.Equals(BattleCodes.EnemyRandom, StringComparison.OrdinalIgnoreCase) ||
               targetCode.Equals(BattleCodes.EnemySameLaneBackRow, StringComparison.OrdinalIgnoreCase) ||
               targetCode.Equals(BattleCodes.LowestHpPercent, StringComparison.OrdinalIgnoreCase);
    }
}
