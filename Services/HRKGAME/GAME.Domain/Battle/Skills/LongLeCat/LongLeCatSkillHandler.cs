using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle.Skills.LongLeCat;

public sealed class LongLeCatSkillHandler : ISkillHandler
{
    private readonly DefaultSkillHandler _defaultHandler;
    private readonly DamageEffectHandler _damageHandler;

    public LongLeCatSkillHandler(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers)
    {
        _defaultHandler = defaultHandler;
        _damageHandler = (effectHandlers.GetRequired(BattleCodes.Damage) as DamageEffectHandler) ?? new DamageEffectHandler();
    }

    public static LongLeCatSkillHandler Create(DefaultSkillHandler defaultHandler) =>
        new(defaultHandler, BattleEffectHandlerRegistry.CreateDefault());

    public bool CanHandle(BattleSkill skill) =>
        skill.Id.Equals(LongLeCatSkillCodes.Basic, StringComparison.OrdinalIgnoreCase) ||
        skill.Id.Equals(LongLeCatSkillCodes.Energy, StringComparison.OrdinalIgnoreCase);

    public SkillExecutionResult Execute(SkillExecutionContext context) =>
        context.Skill.Id.Equals(LongLeCatSkillCodes.Basic, StringComparison.OrdinalIgnoreCase)
            ? ExecuteBasic(context)
            : ExecuteEnergy(context);

    private SkillExecutionResult ExecuteBasic(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();
        var effects = context.Skill.Effects.OrderBy(x => x.DisplayOrder).ToList();
        var primaryEffect = effects.FirstOrDefault(x => x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no Damage effect configured.");

        var target = _defaultHandler.ResolveTargets(primaryEffect.TargetTypeCode, context, primaryEffect).FirstOrDefault();
        if (target == null) return result;

        result.TargetedCombatantIds.Add(target.Id);

        var dmgContext = new BattleEffectContext
        {
            Effect = primaryEffect,
            Skill = context.Skill,
            Actor = context.Actor,
            Target = target,
            SelectedTargets = [target],
            Combatants = context.Combatants,
            Random = context.Random,
            Round = context.Round,
            Turn = context.Turn,
            ActionId = context.ActionId,
            TimelineOffsetMs = 800,
            PhaseCode = "IMPACT"
        };

        var dmgResult = _damageHandler.ExecuteDamage(dmgContext);
        foreach (var evt in dmgResult.EmittedEvents)
        {
            result.Events.Add(evt);
        }

        if (dmgResult.WasHit)
        {
            result.BasicAttackHitTargetIds.Add(target.Id);
        }
        if (dmgResult.WasKilled)
        {
            result.DefeatedTargetIds.Add(target.Id);
            return result;
        }

        // Apply yellow Cat Scratch (Vết Cào)
        // If Deep Cat Scratch exists, do not downgrade
        var hasDeepScratch = target.StatusEffects.Any(s =>
            s.EffectTypeCode.Equals(BattleCodes.DeepCatScratch, StringComparison.OrdinalIgnoreCase));

        if (!hasDeepScratch)
        {
            var existingNormal = target.StatusEffects.FirstOrDefault(s =>
                s.EffectTypeCode.Equals(BattleCodes.CatScratch, StringComparison.OrdinalIgnoreCase));

            if (existingNormal != null)
            {
                existingNormal.RemainingTurns = 2;
                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRefreshed,
                    ActorId = context.Actor.Id,
                    TargetId = target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.CatScratch,
                    RemainingTurns = 2,
                    StatusInstanceId = existingNormal.InstanceId,
                    TimelineOffsetMs = 900,
                    PhaseCode = "IMPACT"
                });
            }
            else
            {
                var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{BattleCodes.CatScratch}:{target.Id}";
                target.StatusEffects.Add(new BattleStatusEffect
                {
                    InstanceId = instanceId,
                    EffectTypeCode = BattleCodes.CatScratch,
                    SourceSkillId = context.Skill.Id,
                    SourceHeroId = context.Actor.Id,
                    RemainingTurns = 2,
                    AppliedTurn = context.Turn,
                    Stacks = 1,
                    MaxStacks = 1,
                    Value = 10m,
                    IncomingDamageBonusPerStackPercent = 10m,
                    IncomingDamageBonusRestrictedToSource = false,
                    Dispellable = true
                });

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.CatScratch,
                    RemainingTurns = 2,
                    Value = 10,
                    StatusInstanceId = instanceId,
                    TimelineOffsetMs = 900,
                    PhaseCode = "IMPACT"
                });
            }
        }

        return result;
    }

    private SkillExecutionResult ExecuteEnergy(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();
        var primaryEffect = context.Skill.Effects.FirstOrDefault()
            ?? throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no effects configured.");

        // Target: up to 3 random eligible living allies (exclude Long Le, exclude heroes with basic HEAL, exclude heroes with cat already)
        var targetCode = primaryEffect.TargetTypeCode ?? BattleCodes.RandomEligibleAlliesN;
        var chosenAllies = _defaultHandler.ResolveTargets(targetCode, context, primaryEffect);

        foreach (var ally in chosenAllies)
        {
            result.TargetedCombatantIds.Add(ally.Id);

            var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{BattleCodes.CatCompanion}:{ally.Id}";
            var existing = ally.StatusEffects.FirstOrDefault(s =>
                s.EffectTypeCode.Equals(BattleCodes.CatCompanion, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.RemainingTurns = 2;
                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRefreshed,
                    ActorId = context.Actor.Id,
                    TargetId = ally.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.CatCompanion,
                    RemainingTurns = 2,
                    StatusInstanceId = existing.InstanceId,
                    TimelineOffsetMs = 600,
                    PhaseCode = "CAST"
                });
            }
            else
            {
                ally.StatusEffects.Add(new BattleStatusEffect
                {
                    InstanceId = instanceId,
                    EffectTypeCode = BattleCodes.CatCompanion,
                    SourceSkillId = context.Skill.Id,
                    SourceHeroId = context.Actor.Id,
                    RemainingTurns = 2,
                    AppliedTurn = context.Turn,
                    Stacks = 1,
                    MaxStacks = 1,
                    Dispellable = false
                });

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = ally.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.CatCompanion,
                    RemainingTurns = 2,
                    StatusInstanceId = instanceId,
                    TimelineOffsetMs = 600,
                    PhaseCode = "CAST"
                });
            }
        }

        return result;
    }
}
