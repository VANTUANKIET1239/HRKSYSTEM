using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Skills.QaKyTinh;
using GAME.Domain.Battle.Skills.HaiLastSmile;
using GAME.Domain.Battle.Skills.ChuanMen;
using GAME.Domain.Battle.Skills.ThanhThaiAura;
using GAME.Domain.Battle.Skills.NghiaPhucPrime;
using GAME.Domain.Battle.Skills.SibaThienThan;
using GAME.Domain.Battle.Reactions;

namespace GAME.Domain.Battle;

public interface IBattleSimulationEngine
{
    bool CanHandleEffect(string effectTypeCode);
    BattleSimulationResult Simulate(BattleSimulationRequest request);
}

public sealed class BattleSimulationEngine : IBattleSimulationEngine
{
    private readonly BattleEffectHandlerRegistry _effectHandlers;
    private readonly SkillHandlerRegistry _skillHandlers;
    private readonly BattleCombatantReactionRegistry _combatantReactions;
    private readonly BattleSkillSelectionStrategyRegistry _skillSelectionStrategies;

    public BattleSimulationEngine()
    {
        _effectHandlers = BattleEffectHandlerRegistry.CreateDefault();
        var targetSelectors = BattleTargetSelectorRegistry.CreateDefault();
        var defaultHandler = new DefaultSkillHandler(_effectHandlers, targetSelectors);
        _skillHandlers = new SkillHandlerRegistry(
            [
                new FatalAllInSkillHandler(defaultHandler),
                new HaiLastSmileSkillHandler(defaultHandler, _effectHandlers, targetSelectors),
                ChuanMenSkillHandler.Create(defaultHandler, _effectHandlers, targetSelectors),
                ThanhThaiAuraSkillHandler.Create(defaultHandler, _effectHandlers, targetSelectors),
                new NghiaPhucPrimeSkillHandler(defaultHandler, _effectHandlers, targetSelectors),
                SibaThienThanSkillHandler.Create(defaultHandler, _effectHandlers, targetSelectors)
            ],
            defaultHandler);
        _combatantReactions = BattleCombatantReactionRegistry.CreateDefault();
        _skillSelectionStrategies = BattleSkillSelectionStrategyRegistry.CreateDefault();
    }

    public BattleSimulationEngine(
        BattleEffectHandlerRegistry effectHandlers,
        BattleTargetSelectorRegistry targetSelectors)
    {
        _effectHandlers = effectHandlers;
        var defaultHandler = new DefaultSkillHandler(_effectHandlers, targetSelectors);
        _skillHandlers = new SkillHandlerRegistry(
            [
                new FatalAllInSkillHandler(defaultHandler),
                new HaiLastSmileSkillHandler(defaultHandler, _effectHandlers, targetSelectors),
                ChuanMenSkillHandler.Create(defaultHandler, _effectHandlers, targetSelectors),
                ThanhThaiAuraSkillHandler.Create(defaultHandler, _effectHandlers, targetSelectors),
                new NghiaPhucPrimeSkillHandler(defaultHandler, _effectHandlers, targetSelectors),
                SibaThienThanSkillHandler.Create(defaultHandler, _effectHandlers, targetSelectors)
            ],
            defaultHandler);
        _combatantReactions = BattleCombatantReactionRegistry.CreateDefault();
        _skillSelectionStrategies = BattleSkillSelectionStrategyRegistry.CreateDefault();
    }

    public BattleSimulationEngine(
        BattleEffectHandlerRegistry effectHandlers,
        SkillHandlerRegistry skillHandlers,
        BattleTargetSelectorRegistry targetSelectors)
        : this(effectHandlers, skillHandlers, targetSelectors, null, null)
    {
    }

