using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle;

public interface IBattleSimulationEngine
{
    bool CanHandleEffect(string effectTypeCode);
    BattleSimulationResult Simulate(BattleSimulationRequest request);
}

public sealed class BattleSimulationEngine : IBattleSimulationEngine
{
    private readonly BattleEffectHandlerRegistry _effectHandlers;
    private readonly BattleTargetSelectorRegistry _targetSelectors;

    public BattleSimulationEngine() : this(
        BattleEffectHandlerRegistry.CreateDefault(),
        BattleTargetSelectorRegistry.CreateDefault()) { }

    public BattleSimulationEngine(
        BattleEffectHandlerRegistry effectHandlers,
        BattleTargetSelectorRegistry targetSelectors)
    {
        _effectHandlers = effectHandlers;
        _targetSelectors = targetSelectors;
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
                if (HasStatus(actor, BattleCodes.Stun))
                {
                    Add("TURN_SKIPPED", round, turn, actor.Id, actor.Id, effectCode: BattleCodes.Stun);
                    TickStatuses(actor, round, turn);
                    Add("TURN_END", round, turn, actor.Id);
                    continue;
                }
                var skill = SelectSkill(actor);
                var basicAttackHitTargets = new HashSet<long>();
                var selectedTargetIdsByType = new Dictionary<string, IReadOnlyList<long>>(
                    StringComparer.OrdinalIgnoreCase);
                Add("SKILL_CAST", round, turn, actor.Id, skillId: skill.Id,
                    castSequence: turn, timelineOffsetMs: 0, phaseCode: "CAST");

                foreach (var effect in skill.Effects)
                {
                    if ((decimal)random.NextDouble() * 100m > effect.ChancePercent) continue;
                    if (!selectedTargetIdsByType.TryGetValue(effect.TargetTypeCode, out var selectedTargetIds))
                    {
                        selectedTargetIds = ResolveTargets(effect.TargetTypeCode, actor, heroes, random)
                            .Select(x => x.Id)
                            .ToList();
                        selectedTargetIdsByType[effect.TargetTypeCode] = selectedTargetIds;
                    }
                    var targets = selectedTargetIds
                        .Select(id => heroes.FirstOrDefault(x => x.Id == id))
                        .Where(x => x != null)
                        .Cast<BattleCombatant>()
                        .ToList();
                    var livingTargets = targets.Where(x => x.IsAlive).ToList();
                    var handler = _effectHandlers.GetRequired(effect.EffectTypeCode);
                    var targetsToApply = handler.ApplyOncePerEffect ? livingTargets.Take(1) : livingTargets;
                    foreach (var target in targetsToApply)
                    {
                        var emittedEvents = handler.Apply(new BattleEffectContext
                        {
                            Effect = effect, Skill = skill, Actor = actor, Target = target,
                            SelectedTargets = livingTargets, Combatants = heroes,
                            Random = random, Round = round, Turn = turn
                        });
                        foreach (var emitted in emittedEvents)
                        {
                            if (skill.SkillTypeCode.Equals(BattleCodes.Normal, StringComparison.OrdinalIgnoreCase) &&
                                effect.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase) &&
                                emitted.EventType == "DAMAGE")
                                basicAttackHitTargets.Add(target.Id);
                            var timing = GetEventTiming(skill, emitted.EventType);
                            Add(emitted.EventType, round, turn, emitted.ActorId, emitted.TargetId,
                                emitted.SkillId, emitted.EffectTypeCode, emitted.DamageSchoolCode,
                                emitted.Value, emitted.HpBefore, emitted.HpAfter,
                                isCrit: emitted.IsCrit, remainingTurns: emitted.RemainingTurns,
                                castSequence: turn, timelineOffsetMs: timing.OffsetMs, phaseCode: timing.PhaseCode);
                        }
                    }
                    if (!actor.IsAlive) break;
                }

