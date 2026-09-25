using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Skills.QaKyTinh;
using GAME.Domain.Battle.Skills.HaiLastSmile;
using GAME.Domain.Battle.Skills.ChuanMen;

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

    public BattleSimulationEngine()
    {
        _effectHandlers = BattleEffectHandlerRegistry.CreateDefault();
        var targetSelectors = BattleTargetSelectorRegistry.CreateDefault();
        var defaultHandler = new DefaultSkillHandler(_effectHandlers, targetSelectors);
        _skillHandlers = new SkillHandlerRegistry(
            [
                new FatalAllInSkillHandler(defaultHandler),
                new HaiLastSmileSkillHandler(defaultHandler, _effectHandlers, targetSelectors),
                ChuanMenSkillHandler.Create(defaultHandler, _effectHandlers, targetSelectors)
            ],
            defaultHandler);
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
                ChuanMenSkillHandler.Create(defaultHandler, _effectHandlers, targetSelectors)
            ],
            defaultHandler);
    }

    public BattleSimulationEngine(
        BattleEffectHandlerRegistry effectHandlers,
        SkillHandlerRegistry skillHandlers,
        BattleTargetSelectorRegistry targetSelectors)
    {
        _effectHandlers = effectHandlers;
        _skillHandlers = skillHandlers;
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
                var skill = SelectSkill(actor);
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
                        executionGroup: emitted.ExecutionGroup, hitIndex: emitted.HitIndex);
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
                            statModifiers: te.StatModifiers);
                    }

                    status.RemainingTurns--;
                    if (status.RemainingTurns > 0)
                    {
                        Add("STATUS_UPDATED", round, currentTurn, status.SourceHeroId, actor.Id,
                            status.SourceSkillId, status.EffectTypeCode, remainingTurns: status.RemainingTurns);
                    }
                    else
                    {
                        actor.StatusEffects.Remove(status);
                        Add("STATUS_EXPIRED", round, currentTurn, status.SourceHeroId, actor.Id,
                            status.SourceSkillId, status.EffectTypeCode);
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
                        currentStacks: status.Stacks, maxStacks: status.MaxStacks);
                    continue;
                }
                actor.StatusEffects.Remove(status);
                Add("STATUS_EXPIRED", round, currentTurn, status.SourceHeroId, actor.Id,
                    status.SourceSkillId, status.EffectTypeCode);
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
            string? executionGroup = null, int? hitIndex = null) => events.Add(new BattleEvent
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
                StatModifiers = statModifiers ?? []
            });
    }

    private static BattleSkill SelectSkill(BattleCombatant actor) =>
        !HasStatus(actor, BattleCodes.Silence) && actor.EnergySkill != null && actor.Energy >= actor.EnergySkill.EnergyCost
            ? actor.EnergySkill
            : actor.BasicSkill;

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
            MaxEnergy = source.MaxEnergy, BasicSkill = source.BasicSkill, EnergySkill = source.EnergySkill
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
                ScaleModifiersWithStacks = status.ScaleModifiersWithStacks,
                StatModifiers = status.StatModifiers
            });
        }
        return clone;
    }
}
