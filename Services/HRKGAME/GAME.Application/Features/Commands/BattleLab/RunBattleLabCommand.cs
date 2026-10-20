using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Battle;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.BattleLab;

namespace GAME.Application.Features.Commands.BattleLab;

public sealed record RunBattleLabCommand(BattleLabRequestDto Request) : ICommand<BaseResponse<BattleLabReportDto>>;

public sealed class RunBattleLabCommandHandler(
    IUnitOfWork unitOfWork,
    IBattleSimulationEngine engine,
    BattleLabBuildResolver? buildResolver, IGameTemplateCatalog? templates = null)
    : BattleLabHandlerSupport(unitOfWork, templates), ICommandHandler<RunBattleLabCommand, BaseResponse<BattleLabReportDto>>
{
    private readonly IBattleSimulationEngine _engine = engine;
    private readonly BattleLabBuildResolver? _buildResolver = buildResolver;
    public async Task<BaseResponse<BattleLabReportDto>> Handle(RunBattleLabCommand message, CancellationToken ct) => BaseResponse<BattleLabReportDto>.SuccessResponse(await ExecuteAsync(message.Request, ct));
    private async Task<BattleLabReportDto> ExecuteAsync(BattleLabRequestDto request, CancellationToken ct)
    {
        BattleLabPolicy.Validate(request);
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
                    resolvedBuilds.Add(await (_buildResolver ?? throw new InvalidOperationException("Build resolver unavailable.")).ResolveAsync(heroes[i], slots[i].Build!, team, ct));
        }

        var maxEnergy = Config(configs, "MAX_ENERGY", 100, 1);
        var energy = Math.Clamp(Config(configs, "INITIAL_ENERGY", 0), 0, maxEnergy);
        var combatants = initial.LeftTeam.Select(h => BattleCombatantMapper.Map(h, 0, energy, maxEnergy)).Concat(initial.RightTeam.Select(h => BattleCombatantMapper.Map(h, 1, energy, maxEnergy))).ToList();
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
            SnapshotHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new { initial, configs, request.MaxRounds })))),
            Warnings = ["One matchup cannot establish a hero's global balance. Compare identical seeds/builds, swap sides and test multiple opponents.", "AuraTier currently has no stat bonus in the shared hero calculator. StarAura is visual.", "No formation/faction/set bonus is added by Lab. CUSTOM stats are final; BUILD equipment is applied once.", "Accuracy, resistance and lifesteal are not mapped into BattleCombatant by the current shared mapper; do not infer their effectiveness from this report."]
        };
        var outcomes = new HashSet<string>();
        for (var i = 0; i < request.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var seed = request.Seed + i;
            var simulation = _engine.Simulate(new BattleSimulationRequest { RandomSeed = seed, MaxRounds = request.MaxRounds, Combatants = combatants, DefenseMitigationConstant = request.DefenseConstant.Value, BasicAttackEnergyGain = Config(configs, "BASIC_ATTACK_ENERGY_GAIN", 25), BasicAttackHitEnergyGain = Config(configs, "BASIC_ATTACK_HIT_ENERGY_GAIN", 25) });
            var stats = BattleStatisticsCalculator.Calculate(initial, simulation.Events);
            report.Runs.Add(new BattleLabRunDto { Heroes = stats.Select(s => new BattleLabHeroRunDto { Statistics = new BattleHeroStatisticsDto { CombatantId = s.CombatantId, SourceHeroId = s.SourceHeroId, Team = s.Team, HeroName = s.HeroName, PhysicalDamageDealt = s.PhysicalDamageDealt, MagicDamageDealt = s.MagicDamageDealt, HealingDone = s.HealingDone, PhysicalDamageTaken = s.PhysicalDamageTaken, MagicDamageTaken = s.MagicDamageTaken }, RemainingHp = simulation.Events.LastOrDefault(e => e.TargetId == s.CombatantId && e.HpAfter.HasValue && (e.EventType == "DAMAGE" || e.EventType == "HEAL" || e.EventType == "BLEED_DAMAGE" || e.EventType == "BLEED_DETONATED"))?.HpAfter ?? (s.Team == 0 ? initial.LeftTeam : initial.RightTeam).Single(h => h.Id == Math.Abs(s.CombatantId)).Stats.Hp, SkillCasts = simulation.Events.Count(e => e.ActorId == s.CombatantId && e.EventType == "SKILL_CAST"), EnergyCasts = simulation.Events.Count(e => e.ActorId == s.CombatantId && e.EventType == "SKILL_CAST" && (s.Team == 0 ? initial.LeftTeam : initial.RightTeam).Single(h => h.Id == Math.Abs(s.CombatantId)).Skills.Any(k => k.Id == e.SkillId && k.SkillTypeCode == "ENERGY")) }).ToList(), Seed = seed, Winner = simulation.Winner, Rounds = simulation.Events.Select(e => e.Round).DefaultIfEmpty().Max() });
            foreach (var group in simulation.Events.Where(e => e.EventType == "DAMAGE" && e.ActorId.HasValue)
                .GroupBy(e => (Actor: e.ActorId!.Value, Skill: e.SkillId ?? "", School: e.DamageSchoolCode ?? "")))
            {
                var metric = report.Skills.SingleOrDefault(m => m.ActorId == group.Key.Actor && m.SkillId == group.Key.Skill && m.School == group.Key.School);
                if (metric == null)
                {
                    metric = new()
                    {
                        ActorId = group.Key.Actor,
                        SkillId = group.Key.Skill,
                        School = group.Key.School
                    };
                    report.Skills.Add(metric);
                }

                metric.Hits += group.Count();
                metric.HpDamage += group.Sum(e => (long)e.Value);
                metric.CritHits += group.Count(e => e.IsCrit);
            }

            foreach (var stat in stats)
            {
                var total = report.Totals.SingleOrDefault(s => s.CombatantId == stat.CombatantId);
                if (total == null)
                {
                    report.Totals.Add(stat);
                    continue;
                }

                total.PhysicalDamageDealt += stat.PhysicalDamageDealt;
                total.MagicDamageDealt += stat.MagicDamageDealt;
                total.HealingDone += stat.HealingDone;
                total.PhysicalDamageTaken += stat.PhysicalDamageTaken;
                total.MagicDamageTaken += stat.MagicDamageTaken;
            }

            // Keep one full replay per outcome, not hundreds of multi-MB timelines.
            if (outcomes.Add(simulation.Winner))
            {
                var state = new BattleInitialStateDto
                {
                    LeftTeam = initial.LeftTeam,
                    RightTeam = initial.RightTeam
                };
                report.Replays.Add(BattleResultMapper.Map(Guid.NewGuid().ToString("N"), state, simulation, seed, BattleStatisticsCalculator.Calculate(initial, simulation.Events)));
            }
        }

        return report;
    }

    private static List<PlayerHeroDto> BuildTeam(List<BattleLabSlotDto> slots, List<PlayerHeroDto> catalog) => slots.Select(slot =>
    {
        var template = catalog.SingleOrDefault(h => h.HeroTemplateId == slot.HeroTemplateId) ?? throw new BusinessException("INVALID_REQUEST", $"Unknown hero template {slot.HeroTemplateId}.", BusinessErrorKind.Validation);
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
    private static int Config(IReadOnlyDictionary<string, decimal> configs, string code, int fallback, int min = 0) => Math.Max(min, configs.TryGetValue(code, out var value) ? decimal.ToInt32(value) : fallback);
}