                if (request.BasicAttackHitEnergyGain > 0)
                {
                    var impactTiming = GetEventTiming(skill, "DAMAGE");
                    foreach (var hitTarget in heroes.Where(x => basicAttackHitTargets.Contains(x.Id) && x.IsAlive))
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

                var energyBefore = actor.Energy;
                actor.Energy = skill.SkillTypeCode == BattleCodes.Energy
                    ? Math.Max(0, actor.Energy - skill.EnergyCost)
                    : Math.Min(actor.MaxEnergy, actor.Energy + request.BasicAttackEnergyGain);
                Add("ENERGY_CHANGED", round, turn, actor.Id, actor.Id, skill.Id,
                    value: actor.Energy - energyBefore, energyBefore: energyBefore, energyAfter: actor.Energy,
                    castSequence: turn, timelineOffsetMs: skill.Animation.TotalDurationMs, phaseCode: "RECOVERY");

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

        void TickStatuses(BattleCombatant actor, int round, int currentTurn)
        {
            foreach (var status in actor.StatusEffects.ToList())
            {
                if (status.AppliedTurn >= currentTurn) continue;
                status.RemainingTurns--;
                if (status.RemainingTurns > 0)
                {
                    Add("STATUS_UPDATED", round, currentTurn, status.SourceHeroId, actor.Id,
                        status.SourceSkillId, status.EffectTypeCode, remainingTurns: status.RemainingTurns);
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
            bool isCrit = false, int? remainingTurns = null, int? castSequence = null,
            int timelineOffsetMs = 0, string? phaseCode = null) => events.Add(new BattleEvent
            {
                Sequence = ++sequence, Round = round, Turn = currentTurn, EventType = type,
                ActorId = actorId, TargetId = targetId, SkillId = skillId, EffectTypeCode = effectCode,
                DamageSchoolCode = school, Value = value, HpBefore = hpBefore, HpAfter = hpAfter,
                EnergyBefore = energyBefore, EnergyAfter = energyAfter, IsCrit = isCrit,
                RemainingTurns = remainingTurns, CastSequence = castSequence,
                TimelineOffsetMs = timelineOffsetMs, PhaseCode = phaseCode
            });
    }

    private static BattleSkill SelectSkill(BattleCombatant actor) =>
        !HasStatus(actor, BattleCodes.Silence) && actor.EnergySkill != null && actor.Energy >= actor.EnergySkill.EnergyCost
            ? actor.EnergySkill
            : actor.BasicSkill;

    private IReadOnlyList<BattleCombatant> ResolveTargets(string targetCode, BattleCombatant actor,
        List<BattleCombatant> heroes, Random random)
    {
        if (IsTauntRedirectableTarget(targetCode))
        {
            var taunt = actor.StatusEffects
                .Where(x => x.RemainingTurns > 0 &&
                    x.EffectTypeCode.Equals(BattleCodes.Taunt, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.AppliedTurn)
                .FirstOrDefault();
            if (taunt != null)
            {
                var taunter = heroes.FirstOrDefault(x => x.Id == taunt.SourceHeroId &&
                    x.Team != actor.Team && x.IsAlive);
                if (taunter != null) return [taunter];
            }
        }
        var context = new BattleTargetContext
        {
            Actor = actor,
            Allies = heroes.Where(x => x.Team == actor.Team && x.IsAlive)
                .OrderBy(x => x.Position).ThenBy(x => x.Id).ToList(),
            Enemies = heroes.Where(x => x.Team != actor.Team && x.IsAlive)
                .OrderBy(x => x.Position).ThenBy(x => x.Id).ToList(),
            Random = random
        };
        return _targetSelectors.GetRequired(targetCode).Select(context);
    }

    private static bool HasStatus(BattleCombatant hero, string effectCode) =>
        hero.StatusEffects.Any(x => x.RemainingTurns > 0 &&
            x.EffectTypeCode.Equals(effectCode, StringComparison.OrdinalIgnoreCase));

    private static bool IsTauntRedirectableTarget(string targetCode) =>
        targetCode.Equals(BattleCodes.EnemySingle, StringComparison.OrdinalIgnoreCase) ||
        targetCode.Equals(BattleCodes.EnemyRandom, StringComparison.OrdinalIgnoreCase) ||
        targetCode.Equals(BattleCodes.EnemySameLaneBackRow, StringComparison.OrdinalIgnoreCase);

    private static (int OffsetMs, string PhaseCode) GetEventTiming(BattleSkill skill, string eventType)
    {
        var configuredPhase = skill.Animation.Phases.FirstOrDefault(x =>
            x.TriggerEventType?.Equals(eventType, StringComparison.OrdinalIgnoreCase) == true);
        if (configuredPhase != null)
            return (PhaseEventOffset(configuredPhase), configuredPhase.PhaseCode);

        var phaseCode = eventType switch
        {
            "DAMAGE" or "HEAL" or "SHIELD_ABSORBED" or "POSITION_CHANGED" or "DEATH" => "IMPACT",
            "STATUS_APPLIED" or "SHIELD_APPLIED" or "STATUS_EXPIRED" => "STATUS",
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

    private static BattleCombatant Clone(BattleCombatant source) => new()
    {
        Id = source.Id, SourceHeroId = source.SourceHeroId, Team = source.Team, Position = source.Position,
        Name = source.Name, MaxHp = source.MaxHp, Hp = source.Hp, Atk = source.Atk, Def = source.Def,
        Spd = source.Spd, MagicDamage = source.MagicDamage, MagicResistance = source.MagicResistance,
        CritChance = source.CritChance, CritDamage = source.CritDamage, Energy = source.Energy,
        MaxEnergy = source.MaxEnergy, BasicSkill = source.BasicSkill, EnergySkill = source.EnergySkill
    };
}