    public BattleSimulationEngine(
        BattleEffectHandlerRegistry effectHandlers,
        SkillHandlerRegistry skillHandlers,
        BattleTargetSelectorRegistry targetSelectors,
        BattleCombatantReactionRegistry? combatantReactions,
        BattleSkillSelectionStrategyRegistry? skillSelectionStrategies)
    {
        _effectHandlers = effectHandlers;
        _skillHandlers = skillHandlers;
        _combatantReactions = combatantReactions ?? BattleCombatantReactionRegistry.CreateDefault();
        _skillSelectionStrategies = skillSelectionStrategies ?? BattleSkillSelectionStrategyRegistry.CreateDefault();
    }

    public bool CanHandleEffect(string effectTypeCode) => _effectHandlers.CanHandle(effectTypeCode);

    public BattleSimulationResult Simulate(BattleSimulationRequest request)
    {
        Validate(request);
        var random = new Random(request.RandomSeed);
        var heroes = request.Combatants.Select(Clone).ToList();
        var events = new List<BattleEvent>();
        var sequence = 0;
        var turn = 0;
        var completedRounds = 0;

        Add("BATTLE_START", 0, 0);

        for (var round = 1; round <= request.MaxRounds && HasBothTeamsAlive(heroes); round++)
        {
            completedRounds = round;
            Add("ROUND_START", round, turn);
            var queue = heroes.Where(x => x.IsAlive)
                .OrderByDescending(GetEffectiveSpeed)
                .ThenBy(x => x.Position)
                .ThenBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            foreach (var actorId in queue)
            {
                var actor = heroes.Single(x => x.Id == actorId);
                if (!actor.IsAlive || !HasBothTeamsAlive(heroes)) continue;

                turn++;
                Add("TURN_START", round, turn, actor.Id);
                TickTurnStartStatuses(actor, round, turn);
                if (!actor.IsAlive || !HasBothTeamsAlive(heroes))
                {
                    Add("TURN_END", round, turn, actor.Id);
                    continue;
                }
                if (HasStatus(actor, BattleCodes.Stun))
                {
                    Add("TURN_SKIPPED", round, turn, actor.Id, actor.Id, effectCode: BattleCodes.Stun);
                    TickStatuses(actor, round, turn);
                    Add("TURN_END", round, turn, actor.Id);
                    continue;
                }
                var skill = _skillSelectionStrategies.SelectSkill(actor, heroes);
                Add("SKILL_CAST", round, turn, actor.Id, skillId: skill.Id,
                    castSequence: turn, timelineOffsetMs: 0, phaseCode: "CAST");

                // Pay an energy skill's cost before executing its handler. Conditional
                // effects such as ENERGY_CHANGE can then restore energy without being
                // lost to the MaxEnergy clamp before the skill cost is deducted.
                if (skill.SkillTypeCode == BattleCodes.Energy)
                {
                    var energyBeforeCast = actor.Energy;
                    actor.Energy = Math.Max(0, actor.Energy - skill.EnergyCost);
                    Add(BattleCodes.EnergyChanged, round, turn, actor.Id, actor.Id, skill.Id,
                        effectCode: BattleCodes.EnergyChange,
                        value: actor.Energy - energyBeforeCast,
                        energyBefore: energyBeforeCast, energyAfter: actor.Energy,
                        castSequence: turn, timelineOffsetMs: 0, phaseCode: "CAST");
                }

                var actionId = $"turn_{turn}_actor_{actor.Id}";
                var execution = _skillHandlers.Resolve(skill).Execute(new SkillExecutionContext
                {
                    Skill = skill, Actor = actor, Combatants = heroes, Random = random,
                    Round = round, Turn = turn, ActionId = actionId
                });

                // Coordinate targeted reactions generically when action targets are determined
                foreach (var targetId in execution.TargetedCombatantIds.Distinct())
                {
                    var targetedHero = heroes.FirstOrDefault(x => x.Id == targetId && x.IsAlive && x.Team != actor.Team);
                    if (targetedHero != null)
                    {
                        var targetCtx = new BattleTargetedReactionContext
                        {
                            Actor = actor,
                            Target = targetedHero,
                            Skill = skill,
                            ActionId = actionId,
                            Round = round,
                            Turn = turn,
                            Combatants = heroes,
                            Random = random,
                            TimelineOffsetMs = 0,
                            PhaseCode = "CAST"
                        };
                        foreach (var reactionHandler in _combatantReactions.Handlers)
                        {
                            var revts = reactionHandler.OnTargeted(targetedHero, targetCtx);
                            foreach (var revt in revts)
                            {
                                Add(revt.EventType, round, turn, revt.ActorId, revt.TargetId, revt.SkillId,
                                    revt.EffectTypeCode, revt.DamageSchoolCode, revt.Value, revt.HpBefore, revt.HpAfter,
                                    energyBefore: revt.EnergyBefore, energyAfter: revt.EnergyAfter,
                                    isCrit: revt.IsCrit, remainingTurns: revt.RemainingTurns,
                                    previousStacks: revt.PreviousStacks, currentStacks: revt.CurrentStacks, maxStacks: revt.MaxStacks,
                                    castSequence: turn, timelineOffsetMs: revt.TimelineOffsetMs ?? 0, phaseCode: revt.PhaseCode ?? "CAST",
                                    statModifiers: revt.StatModifiers, executionGroup: revt.ExecutionGroup, hitIndex: revt.HitIndex,
                                    resourceCode: revt.ResourceCode, previousValue: revt.PreviousValue, currentValue: revt.CurrentValue,
                                    reasonCode: revt.ReasonCode, actionId: revt.ActionId ?? actionId,
                                    statusInstanceId: revt.StatusInstanceId);
                            }
                        }
                    }
                }

                foreach (var emitted in execution.Events)
                {
                    var timing = GetEventTiming(skill, emitted.EventType);
                    var offsetMs = emitted.TimelineOffsetMs ?? timing.OffsetMs;
                    var phaseCode = emitted.PhaseCode ?? timing.PhaseCode;
                    Add(emitted.EventType, round, turn, emitted.ActorId, emitted.TargetId,
                        emitted.SkillId, emitted.EffectTypeCode, emitted.DamageSchoolCode,
                        emitted.Value, emitted.HpBefore, emitted.HpAfter,
                        energyBefore: emitted.EnergyBefore, energyAfter: emitted.EnergyAfter,
                        isCrit: emitted.IsCrit, remainingTurns: emitted.RemainingTurns,
                        previousStacks: emitted.PreviousStacks, currentStacks: emitted.CurrentStacks, maxStacks: emitted.MaxStacks,
                        castSequence: turn, timelineOffsetMs: offsetMs, phaseCode: phaseCode,
                        statModifiers: emitted.StatModifiers,
                        executionGroup: emitted.ExecutionGroup, hitIndex: emitted.HitIndex,
                        resourceCode: emitted.ResourceCode, previousValue: emitted.PreviousValue, currentValue: emitted.CurrentValue,
                        reasonCode: emitted.ReasonCode, actionId: emitted.ActionId ?? actionId,
                        sourceHeroId: emitted.SourceHeroId, originalDamage: emitted.OriginalDamage,
                        redirectRequested: emitted.RedirectRequested, redirectActual: emitted.RedirectActual,
                        allyDamageAfterRedirect: emitted.AllyDamageAfterRedirect,
                        guardianHpBefore: emitted.GuardianHpBefore, guardianHpAfter: emitted.GuardianHpAfter,
                        statusInstanceId: emitted.StatusInstanceId);
                }

                // Coordinate defeated combatant reactions generically after damage and death resolution
                foreach (var defId in execution.DefeatedTargetIds.Distinct())
                {
                    var defHero = heroes.FirstOrDefault(x => x.Id == defId);
                    if (defHero != null)
                    {
                        foreach (var observer in heroes.Where(x => x.IsAlive && x.Id != defId))
                        {
                            var defCtx = new BattleCombatantDefeatedReactionContext
                            {
                                DefeatedCombatant = defHero,
                                Killer = actor,
                                Skill = skill,
                                ActionId = actionId,
                                Round = round,
                                Turn = turn,
                                Combatants = heroes,
                                Random = random,
                                TimelineOffsetMs = skill.Animation.TotalDurationMs,
                                PhaseCode = "RECOVERY"
                            };
                            foreach (var reactionHandler in _combatantReactions.Handlers)
                            {
                                var devts = reactionHandler.OnCombatantDefeated(observer, defCtx);
                                foreach (var devt in devts)
                                {
                                    Add(devt.EventType, round, turn, devt.ActorId, devt.TargetId, devt.SkillId,
                                        devt.EffectTypeCode, devt.DamageSchoolCode, devt.Value, devt.HpBefore, devt.HpAfter,
                                        energyBefore: devt.EnergyBefore, energyAfter: devt.EnergyAfter,
                                        isCrit: devt.IsCrit, remainingTurns: devt.RemainingTurns,
                                        previousStacks: devt.PreviousStacks, currentStacks: devt.CurrentStacks, maxStacks: devt.MaxStacks,
                                        castSequence: turn, timelineOffsetMs: devt.TimelineOffsetMs ?? skill.Animation.TotalDurationMs,
                                        phaseCode: devt.PhaseCode ?? "RECOVERY",
                                        statModifiers: devt.StatModifiers, executionGroup: devt.ExecutionGroup, hitIndex: devt.HitIndex,
                                        resourceCode: devt.ResourceCode, previousValue: devt.PreviousValue, currentValue: devt.CurrentValue,
                                        reasonCode: devt.ReasonCode, actionId: devt.ActionId ?? actionId,
                                        statusInstanceId: devt.StatusInstanceId);
                                }
                            }
                        }
                    }
                }

                // Coordinate post-action reactions generically
                var actionCompletedCtx = new BattleActionCompletedReactionContext
                {
                    Actor = actor,
                    Skill = skill,
                    ExecutionResult = execution,
                    ActionId = actionId,
                    Round = round,
                    Turn = turn,
                    Combatants = heroes,
                    Random = random,
                    TimelineOffsetMs = skill.Animation.TotalDurationMs,
                    PhaseCode = "RECOVERY"
                };
                foreach (var reactionHandler in _combatantReactions.Handlers)
                {
                    var postEvts = reactionHandler.OnActionCompleted(actor, actionCompletedCtx);
                    foreach (var pevt in postEvts)
                    {
                        Add(pevt.EventType, round, turn, pevt.ActorId, pevt.TargetId, pevt.SkillId,
                            pevt.EffectTypeCode, pevt.DamageSchoolCode, pevt.Value, pevt.HpBefore, pevt.HpAfter,
                            energyBefore: pevt.EnergyBefore, energyAfter: pevt.EnergyAfter,
                            isCrit: pevt.IsCrit, remainingTurns: pevt.RemainingTurns,
                            previousStacks: pevt.PreviousStacks, currentStacks: pevt.CurrentStacks, maxStacks: pevt.MaxStacks,
                            castSequence: turn, timelineOffsetMs: pevt.TimelineOffsetMs ?? skill.Animation.TotalDurationMs,
                            phaseCode: pevt.PhaseCode ?? "RECOVERY",
                            statModifiers: pevt.StatModifiers, executionGroup: pevt.ExecutionGroup, hitIndex: pevt.HitIndex,
                            resourceCode: pevt.ResourceCode, previousValue: pevt.PreviousValue, currentValue: pevt.CurrentValue,
                            reasonCode: pevt.ReasonCode, actionId: pevt.ActionId ?? actionId,
                            statusInstanceId: pevt.StatusInstanceId);
                    }
                }

                if (request.BasicAttackHitEnergyGain > 0)
                {
                    var impactTiming = GetEventTiming(skill, "DAMAGE");
                    foreach (var hitTarget in heroes.Where(x => execution.BasicAttackHitTargetIds.Contains(x.Id) && x.IsAlive))
                    {
                        var targetEnergyBefore = hitTarget.Energy;
                        hitTarget.Energy = Math.Min(hitTarget.MaxEnergy,
                            hitTarget.Energy + request.BasicAttackHitEnergyGain);
                        if (hitTarget.Energy == targetEnergyBefore) continue;
                        Add("ENERGY_CHANGED", round, turn, actor.Id, hitTarget.Id, skill.Id,
                            value: hitTarget.Energy - targetEnergyBefore,
                            energyBefore: targetEnergyBefore, energyAfter: hitTarget.Energy,
                            castSequence: turn, timelineOffsetMs: impactTiming.OffsetMs,
                            phaseCode: impactTiming.PhaseCode);
                    }
                }

                if (skill.SkillTypeCode == BattleCodes.Normal)
                {
                    var energyBeforeGain = actor.Energy;
                    actor.Energy = Math.Min(actor.MaxEnergy, actor.Energy + request.BasicAttackEnergyGain);
                    Add(BattleCodes.EnergyChanged, round, turn, actor.Id, actor.Id, skill.Id,
                        effectCode: BattleCodes.EnergyChange,
                        value: actor.Energy - energyBeforeGain,
                        energyBefore: energyBeforeGain, energyAfter: actor.Energy,
                        castSequence: turn, timelineOffsetMs: skill.Animation.TotalDurationMs,
                        phaseCode: "RECOVERY");
                }

                TickStatuses(actor, round, turn);
                Add("SKILL_COMPLETED", round, turn, actor.Id, skillId: skill.Id,
                    castSequence: turn, timelineOffsetMs: skill.Animation.TotalDurationMs, phaseCode: "RECOVERY");
                Add("TURN_END", round, turn, actor.Id, castSequence: turn,
                    timelineOffsetMs: skill.Animation.TotalDurationMs, phaseCode: "RECOVERY");
            }
        }

        var winner = GetWinner(heroes);
        Add("BATTLE_END", completedRounds, turn, value: winner == "LEFT" ? 0 : winner == "RIGHT" ? 1 : -1);
        return new BattleSimulationResult { Winner = winner, Rounds = completedRounds, Events = events, FinalCombatants = heroes };

        void TickTurnStartStatuses(BattleCombatant actor, int round, int currentTurn)
        {
            foreach (var status in actor.StatusEffects.ToList())
            {
                if (status.AppliedTurn >= currentTurn) continue;
                var turnStartHandler = _effectHandlers.GetTurnStartHandler(status.EffectTypeCode);
                if (turnStartHandler != null)
                {
                    var turnEvents = turnStartHandler.OnTurnStart(status, actor, round, currentTurn, heroes);
                    foreach (var te in turnEvents)
                    {
                        Add(te.EventType, round, currentTurn, te.ActorId, te.TargetId,
                            te.SkillId, te.EffectTypeCode, te.DamageSchoolCode,
                            te.Value, te.HpBefore, te.HpAfter, isCrit: te.IsCrit,
                            remainingTurns: te.RemainingTurns, castSequence: currentTurn,
                            timelineOffsetMs: 0, phaseCode: "IMPACT",
                            statModifiers: te.StatModifiers,
                            statusInstanceId: te.StatusInstanceId);
                    }

                    status.RemainingTurns--;
                    if (status.RemainingTurns > 0)
                    {
                        Add("STATUS_UPDATED", round, currentTurn, status.SourceHeroId, actor.Id,
                            status.SourceSkillId, status.EffectTypeCode, remainingTurns: status.RemainingTurns,
                            statusInstanceId: status.InstanceId);
                    }
                    else
                    {
                        actor.StatusEffects.Remove(status);
                        Add("STATUS_EXPIRED", round, currentTurn, status.SourceHeroId, actor.Id,
                            status.SourceSkillId, status.EffectTypeCode, statusInstanceId: status.InstanceId);
                    }
                }
            }
        }

        void TickStatuses(BattleCombatant actor, int round, int currentTurn)
        {
            foreach (var status in actor.StatusEffects.ToList())
            {
                if (status.AppliedTurn >= currentTurn) continue;
                if (_effectHandlers.GetTurnStartHandler(status.EffectTypeCode) != null) continue;
                if (status.RemainingTurns < 0) continue; // Permanent status does not expire

                status.RemainingTurns--;
                if (status.RemainingTurns > 0)
                {
                    Add("STATUS_UPDATED", round, currentTurn, status.SourceHeroId, actor.Id,
                        status.SourceSkillId, status.EffectTypeCode, remainingTurns: status.RemainingTurns,
                        currentStacks: status.Stacks, maxStacks: status.MaxStacks,
                        statusInstanceId: status.InstanceId);
                    continue;
                }

                var turnEndHandler = _effectHandlers.GetTurnEndHandler(status.EffectTypeCode);
                if (turnEndHandler != null)
                {
                    var turnEndEvents = turnEndHandler.OnTurnEnd(status, actor, round, currentTurn, heroes);
                    foreach (var te in turnEndEvents)
                    {
                        Add(te.EventType, round, currentTurn, te.ActorId, te.TargetId,
                            te.SkillId, te.EffectTypeCode, te.DamageSchoolCode,
                            te.Value, te.HpBefore, te.HpAfter,
                            energyBefore: te.EnergyBefore, energyAfter: te.EnergyAfter,
                            isCrit: te.IsCrit, remainingTurns: te.RemainingTurns,
                            previousStacks: te.PreviousStacks, currentStacks: te.CurrentStacks, maxStacks: te.MaxStacks,
                            castSequence: currentTurn, timelineOffsetMs: te.TimelineOffsetMs ?? 0, phaseCode: te.PhaseCode ?? "RECOVERY",
                            statModifiers: te.StatModifiers, executionGroup: te.ExecutionGroup, hitIndex: te.HitIndex,
                            resourceCode: te.ResourceCode, previousValue: te.PreviousValue, currentValue: te.CurrentValue,
                            reasonCode: te.ReasonCode, actionId: te.ActionId ?? $"turn_{currentTurn}_expire",
                            sourceHeroId: te.SourceHeroId, originalDamage: te.OriginalDamage,
                            redirectRequested: te.RedirectRequested, redirectActual: te.RedirectActual,
                            allyDamageAfterRedirect: te.AllyDamageAfterRedirect,
                            guardianHpBefore: te.GuardianHpBefore, guardianHpAfter: te.GuardianHpAfter,
                            statusInstanceId: te.StatusInstanceId);
                    }
                }

                actor.StatusEffects.Remove(status);
                Add("STATUS_EXPIRED", round, currentTurn, status.SourceHeroId, actor.Id,
                    status.SourceSkillId, status.EffectTypeCode, statusInstanceId: status.InstanceId);
            }
        }

        void Add(string type, int round, int currentTurn, long? actorId = null, long? targetId = null,
            string? skillId = null, string? effectCode = null, string? school = null, int value = 0,
            int? hpBefore = null, int? hpAfter = null, int? energyBefore = null, int? energyAfter = null,
            bool isCrit = false, int? remainingTurns = null,
            int? previousStacks = null, int? currentStacks = null, int? maxStacks = null,
            int? castSequence = null,
            int timelineOffsetMs = 0, string? phaseCode = null,
            IReadOnlyList<BattleStatModifier>? statModifiers = null,
            string? executionGroup = null, int? hitIndex = null,
            string? resourceCode = null, int? previousValue = null, int? currentValue = null,
            string? reasonCode = null, string? actionId = null,
            long? sourceHeroId = null, int? originalDamage = null,
            int? redirectRequested = null, int? redirectActual = null,
            int? allyDamageAfterRedirect = null,
            int? guardianHpBefore = null, int? guardianHpAfter = null,
            string? statusInstanceId = null) => events.Add(new BattleEvent
            {
                Sequence = ++sequence, Round = round, Turn = currentTurn, EventType = type,
                ActorId = actorId, TargetId = targetId, SkillId = skillId, EffectTypeCode = effectCode,
                DamageSchoolCode = school, Value = value, HpBefore = hpBefore, HpAfter = hpAfter,
                EnergyBefore = energyBefore, EnergyAfter = energyAfter, IsCrit = isCrit,
                RemainingTurns = remainingTurns,
                PreviousStacks = previousStacks, CurrentStacks = currentStacks, MaxStacks = maxStacks,
                CastSequence = castSequence,
                TimelineOffsetMs = timelineOffsetMs, PhaseCode = phaseCode,
                ExecutionGroup = executionGroup, HitIndex = hitIndex,
                StatModifiers = statModifiers ?? [],
                ResourceCode = resourceCode,
                PreviousValue = previousValue,
                CurrentValue = currentValue,
                ReasonCode = reasonCode,
                ActionId = actionId,
                StatusInstanceId = statusInstanceId,
                SourceHeroId = sourceHeroId,
                OriginalDamage = originalDamage,
                RedirectRequested = redirectRequested,
                RedirectActual = redirectActual,
                AllyDamageAfterRedirect = allyDamageAfterRedirect,
                GuardianHpBefore = guardianHpBefore,
                GuardianHpAfter = guardianHpAfter
            });
    }

