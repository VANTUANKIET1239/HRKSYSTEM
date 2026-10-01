using Core.Common.Repositories;
using GAME.Application.Common.Helpers;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Battle;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services;

// Read-only sandbox. No player repositories, reward services or SaveChanges calls.
public sealed class BattleLabService(IUnitOfWork unitOfWork, IBattleSimulationEngine engine,
    BattleLabBuildResolver? buildResolver = null) : IBattleLabService
{
    public async Task<BattleLabCatalogDto> GetCatalogAsync(CancellationToken ct)
    {
        var templates = await unitOfWork.ReadOnlyRepository<HrkHeroTemplate>().Query()
            .AsNoTracking().OrderBy(h => h.Id).ToListAsync(ct);
        var skills = await unitOfWork.ReadOnlyRepository<HrkHeroSkill>().Query()
            .Where(h => h.Skill.IsActive)
            .Include(h => h.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.EffectType)
            .Include(h => h.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.TargetType)
            .Include(h => h.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.Scalings).ThenInclude(s => s.AttributeType)
            .Include(h => h.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.StatModifiers).ThenInclude(s => s.AttributeType)
            .Include(h => h.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.Parameters)
            .Include(h => h.Skill).ThenInclude(s => s.AnimationConfig).ThenInclude(a => a!.Phases)
            .AsNoTracking().AsSplitQuery().ToListAsync(ct);
        var configs = await ConfigsAsync(ct);
        return new BattleLabCatalogDto
        {
            Equipment = await unitOfWork.ReadOnlyRepository<HrkItemTemplate>().Query().AsNoTracking()
                .Where(i => i.Category.IsEquipment).OrderBy(i => i.CategoryId).ThenBy(i => i.Id)
                .Select(i => new BattleLabItemOptionDto { Id = i.Id, Name = i.Name, Category = i.Category.Code, LevelReq = i.LevelReq }).ToListAsync(ct),
            DefenseConstant = BattleMitigationConfig.Read(configs),
            Heroes = templates.Select(t => new PlayerHeroDto
            {
                Id = t.Id,
                HeroTemplateId = t.Id,
                Name = t.Name,
                Avatar = t.Avatar,
                Level = 1,
                Position = 1,
                Stars = 0,
                Stats = new CalculatedStatsDto
                {
                    Hp = t.BaseHp,
                    Atk = t.BaseAtk,
                    Def = t.BaseDef,
                    Spd = t.BaseSpd,
                    MagicDamage = t.BaseMagicDamage,
                    MagicResistance = t.BaseMagicResistance,
                    Crit = t.BaseCrit,
                    CritDmg = t.BaseCritDmg,
                    Accuracy = t.BaseAccuracy,
                    Resistance = t.BaseResistance,
                    Lifesteal = t.BaseLifesteal
                },
                Skills = skills.Where(s => s.HeroTemplateId == t.Id).OrderBy(s => s.SkillOrder)
                    .Select(s => GameDtoMapper.MapSkillTemplate(s.Skill)).OfType<SkillTemplateDto>().ToList()
            }).ToList()
        };
    }

    public static void Validate(BattleLabRequestDto request)
    {
        if (request.Count is < 1 or > 300 || request.MaxRounds is < 1 or > 100 ||
            request.Seed < 0 || request.Seed > int.MaxValue - request.Count ||
            request.DefenseConstant is <= 0 or > 1000000)
            throw new ArgumentException("Invalid lab settings: count 1–300, rounds 1–100, positive K, nonnegative seed.");
        foreach (var team in new[] { request.Left, request.Right })
        {
            if (team == null || team.Count is < 1 or > 5 || team.Any(s => s == null) ||
                team.Select(s => s.Position).Distinct().Count() != team.Count)
                throw new ArgumentException("Each side needs 1–5 heroes with unique positions.");
            foreach (var slot in team)
            {
                if (slot.Position is < 1 or > 5) throw new ArgumentException("Invalid position.");
                if (slot.Mode == "BUILD")
                {
                    var b = slot.Build;
                    if (slot.Stats != null || b == null || b.Level is < 1 or > 1000 || b.Stars is < 1 or > 5 ||
                        b.AuraTier is < 1 or > 4 || b.RollSeed < 0 || b.Equipment == null || b.Equipment.Count > 6 ||
                        b.Equipment.Any(e => e == null || e.ItemTemplateId <= 0 || e.Enhancement is < 0 or > 15 || e.Stars is < 0 or > 5))
                        throw new ArgumentException("BUILD requires build only (no stats), valid level/stars/aura and equipment.");
                    continue;
                }
                if (slot.Mode != "CUSTOM" || slot.Build != null) throw new ArgumentException("CUSTOM accepts stats only; BUILD accepts build only.");
                var s = slot.Stats;
                if (slot.Position is < 1 or > 5 || s == null || s.Hp is < 1 or > 1000000 ||
                    s.Spd is < 1 or > 10000 || s.Atk is < 0 or > 100000 || s.Def is < 0 or > 100000 ||
                    s.MagicDamage is < 0 or > 100000 || s.MagicResistance is < 0 or > 100000 ||
                    s.Crit is < 0 or > 1000 || s.CritDmg is < 0 or > 1000)
                    throw new ArgumentException("Invalid hero stats or position in lab.");
            }
        }
    }

    public async Task<BattleLabReportDto> RunAsync(BattleLabRequestDto request, CancellationToken ct)
    {
        Validate(request);
        var catalog = await GetCatalogAsync(ct);
        var configs = await ConfigsAsync(ct);
        request.DefenseConstant ??= BattleMitigationConfig.Read(configs);
        var initial = new BattleInitialStateDto
        {
            LeftTeam = BuildTeam(request.Left, catalog.Heroes),
            RightTeam = BuildTeam(request.Right, catalog.Heroes)
        };
        var resolvedBuilds = new List<BattleLabResolvedBuildDto>();
        for (var team = 0; team < 2; team++)
        {
            var slots = team == 0 ? request.Left : request.Right;
            var heroes = team == 0 ? initial.LeftTeam : initial.RightTeam;
            for (var i = 0; i < slots.Count; i++)
                if (slots[i].Mode == "BUILD")
                    resolvedBuilds.Add(await (buildResolver ?? throw new InvalidOperationException("Build resolver unavailable."))
                        .ResolveAsync(heroes[i], slots[i].Build!, team, ct));
        }
        var maxEnergy = Config(configs, "MAX_ENERGY", 100, 1);
        var energy = Math.Clamp(Config(configs, "INITIAL_ENERGY", 0), 0, maxEnergy);
        var combatants = initial.LeftTeam.Select(h => BattleCombatantMapper.Map(h, 0, energy, maxEnergy))
            .Concat(initial.RightTeam.Select(h => BattleCombatantMapper.Map(h, 1, energy, maxEnergy))).ToList();
        configs[BattleMitigationConfig.Code] = request.DefenseConstant.Value;
        configs["MAX_ROUNDS"] = request.MaxRounds;
        configs["MAX_ENERGY"] = maxEnergy;
        configs["INITIAL_ENERGY"] = energy;
        configs["BASIC_ATTACK_ENERGY_GAIN"] = Config(configs, "BASIC_ATTACK_ENERGY_GAIN", 25);
        configs["BASIC_ATTACK_HIT_ENERGY_GAIN"] = Config(configs, "BASIC_ATTACK_HIT_ENERGY_GAIN", 25);
        var report = new BattleLabReportDto
        {
            Settings = request,
            BattleConfigs = configs,
            ResolvedSnapshot = initial,
            ResolvedBuilds = resolvedBuilds,
            EngineVersion = typeof(BattleSimulationEngine).Assembly.GetName().Version?.ToString() ?? "unknown",
            SnapshotHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new { initial, configs, request.MaxRounds })))),
            Warnings = [
                "One matchup cannot establish a hero's global balance. Compare identical seeds/builds, swap sides and test multiple opponents.",
                "AuraTier currently has no stat bonus in the shared hero calculator. StarAura is visual.",
                "No formation/faction/set bonus is added by Lab. CUSTOM stats are final; BUILD equipment is applied once.",
                "Accuracy, resistance and lifesteal are not mapped into BattleCombatant by the current shared mapper; do not infer their effectiveness from this report."
            ]
        };
        var outcomes = new HashSet<string>();
        for (var i = 0; i < request.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var seed = request.Seed + i;
            var simulation = engine.Simulate(new BattleSimulationRequest
            {
                RandomSeed = seed,
                MaxRounds = request.MaxRounds,
                Combatants = combatants,
                DefenseMitigationConstant = request.DefenseConstant.Value,
                BasicAttackEnergyGain = Config(configs, "BASIC_ATTACK_ENERGY_GAIN", 25),
                BasicAttackHitEnergyGain = Config(configs, "BASIC_ATTACK_HIT_ENERGY_GAIN", 25)
            });
            var stats = BattleStatisticsCalculator.Calculate(initial, simulation.Events);
            report.Runs.Add(new BattleLabRunDto
            {
                Heroes = stats.Select(s => new BattleLabHeroRunDto
                {
                    Statistics = new BattleHeroStatisticsDto
                    {
                        CombatantId = s.CombatantId,
                        SourceHeroId = s.SourceHeroId,
                        Team = s.Team,
                        HeroName = s.HeroName,
                        PhysicalDamageDealt = s.PhysicalDamageDealt,
                        MagicDamageDealt = s.MagicDamageDealt,
                        HealingDone = s.HealingDone,
                        PhysicalDamageTaken = s.PhysicalDamageTaken,
                        MagicDamageTaken = s.MagicDamageTaken
                    },
                    RemainingHp = simulation.Events.LastOrDefault(e => e.TargetId == s.CombatantId && e.HpAfter.HasValue &&
                        (e.EventType == "DAMAGE" || e.EventType == "HEAL" || e.EventType == "BLEED_DAMAGE" || e.EventType == "BLEED_DETONATED"))?.HpAfter
                        ?? (s.Team == 0 ? initial.LeftTeam : initial.RightTeam).Single(h => h.Id == Math.Abs(s.CombatantId)).Stats.Hp,
                    SkillCasts = simulation.Events.Count(e => e.ActorId == s.CombatantId && e.EventType == "SKILL_CAST"),
                    EnergyCasts = simulation.Events.Count(e => e.ActorId == s.CombatantId && e.EventType == "SKILL_CAST" &&
                        (s.Team == 0 ? initial.LeftTeam : initial.RightTeam).Single(h => h.Id == Math.Abs(s.CombatantId))
                            .Skills.Any(k => k.Id == e.SkillId && k.SkillTypeCode == "ENERGY"))
                }).ToList(),
                Seed = seed,
                Winner = simulation.Winner,
                Rounds = simulation.Events.Select(e => e.Round).DefaultIfEmpty().Max()
            });
            foreach (var group in simulation.Events.Where(e => e.EventType == "DAMAGE" && e.ActorId.HasValue)
                .GroupBy(e => (Actor: e.ActorId!.Value, Skill: e.SkillId ?? "", School: e.DamageSchoolCode ?? "")))
            {
                var metric = report.Skills.SingleOrDefault(m => m.ActorId == group.Key.Actor && m.SkillId == group.Key.Skill && m.School == group.Key.School);
                if (metric == null) { metric = new() { ActorId = group.Key.Actor, SkillId = group.Key.Skill, School = group.Key.School }; report.Skills.Add(metric); }
                metric.Hits += group.Count(); metric.HpDamage += group.Sum(e => (long)e.Value); metric.CritHits += group.Count(e => e.IsCrit);
            }
            foreach (var stat in stats)
            {
                var total = report.Totals.SingleOrDefault(s => s.CombatantId == stat.CombatantId);
                if (total == null) { report.Totals.Add(stat); continue; }
                total.PhysicalDamageDealt += stat.PhysicalDamageDealt;
                total.MagicDamageDealt += stat.MagicDamageDealt;
                total.HealingDone += stat.HealingDone;
                total.PhysicalDamageTaken += stat.PhysicalDamageTaken;
                total.MagicDamageTaken += stat.MagicDamageTaken;
            }
            // Keep one full replay per outcome, not hundreds of multi-MB timelines.
            if (outcomes.Add(simulation.Winner))
            {
                var state = new BattleInitialStateDto { LeftTeam = initial.LeftTeam, RightTeam = initial.RightTeam };
                report.Replays.Add(BattleResultMapper.Map(Guid.NewGuid().ToString("N"), state,
                    simulation, seed, BattleStatisticsCalculator.Calculate(initial, simulation.Events)));
            }
        }
        return report;
    }

    private static List<PlayerHeroDto> BuildTeam(List<BattleLabSlotDto> slots, List<PlayerHeroDto> catalog) =>
        slots.Select(slot =>
        {
            var template = catalog.SingleOrDefault(h => h.HeroTemplateId == slot.HeroTemplateId)
                ?? throw new ArgumentException($"Unknown hero template {slot.HeroTemplateId}.");
            return new PlayerHeroDto
            {
                Id = slot.Position,
                HeroTemplateId = template.HeroTemplateId,
                Name = template.Name,
                Avatar = template.Avatar,
                Position = slot.Position,
                Stats = slot.Stats ?? new(),
                Skills = template.Skills,
                Level = 1
            };
        }).ToList();

    private Task<Dictionary<string, decimal>> ConfigsAsync(CancellationToken ct) =>
        unitOfWork.ReadOnlyRepository<HrkBattleConfig>().Query().Where(c => c.IsEnabled)
            .ToDictionaryAsync(c => c.Code, c => c.Value, StringComparer.OrdinalIgnoreCase, ct);

    private static int Config(IReadOnlyDictionary<string, decimal> configs, string code, int fallback, int min = 0) =>
        Math.Max(min, configs.TryGetValue(code, out var value) ? decimal.ToInt32(value) : fallback);
}
