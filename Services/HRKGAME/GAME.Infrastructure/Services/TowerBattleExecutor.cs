using Core.Common.Repositories;
using GAME.Application.Common.Helpers;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Battle;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services;

public sealed class TowerBattleExecutor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBattleSimulationEngine _simulationEngine;
    private readonly IRandomService _randomService;
    private readonly ICombatPowerService _combatPowerService;

    public TowerBattleExecutor(IUnitOfWork unitOfWork, IBattleSimulationEngine simulationEngine,
        IRandomService randomService, ICombatPowerService combatPowerService)
    {
        _unitOfWork = unitOfWork;
        _simulationEngine = simulationEngine;
        _randomService = randomService;
        _combatPowerService = combatPowerService;
    }

    public async Task<List<PlayerHeroDto>> BuildEnemiesAsync(HrkTowerFloor floor, CancellationToken ct, bool includeSkills = true)
    {
        var ev = await _unitOfWork.ReadOnlyRepository<HrkGameEvent>().Query().SingleAsync(e => e.Id == floor.EventId, ct);
        var rules = TowerRules.Parse(ev.RulesJson);
        var floorEnemies = floor.Enemies.OrderBy(e => e.Position).ToList();
        var enemyTemplateIds = floorEnemies.Select(e => e.HeroTemplateId).Distinct().ToList();

        var templateQuery = _unitOfWork.ReadOnlyRepository<HrkHeroTemplate>().Query()
            .Where(ht => enemyTemplateIds.Contains(ht.Id))
            .Include(ht => ht.Faction).Include(ht => ht.Class).Include(ht => ht.Rarity)
            .AsQueryable();
        if (includeSkills)
        {
            templateQuery = templateQuery
                .Include(ht => ht.HeroSkills.Where(hs => hs.Skill.IsActive)).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.EffectType)
                .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
                .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
                .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
                .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Parameters)
                .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.AnimationConfig)!.ThenInclude(a => a!.Phases);
        }
        var enemyFullTemplates = await templateQuery.AsNoTracking().AsSplitQuery().ToDictionaryAsync(ht => ht.Id, ct);

        long enemyIdCounter = 9501;
        var rightTeam = new List<PlayerHeroDto>();

        foreach (var enemyCfg in floorEnemies)
        {
            if (!enemyFullTemplates.TryGetValue(enemyCfg.HeroTemplateId, out var ht)) continue;

            // Safe progressive scaling
            int level = enemyCfg.Level;
            decimal statMul = enemyCfg.StatMultiplier * floor.StatMultiplier;
            decimal hpMul = statMul * floor.HpMultiplier;
            decimal atkMul = statMul * floor.AtkMultiplier;
            decimal defMul = statMul * floor.DefMultiplier;

            int scaledHp = Math.Max(100, (int)Math.Round((ht.BaseHp + (level - 1) * rules.HpPerLevel) * hpMul));
            int scaledAtk = Math.Max(10, (int)Math.Round((ht.BaseAtk + (level - 1) * rules.AttackPerLevel) * atkMul));
            int scaledDef = Math.Max(5, (int)Math.Round((ht.BaseDef + (level - 1) * rules.DefensePerLevel) * defMul));
            // Capped Spd and Crit to prevent stun lock / uncounterable speed
            int scaledSpd = Math.Min(ht.BaseSpd + (int)(floor.FloorNumber * rules.SpeedPerFloor), ht.BaseSpd + rules.MaxSpeedBonus);
            decimal scaledCrit = Math.Min(ht.BaseCrit + (floor.FloorNumber * rules.SecondaryPerFloor), rules.MaxCrit);
            int scaledMagicDmg = Math.Max(10, (int)Math.Round((ht.BaseMagicDamage + (level - 1) * rules.AttackPerLevel) * atkMul));
            int scaledMagicRes = Math.Max(5, (int)Math.Round((ht.BaseMagicResistance + (level - 1) * rules.DefensePerLevel) * defMul));


            var eHero = new PlayerHeroDto
            {
                Id = enemyIdCounter++,
                HeroTemplateId = ht.Id,
                Name = enemyCfg.DisplayName ?? (floor.FloorType == "BOSS" && enemyCfg.Position == 1 ? $"[BOSS] {ht.Name}" : $"[Địch] {ht.Name}"),
                Avatar = enemyCfg.ImagePath ?? ht.Avatar,
                FactionCode = ht.Faction?.Code ?? "",
                FactionName = ht.Faction?.Name ?? "",
                ClassCode = ht.Class?.Code ?? "",
                ClassName = ht.Class?.Name ?? "",
                RarityCode = ht.Rarity?.Code ?? "",
                RarityName = ht.Rarity?.Name ?? "",
                RarityColorHex = ht.Rarity?.ColorHex,
                Level = level,
                Stars = enemyCfg.Stars,
                Power = 0,
                Position = enemyCfg.Position,
                AuraTier = 1,
                Stats = new CalculatedStatsDto
                {
                    Hp = scaledHp,
                    Atk = scaledAtk,
                    Def = scaledDef,
                    Spd = scaledSpd,
                    Crit = scaledCrit,
                    CritDmg = ht.BaseCritDmg,
                    Lifesteal = ht.BaseLifesteal,
                    Accuracy = ht.BaseAccuracy,
                    Resistance = Math.Min(ht.BaseResistance + (floor.FloorNumber * rules.SecondaryPerFloor), rules.MaxResistance),
                    MagicDamage = scaledMagicDmg,
                    MagicResistance = scaledMagicRes
                },
                Skills = ht.HeroSkills.OrderBy(hs => hs.SkillOrder).Select(hs => GameDtoMapper.MapSkillTemplate(hs.Skill)!).Where(s => s != null).ToList()
            };

            eHero.Power = await _combatPowerService.CalculateAsync(eHero.Stats, ct);
            rightTeam.Add(eHero);
        }


        if (rightTeam.Count == 0)
            throw new InvalidOperationException("Tầng chưa có đội hình địch hợp lệ.");
        return rightTeam;
    }

    public async Task FreezeSkillsAsync(FormationBattleSnapshot snapshot, CancellationToken ct, bool force = false)
    {
        if (!force && snapshot.Heroes.All(h => h.Skills.Count > 0)) return;
        var leftTeam = snapshot.Heroes;
        var formationTemplateIds = leftTeam.Select(x => x.HeroTemplateId).Distinct().ToList();

        var heroSkills = await _unitOfWork.ReadOnlyRepository<HrkHeroSkill>().Query()
            .Where(hs => formationTemplateIds.Contains(hs.HeroTemplateId) && hs.Skill.IsActive)
            .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.EffectType)
            .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.TargetType)
            .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.Scalings).ThenInclude(s => s.AttributeType)
            .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.StatModifiers).ThenInclude(m => m.AttributeType)
            .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.Parameters)
            .Include(hs => hs.Skill).ThenInclude(s => s.AnimationConfig).ThenInclude(a => a!.Phases)
            .AsSplitQuery().AsNoTracking().ToListAsync(ct);

        var skillsByTemplateId = heroSkills.GroupBy(hs => hs.HeroTemplateId).ToDictionary(
            g => g.Key,
            g => g.OrderBy(hs => hs.SkillOrder).Select(hs => GameDtoMapper.MapSkillTemplate(hs.Skill)!).Where(s => s != null).ToList());

        var starAuras = await _unitOfWork.ReadOnlyRepository<HrkHeroStarAuraConfig>().Query()
            .Where(c => c.IsActive && formationTemplateIds.Contains(c.HeroTemplateId))
            .AsNoTracking().ToListAsync(ct);
        var starAuraLookup = starAuras.ToDictionary(c => (c.HeroTemplateId, c.StarLevel));

        foreach (var hero in leftTeam)
        {
            var clampedStars = (byte)Math.Clamp(hero.Stars, 0, 5);
            starAuraLookup.TryGetValue((hero.HeroTemplateId, clampedStars), out var auraCfg);
            hero.StarAura = GameDtoMapper.MapStarAura(auraCfg);
            if (force || hero.Skills.Count == 0)
                hero.Skills = skillsByTemplateId.GetValueOrDefault(hero.HeroTemplateId, new List<SkillTemplateDto>());
        }

        // 2. Build Right Team (Tower Floor Enemies)

    }

    public async Task UpdateRunAsync(HrkPlayerEventPeriodProgress progress, int foughtFloor, CancellationToken ct)
    {
        var run = await _unitOfWork.Repository<HrkPlayerTowerRun>().Query()
            .SingleAsync(r => r.PlayerId == progress.PlayerId && r.EventPeriodId == progress.EventPeriodId &&
                r.RunNumber == progress.CurrentRunNumber, ct);
        run.EndFloor = foughtFloor;
        run.LivesRemaining = progress.RemainingLives;
        run.Status = progress.IsCompleted ? "COMPLETED" : progress.RemainingLives <= 0 ? "DEFEATED" : "IN_PROGRESS";
        if (run.Status != "IN_PROGRESS") run.EndAtUtc = DateTime.UtcNow;
    }

    public async Task<int> MaxFloorAsync(int eventId, CancellationToken ct)
    {
        var ev = await _unitOfWork.ReadOnlyRepository<HrkGameEvent>().Query().SingleAsync(e => e.Id == eventId, ct);
        var floorNumbers = await _unitOfWork.ReadOnlyRepository<HrkTowerFloor>().Query()
            .Where(f => f.EventId == eventId && f.IsActive).Select(f => f.FloorNumber).ToListAsync(ct);
        int maxFloor = TowerRules.Parse(ev.RulesJson).MaxFloor ?? floorNumbers.DefaultIfEmpty(0).Max();
        if (maxFloor <= 0 || Enumerable.Range(1, maxFloor).Except(floorNumbers).Any())
            throw new InvalidOperationException("Cấu hình tháp phải có đầy đủ các tầng từ 1 đến tầng tối đa.");
        return maxFloor;
    }

    public async Task<(BattleInitialStateDto initialState, BattleSimulationResult simulation, int randomSeed)> SimulateTowerFloorBattle(
        HrkPlayer player,
        FormationBattleSnapshot playerFormation,
        HrkTowerFloor floor,
        CancellationToken ct)
    {
        var battleConfigs = await _unitOfWork.ReadOnlyRepository<HrkBattleConfig>().Query()
            .Where(x => x.IsEnabled)
            .ToDictionaryAsync(x => x.Code, x => x.Value, StringComparer.OrdinalIgnoreCase, ct);

        int maxRounds = GetPositiveIntConfig(battleConfigs, "MAX_ROUNDS", 100);
        int maxEnergy = GetPositiveIntConfig(battleConfigs, "MAX_ENERGY", 100);
        int initialEnergy = Math.Clamp(GetIntConfig(battleConfigs, "INITIAL_ENERGY", 0), 0, maxEnergy);
        int basicEnergyGain = Math.Max(0, GetIntConfig(battleConfigs, "BASIC_ATTACK_ENERGY_GAIN", 25));
        int basicHitEnergyGain = Math.Max(0, GetIntConfig(battleConfigs, "BASIC_ATTACK_HIT_ENERGY_GAIN", 25));

        // Skills are frozen with the selected formation for quick-climb jobs.
        // 1. Build Left Team (Player)
        var leftTeam = playerFormation.Heroes;
        await FreezeSkillsAsync(playerFormation, ct);

        var rightTeam = await BuildEnemiesAsync(floor, ct);

        var initialState = new BattleInitialStateDto
        {
            BattleId = Guid.NewGuid().ToString("N"),
            LeftTeam = leftTeam,
            RightTeam = rightTeam
        };

        var seed = _randomService.Next(1, int.MaxValue);
        var combatants = initialState.LeftTeam.Select(h => MapCombatant(h, 0, initialEnergy, maxEnergy))
            .Concat(initialState.RightTeam.Select(h => MapCombatant(h, 1, initialEnergy, maxEnergy)))
            .ToList();

        var simulation = _simulationEngine.Simulate(new BattleSimulationRequest
        {
            RandomSeed = seed,
            MaxRounds = maxRounds,
            DefenseMitigationConstant = BattleMitigationConfig.Read(battleConfigs),
            BasicAttackEnergyGain = basicEnergyGain,
            BasicAttackHitEnergyGain = basicHitEnergyGain,
            Combatants = combatants
        });

        return (initialState, simulation, seed);
    }

    private BattleCombatant MapCombatant(PlayerHeroDto hero, int team, int initialEnergy, int maxEnergy)
    {
        var activeSkills = hero.Skills.Where(s => s != null).ToList();
        var basic = activeSkills.SingleOrDefault(s => s.SkillTypeCode == BattleCodes.Normal)
            ?? throw new InvalidOperationException($"Hero {hero.Name} must have a NORMAL skill.");
        var energySkills = activeSkills.Where(s => s.SkillTypeCode == BattleCodes.Energy).ToList();

        return new BattleCombatant
        {
            Id = team == 0 ? hero.Id : -hero.Id,
            SourceHeroId = hero.Id,
            Team = team,
            Position = hero.Position ?? 1,
            Name = hero.Name,
            MaxHp = hero.Stats.Hp,
            Hp = hero.Stats.Hp,
            Atk = hero.Stats.Atk,
            Def = hero.Stats.Def,
            Spd = hero.Stats.Spd,
            MagicDamage = hero.Stats.MagicDamage,
            MagicResistance = hero.Stats.MagicResistance,
            CritChance = hero.Stats.Crit,
            CritDamage = hero.Stats.CritDmg,
            Energy = initialEnergy,
            MaxEnergy = maxEnergy,
            BasicSkill = MapSkill(basic),
            EnergySkill = energySkills.Count == 1 ? MapSkill(energySkills[0]) : null
        };
    }

    private static BattleSkill MapSkill(SkillTemplateDto skill) => new()
    {
        Id = skill.Id,
        Name = skill.Name,
        SkillTypeCode = skill.SkillTypeCode,
        EnergyCost = skill.EnergyCost,
        Animation = skill.Animation == null ? BattleSkillAnimation.Default : new BattleSkillAnimation
        {
            AnimationKey = skill.Animation.AnimationKey,
            TotalDurationMs = skill.Animation.TotalDurationMs,
            Phases = skill.Animation.Phases.OrderBy(p => p.DisplayOrder)
                .Select(p => new BattleSkillTimelinePhase(p.PhaseCode, p.StartAtMs, p.DurationMs, p.TriggerEventType))
                .ToList()
        },
        Effects = skill.Effects.Select(e => MapEffect(skill.Id, e)).ToList()
    };

    private static BattleSkillEffect MapEffect(string skillId, SkillEffectDto effect) => new()
    {
        EffectTypeCode = effect.EffectTypeCode,
        TargetTypeCode = effect.TargetTypeCode.ToUpperInvariant() switch
        {
            "SINGLE_ENEMY" => BattleCodes.EnemySingle,
            "ALL_ENEMIES" => BattleCodes.EnemyAll,
            "RANDOM_ENEMY" => BattleCodes.EnemyRandom,
            "SELF" => BattleCodes.Self,
            "ALLY_RANDOM" => BattleCodes.AllyRandom,
            "ALL_ALLIES" => BattleCodes.AllyAll,
            var value => value
        },
        DamageSchoolCode = effect.DamageSchoolCode,
        BaseValue = effect.BaseValue,
        DurationTurns = effect.DurationTurns ?? 0,
        ChancePercent = effect.ChancePercent,
        MaxStacks = effect.MaxStacks ?? 1,
        DisplayOrder = effect.DisplayOrder,
        ExecutionGroup = effect.ExecutionGroup,
        ConditionCode = effect.ConditionCode,
        Scalings = effect.Scalings.Select(s => new BattleEffectScaling(s.AttributeTypeCode, s.Coefficient, s.FlatValue)).ToList(),
        StatModifiers = effect.StatModifiers.Select(m => new BattleStatModifier(m.AttributeTypeCode, m.ValueType, m.Value, m.AttributeTypeName)).ToList(),
        Parameters = effect.Parameters.ToDictionary(
            p => p.ParameterCode,
            p => new BattleSkillEffectParameter(p.ParameterCode, p.DecimalValue, p.IntValue, p.BoolValue, p.StringValue),
            StringComparer.OrdinalIgnoreCase)
    };


    public static StartBattleResultDto ToBattleResult(string battleId, BattleInitialStateDto initialState,
        BattleSimulationResult simulation, int randomSeed, List<BattleHeroStatisticsDto> heroStats) =>
        BattleResultMapper.Map(battleId, initialState, simulation, randomSeed, heroStats);

    private static int GetPositiveIntConfig(IReadOnlyDictionary<string, decimal> configs, string code, int fallback) =>
        Math.Max(1, GetIntConfig(configs, code, fallback));

    private static int GetIntConfig(IReadOnlyDictionary<string, decimal> configs, string code, int fallback) =>
        configs.TryGetValue(code, out var value)
            ? decimal.ToInt32(decimal.Round(value, 0, MidpointRounding.AwayFromZero))
            : fallback;


}