    private static bool HasStatus(BattleCombatant hero, string effectCode) =>
        hero.StatusEffects.Any(x => x.RemainingTurns > 0 &&
            x.EffectTypeCode.Equals(effectCode, StringComparison.OrdinalIgnoreCase));

    private static (int OffsetMs, string PhaseCode) GetEventTiming(BattleSkill skill, string eventType)
    {
        var configuredPhase = skill.Animation.Phases.FirstOrDefault(x =>
            x.TriggerEventType?.Equals(eventType, StringComparison.OrdinalIgnoreCase) == true);
        if (configuredPhase != null)
            return (PhaseEventOffset(configuredPhase), configuredPhase.PhaseCode);

        var phaseCode = eventType switch
        {
            "DAMAGE" or "HEAL" or "SHIELD_ABSORBED" or "POSITION_CHANGED" or "DEATH"
            or "BLEED_DAMAGE" or "BLEED_DETONATED" => "IMPACT",
            "STATUS_APPLIED" or "SHIELD_APPLIED" or "STATUS_EXPIRED" or "STATUS_REFRESHED"
            or "ACTION_BAR_CHANGED" => "STATUS",
            _ => "IMPACT"
        };
        var phase = skill.Animation.Phase(phaseCode);
        return (PhaseEventOffset(phase), phaseCode);
    }

