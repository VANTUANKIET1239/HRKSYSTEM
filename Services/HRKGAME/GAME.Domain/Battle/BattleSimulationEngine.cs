using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle;

public interface IBattleSimulationEngine
{
    BattleSimulationResult Simulate(BattleSimulationRequest request);
}

public sealed class BattleSimulationEngine : IBattleSimulationEngine
{
    private const int BasicEnergyGain = 25;
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
                var skill = SelectSkill(actor);
                Add("SKILL_CAST", round, turn, actor.Id, skillId: skill.Id);

                foreach (var effect in skill.Effects)
                {
                    if ((decimal)random.NextDouble() * 100m > effect.ChancePercent) continue;
                    var targets = ResolveTargets(effect.TargetTypeCode, actor, heroes, random);
                    foreach (var target in targets.Where(x => x.IsAlive))
                    {
                        var handler = _effectHandlers.GetRequired(effect.EffectTypeCode);
                        var emittedEvents = handler.Apply(new BattleEffectContext
                        {
                            Effect = effect, Skill = skill, Actor = actor, Target = target,
                            Random = random, Round = round, Turn = turn
                        });
                        foreach (var emitted in emittedEvents)
                            Add(emitted.EventType, round, turn, emitted.ActorId, emitted.TargetId,
                                emitted.SkillId, emitted.EffectTypeCode, emitted.DamageSchoolCode,
                                emitted.Value, emitted.HpBefore, emitted.HpAfter,
                                isCrit: emitted.IsCrit, remainingTurns: emitted.RemainingTurns);
                    }
                }

                var energyBefore = actor.Energy;
                actor.Energy = skill.SkillTypeCode == BattleCodes.Energy
                    ? Math.Max(0, actor.Energy - skill.EnergyCost)
                    : Math.Min(actor.MaxEnergy, actor.Energy + BasicEnergyGain);
                Add("ENERGY_CHANGED", round, turn, actor.Id, actor.Id, skill.Id,
                    value: actor.Energy - energyBefore, energyBefore: energyBefore, energyAfter: actor.Energy);

                TickStatuses(actor, round, turn);
                Add("TURN_END", round, turn, actor.Id);
            }
        }

        var winner = GetWinner(heroes);
        Add("BATTLE_END", completedRounds, turn, value: winner == "LEFT" ? 0 : winner == "RIGHT" ? 1 : -1);
        return new BattleSimulationResult { Winner = winner, Rounds = completedRounds, Events = events, FinalCombatants = heroes };

        void TickStatuses(BattleCombatant actor, int round, int currentTurn)
        {
            foreach (var status in actor.StatusEffects.ToList())
            {
                status.RemainingTurns--;
                if (status.RemainingTurns > 0) continue;
                actor.StatusEffects.Remove(status);
                Add("STATUS_EXPIRED", round, currentTurn, status.SourceHeroId, actor.Id,
                    status.SourceSkillId, status.EffectTypeCode);
            }
        }

        void Add(string type, int round, int currentTurn, long? actorId = null, long? targetId = null,
            string? skillId = null, string? effectCode = null, string? school = null, int value = 0,
            int? hpBefore = null, int? hpAfter = null, int? energyBefore = null, int? energyAfter = null,
            bool isCrit = false, int? remainingTurns = null) => events.Add(new BattleEvent
            {
                Sequence = ++sequence, Round = round, Turn = currentTurn, EventType = type,
                ActorId = actorId, TargetId = targetId, SkillId = skillId, EffectTypeCode = effectCode,
                DamageSchoolCode = school, Value = value, HpBefore = hpBefore, HpAfter = hpAfter,
                EnergyBefore = energyBefore, EnergyAfter = energyAfter, IsCrit = isCrit,
                RemainingTurns = remainingTurns
            });
    }

    private static BattleSkill SelectSkill(BattleCombatant actor) =>
        actor.EnergySkill != null && actor.Energy >= actor.EnergySkill.EnergyCost
            ? actor.EnergySkill
            : actor.BasicSkill;

    private IReadOnlyList<BattleCombatant> ResolveTargets(string targetCode, BattleCombatant actor,
        List<BattleCombatant> heroes, Random random)
    {
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