    // CAST begins immediately. IMPACT visuals may contain many cosmetic hits, but the
    // authoritative HP change happens once, at the end of the impact phase.
    private static int PhaseEventOffset(BattleSkillTimelinePhase phase) =>
        phase.PhaseCode.Equals("IMPACT", StringComparison.OrdinalIgnoreCase)
            ? phase.StartAtMs + phase.DurationMs
            : phase.StartAtMs;

    private static int GetEffectiveSpeed(BattleCombatant hero) =>
        Math.Max(1, (int)Math.Round(BattleStatCalculator.GetEffectiveStat(hero, "SPD")));

    private static bool HasBothTeamsAlive(IEnumerable<BattleCombatant> heroes) =>
        heroes.Any(x => x.Team == 0 && x.IsAlive) && heroes.Any(x => x.Team == 1 && x.IsAlive);

    private static string GetWinner(IEnumerable<BattleCombatant> heroes)
    {
        var left = heroes.Any(x => x.Team == 0 && x.IsAlive);
        var right = heroes.Any(x => x.Team == 1 && x.IsAlive);
        return left == right ? "DRAW" : left ? "LEFT" : "RIGHT";
    }

    private static void Validate(BattleSimulationRequest request)
    {
        if (request.Combatants.Count == 0) throw new ArgumentException("Battle must contain combatants.");
        if (!request.Combatants.Any(x => x.Team == 0) || !request.Combatants.Any(x => x.Team == 1))
            throw new ArgumentException("Battle must contain both teams.");
        if (request.MaxRounds <= 0) throw new ArgumentOutOfRangeException(nameof(request.MaxRounds));
        if (request.BasicAttackEnergyGain < 0)
            throw new ArgumentOutOfRangeException(nameof(request.BasicAttackEnergyGain));
        if (request.BasicAttackHitEnergyGain < 0)
            throw new ArgumentOutOfRangeException(nameof(request.BasicAttackHitEnergyGain));
        foreach (var hero in request.Combatants)
        {
            if (hero.BasicSkill.SkillTypeCode != BattleCodes.Normal)
                throw new ArgumentException($"Hero {hero.Id} must have exactly one NORMAL basic skill.");
            if (hero.EnergySkill != null && hero.EnergySkill.SkillTypeCode != BattleCodes.Energy)
                throw new ArgumentException($"Hero {hero.Id} energy skill must use ENERGY type.");
        }
    }

    private static BattleCombatant Clone(BattleCombatant source)
    {
        var clone = new BattleCombatant
        {
            Id = source.Id, SourceHeroId = source.SourceHeroId, Team = source.Team, Position = source.Position,
            Name = source.Name, MaxHp = source.MaxHp, Hp = source.Hp, Atk = source.Atk, Def = source.Def,
            Spd = source.Spd, MagicDamage = source.MagicDamage, MagicResistance = source.MagicResistance,
            CritChance = source.CritChance, CritDamage = source.CritDamage, Energy = source.Energy,
            MaxEnergy = source.MaxEnergy, BasicSkill = source.BasicSkill, EnergySkill = source.EnergySkill,
            Resources = new Dictionary<string, int>(source.Resources, StringComparer.OrdinalIgnoreCase),
            ProcessedActionIds = new HashSet<string>(source.ProcessedActionIds, StringComparer.OrdinalIgnoreCase),
            ProcessedDefeatedCombatantIds = new HashSet<long>(source.ProcessedDefeatedCombatantIds)
        };
        foreach (var status in source.StatusEffects)
        {
            clone.StatusEffects.Add(new BattleStatusEffect
            {
                InstanceId = status.InstanceId,
                EffectTypeCode = status.EffectTypeCode,
                SourceSkillId = status.SourceSkillId,
                SourceHeroId = status.SourceHeroId,
                RemainingTurns = status.RemainingTurns,
                AppliedTurn = status.AppliedTurn,
                Stacks = status.Stacks,
                MaxStacks = status.MaxStacks,
                Value = status.Value,
                ShieldRemaining = status.ShieldRemaining,
                LastProcessedActionId = status.LastProcessedActionId,
                DamageBonusPerStackPercent = status.DamageBonusPerStackPercent,
                OutgoingDamageBonusPerStackPercent = status.OutgoingDamageBonusPerStackPercent,
                IncomingDamageBonusPerStackPercent = status.IncomingDamageBonusPerStackPercent,
                IncomingDamageBonusRestrictedToSource = status.IncomingDamageBonusRestrictedToSource,
                ScaleModifiersWithStacks = status.ScaleModifiersWithStacks,
                StatModifiers = status.StatModifiers
            });
        }
        return clone;
    }
}
