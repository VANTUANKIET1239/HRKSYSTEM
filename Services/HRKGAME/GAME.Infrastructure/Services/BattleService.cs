using Core.Common.Repositories;
using GAME.Application.Common.Helpers;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Domain.Battle;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services
{
    public class BattleService : IBattleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;
        private readonly IFormationStatService _formationStatService;
        private readonly IHeroStatCalculationService _heroStatCalculationService;
        private readonly ICombatPowerService _combatPowerService;
        private readonly IBattleSimulationEngine _battleSimulationEngine;
        private readonly IFormationSnapshotService _formationSnapshotService;

        public BattleService(
            IUnitOfWork unitOfWork,
            IGamePlayerService gamePlayerService,
            IFormationStatService formationStatService,
            IHeroStatCalculationService heroStatCalculationService,
            ICombatPowerService combatPowerService,
            IBattleSimulationEngine battleSimulationEngine,
            IFormationSnapshotService formationSnapshotService)
        {
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
            _formationStatService = formationStatService;
            _heroStatCalculationService = heroStatCalculationService;
            _combatPowerService = combatPowerService;
            _battleSimulationEngine = battleSimulationEngine;
            _formationSnapshotService = formationSnapshotService;
        }

        public async Task<StartBattleResultDto> StartBattleAsync(string userId, StartBattleRequestDto request, CancellationToken cancellationToken = default)
        {
            var battleConfigs = await _unitOfWork.ReadOnlyRepository<HrkBattleConfig>().Query()
                .Where(x => x.IsEnabled)
                .ToDictionaryAsync(x => x.Code, x => x.Value, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var maxRounds = GetPositiveIntConfig(battleConfigs, "MAX_ROUNDS", 100);
            var maxEnergy = GetPositiveIntConfig(battleConfigs, "MAX_ENERGY", 100);
            var initialEnergy = Math.Clamp(GetIntConfig(battleConfigs, "INITIAL_ENERGY", 0), 0, maxEnergy);
            var basicEnergyGain = Math.Max(0, GetIntConfig(battleConfigs, "BASIC_ATTACK_ENERGY_GAIN", 25));
            var basicHitEnergyGain = Math.Max(0, GetIntConfig(battleConfigs, "BASIC_ATTACK_HIT_ENERGY_GAIN", 25));
            var initialState = await GetBattleInitialStateAsync(userId, cancellationToken, request.StageId, request.FormationCode, request.Positions);
            if (initialState.LeftTeam.Count == 0)
                throw new InvalidOperationException("The selected formation has no active heroes.");
            if (initialState.RightTeam.Count == 0)
                throw new InvalidOperationException("The enemy formation has no active heroes.");

            var seed = request.RandomSeed ?? System.Random.Shared.Next(1, int.MaxValue);
            var combatants = initialState.LeftTeam.Select(h => MapCombatant(h, 0, initialEnergy, maxEnergy))
                .Concat(initialState.RightTeam.Select(h => MapCombatant(h, 1, initialEnergy, maxEnergy)))
                .ToList();
            var simulation = _battleSimulationEngine.Simulate(new BattleSimulationRequest
            {
                RandomSeed = seed,
                MaxRounds = maxRounds,
                BasicAttackEnergyGain = basicEnergyGain,
                BasicAttackHitEnergyGain = basicHitEnergyGain,
                Combatants = combatants
            });

            var heroStats = BattleStatisticsCalculator.Calculate(initialState, simulation.Events);

            return new StartBattleResultDto
            {
                BattleId = initialState.BattleId,
                RandomSeed = seed,
                Winner = simulation.Winner,
                InitialState = initialState,
                HeroStatistics = heroStats,
                Events = simulation.Events.Select(e => new BattleEventDto
                {
                    Sequence = e.Sequence, Round = e.Round, Turn = e.Turn, EventType = e.EventType,
                    ActorId = e.ActorId, TargetId = e.TargetId, SkillId = e.SkillId,
                    EffectTypeCode = e.EffectTypeCode, DamageSchoolCode = e.DamageSchoolCode,
                    Value = e.Value, HpBefore = e.HpBefore, HpAfter = e.HpAfter,
                    EnergyBefore = e.EnergyBefore, EnergyAfter = e.EnergyAfter,
                    IsCrit = e.IsCrit, RemainingTurns = e.RemainingTurns,
                    PreviousStacks = e.PreviousStacks, CurrentStacks = e.CurrentStacks, MaxStacks = e.MaxStacks,
                    CastSequence = e.CastSequence, TimelineOffsetMs = e.TimelineOffsetMs,
                    PhaseCode = e.PhaseCode,
                    ExecutionGroup = e.ExecutionGroup,
                    HitIndex = e.HitIndex,
                    ResourceCode = e.ResourceCode,
                    PreviousValue = e.PreviousValue,
                    CurrentValue = e.CurrentValue,
                    ReasonCode = e.ReasonCode,
                    ActionId = e.ActionId,
                    StatusInstanceId = e.StatusInstanceId,
                    SourceHeroId = e.SourceHeroId,
                    OriginalDamage = e.OriginalDamage,
                    RedirectRequested = e.RedirectRequested,
                    RedirectActual = e.RedirectActual,
                    AllyDamageAfterRedirect = e.AllyDamageAfterRedirect,
                    GuardianHpBefore = e.GuardianHpBefore,
                    GuardianHpAfter = e.GuardianHpAfter,
                    StatModifiers = e.StatModifiers.Select(m => new BattleStatModifierDto
                    {
                        AttributeCode = m.AttributeCode,
                        AttributeName = m.AttributeName,
                        ValueType = m.ValueType,
                        Value = m.Value
                    }).ToList()
                }).ToList()
            };
        }


        private BattleCombatant MapCombatant(PlayerHeroDto hero, int team, int initialEnergy, int maxEnergy)
        {
            var activeSkills = hero.Skills.Where(s => s != null).ToList();
            var basic = activeSkills.SingleOrDefault(s => s.SkillTypeCode == BattleCodes.Normal)
                ?? throw new InvalidOperationException($"Hero {hero.Name} must have exactly one active NORMAL skill.");
            var energySkills = activeSkills.Where(s => s.SkillTypeCode == BattleCodes.Energy).ToList();
            if (energySkills.Count > 1)
                throw new InvalidOperationException($"Hero {hero.Name} can have at most one active ENERGY skill.");

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

        private static int GetPositiveIntConfig(IReadOnlyDictionary<string, decimal> configs, string code, int fallback) =>
            Math.Max(1, GetIntConfig(configs, code, fallback));

        private static int GetIntConfig(IReadOnlyDictionary<string, decimal> configs, string code, int fallback) =>
            configs.TryGetValue(code, out var value)
                ? decimal.ToInt32(decimal.Round(value, 0, MidpointRounding.AwayFromZero))
                : fallback;

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

        private static BattleSkillEffect MapEffect(string skillId, SkillEffectDto effect)
        {
            if (string.IsNullOrWhiteSpace(effect.EffectTypeCode))
                throw new InvalidOperationException(
                    $"Skill '{skillId}' contains effect {effect.Id} without EffectTypeCode.");
            if (string.IsNullOrWhiteSpace(effect.TargetTypeCode))
                throw new InvalidOperationException(
                    $"Skill '{skillId}', effect {effect.Id}, has no TargetTypeCode. Check HRK_SkillEffects.TargetTypeId.");

            return new BattleSkillEffect
            {
                EffectTypeCode = effect.EffectTypeCode,
                TargetTypeCode = NormalizeTargetCode(effect.TargetTypeCode),
                DamageSchoolCode = effect.DamageSchoolCode,
                BaseValue = effect.BaseValue,
                DurationTurns = effect.DurationTurns ?? 0,
                ChancePercent = effect.ChancePercent,
                MaxStacks = effect.MaxStacks ?? 1,
                DisplayOrder = effect.DisplayOrder,
                ExecutionGroup = effect.ExecutionGroup,
                ConditionCode = effect.ConditionCode,
                Scalings = effect.Scalings.Select(s =>
                    new BattleEffectScaling(s.AttributeTypeCode, s.Coefficient, s.FlatValue)).ToList(),
                StatModifiers = effect.StatModifiers.Select(m =>
                    new BattleStatModifier(m.AttributeTypeCode, m.ValueType, m.Value, m.AttributeTypeName)).ToList(),
                Parameters = effect.Parameters.ToDictionary(
                    p => p.ParameterCode,
                    p => new BattleSkillEffectParameter(p.ParameterCode, p.DecimalValue, p.IntValue, p.BoolValue, p.StringValue),
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        private static string NormalizeTargetCode(string code) => code.ToUpperInvariant() switch
        {
            "SINGLE_ENEMY" => BattleCodes.EnemySingle,
            "ALL_ENEMIES" => BattleCodes.EnemyAll,
            "RANDOM_ENEMY" => BattleCodes.EnemyRandom,
            "SELF" => BattleCodes.Self,
            "ALLY_RANDOM" => BattleCodes.AllyRandom,
            "ALL_ALLIES" => BattleCodes.AllyAll,
            var value => value
        };

        public async Task<BattleInitialStateDto> GetBattleInitialStateAsync(
            string userId,
            CancellationToken cancellationToken = default,
            int? stageId = null,
            string? formationCode = null,
            IReadOnlyCollection<FormationPositionRequestDto>? positions = null)
        {
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
            var leftTeam = new List<PlayerHeroDto>();

            if (player != null)
            {
                var snapshot = await _formationSnapshotService.BuildAsync(player.Id, formationCode, positions, cancellationToken);
                leftTeam = snapshot.Heroes;

                if (leftTeam.Count > 0)
                {
                    // Load the full active skill graph once
                    var formationTemplateIds = leftTeam.Select(x => x.HeroTemplateId).Distinct().ToList();
                    var fullHeroSkills = await _unitOfWork.ReadOnlyRepository<HrkHeroSkill>().Query()
                        .Where(hs => formationTemplateIds.Contains(hs.HeroTemplateId) && hs.Skill.IsActive)
                        .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.EffectType)
                        .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.TargetType)
                        .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.Scalings).ThenInclude(s => s.AttributeType)
                        .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.StatModifiers).ThenInclude(m => m.AttributeType)
                        .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.Parameters)
                        .Include(hs => hs.Skill).ThenInclude(s => s.AnimationConfig)!.ThenInclude(a => a.Phases)
                        .AsSplitQuery()
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                    var skillsByTemplateId = fullHeroSkills
                        .GroupBy(hs => hs.HeroTemplateId)
                        .ToDictionary(
                            group => group.Key,
                            group => group.OrderBy(hs => hs.SkillOrder)
                                .Select(hs => GameDtoMapper.MapSkillTemplate(hs.Skill))
                                .Where(skill => skill != null)
                                .Cast<SkillTemplateDto>()
                                .ToList());

                    var enemyTemplatePool = await _unitOfWork.ReadOnlyRepository<HrkHeroTemplate>().Query()
                        .Include(ht => ht.Faction)
                        .Include(ht => ht.Class)
                        .Include(ht => ht.Rarity)
                        .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
                        .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
                        .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
                        .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
                        .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Parameters)
                        .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.AnimationConfig)!.ThenInclude(a => a.Phases)
                        .OrderBy(ht => ht.Id)
                        .ToListAsync(cancellationToken);

                    var stageEnemyConfigs = stageId.HasValue
                        ? await _unitOfWork.ReadOnlyRepository<HrkDungeonStageEnemy>().Query()
                            .Where(x => x.StageId == stageId.Value)
                            .OrderBy(x => x.Position)
                            .AsNoTracking()
                            .ToListAsync(cancellationToken)
                        : new List<HrkDungeonStageEnemy>();

                    var enemyTemplates = stageEnemyConfigs.Count > 0
                        ? stageEnemyConfigs.Select(config => enemyTemplatePool.Single(x => x.Id == config.HeroTemplateId)).ToList()
                        : enemyTemplatePool.OrderBy(_ => System.Random.Shared.Next()).Take(5).ToList();

                    // Batch load star aura configs for all active heroes and enemy templates (no N+1 query)
                    var allTemplateIds = leftTeam.Select(x => x.HeroTemplateId)
                        .Concat(enemyTemplates.Select(ht => ht.Id))
                        .Distinct()
                        .ToList();

                    var starAuraConfigs = await _unitOfWork.ReadOnlyRepository<HrkHeroStarAuraConfig>().Query()
                        .Where(c => c.IsActive && allTemplateIds.Contains(c.HeroTemplateId))
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                    var starAuraLookup = starAuraConfigs
                        .ToDictionary(c => (c.HeroTemplateId, c.StarLevel));

                    foreach (var hero in leftTeam)
                    {
                        var clampedStars = (byte)Math.Clamp(hero.Stars, 0, 5);
                        starAuraLookup.TryGetValue((hero.HeroTemplateId, clampedStars), out var auraCfg);
                        hero.StarAura = GameDtoMapper.MapStarAura(auraCfg);
                        hero.Skills = skillsByTemplateId.GetValueOrDefault(
                            hero.HeroTemplateId, new List<SkillTemplateDto>());
                    }

                    long enemyIdCounter = 9001;
                    int enemyPosCounter = 1;
                    var rightTeam = enemyTemplates.Select((ht, index) =>
                    {
                        var enemyConfig = stageEnemyConfigs.ElementAtOrDefault(index);
                        var enemyStars = enemyConfig?.Stars ?? (byte)2;
                        var multiplier = enemyConfig?.StatMultiplier ?? 1.5m;
                        starAuraLookup.TryGetValue((ht.Id, enemyStars), out var enemyAuraCfg);
                        return new PlayerHeroDto
                        {
                            Id = enemyIdCounter++,
                            HeroTemplateId = ht.Id,
                            Name = enemyConfig?.DisplayName ?? $"[Địch] {ht.Name}",
                            Avatar = enemyConfig?.ImagePath ?? ht.Avatar,
                            FactionCode = ht.Faction?.Code ?? "",
                            FactionName = ht.Faction?.Name ?? "",
                            ClassCode = ht.Class?.Code ?? "",
                            ClassName = ht.Class?.Name ?? "",
                            RarityCode = ht.Rarity?.Code ?? "",
                            RarityName = ht.Rarity?.Name ?? "",
                            RarityColorHex = ht.Rarity?.ColorHex,
                            Level = enemyConfig?.Level ?? 10,
                            Exp = 0,
                            MaxExp = 1000,
                            Stars = enemyStars,
                            Power = (int)Math.Round((ht.BaseHp * .25m + ht.BaseAtk * 3.5m + ht.BaseDef * 2m + ht.BaseSpd) * multiplier),
                            Position = enemyConfig?.Position ?? enemyPosCounter++,
                            AuraTier = 1,
                            IsLocked = false,
                            IsFavorite = false,
                            Stats = new CalculatedStatsDto
                            {
                                Hp = (int)Math.Round(ht.BaseHp * 2m * multiplier),
                                Atk = (int)Math.Round(ht.BaseAtk * multiplier),
                                Def = (int)Math.Round(ht.BaseDef * multiplier),
                                Spd = (int)Math.Round(ht.BaseSpd * Math.Min(multiplier, 1.5m)),
                                Crit = ht.BaseCrit,
                                CritDmg = ht.BaseCritDmg,
                                Lifesteal = ht.BaseLifesteal,
                                Accuracy = ht.BaseAccuracy,
                                Resistance = ht.BaseResistance,
                                MagicDamage = (int)Math.Round(ht.BaseMagicDamage * multiplier),
                                MagicResistance = (int)Math.Round(ht.BaseMagicResistance * multiplier)
                            },
                            Skills = ht.HeroSkills
                                .OrderBy(hs => hs.SkillOrder)
                                .Select(hs => GameDtoMapper.MapSkillTemplate(hs.Skill)!)
                                .Where(s => s != null)
                                .ToList(),
                            StarAura = GameDtoMapper.MapStarAura(enemyAuraCfg)
                        };
                    }).ToList();

                    return new BattleInitialStateDto
                    {
                        BattleId = Guid.NewGuid().ToString("N"),
                        LeftTeam = leftTeam,
                        RightTeam = rightTeam
                    };
                }
            }

            return new BattleInitialStateDto
            {
                BattleId = Guid.NewGuid().ToString("N"),
                LeftTeam = leftTeam,
                RightTeam = new List<PlayerHeroDto>()
            };
        }

        public async Task<List<BattleLogDto>> GetBattleLogsAsync(string battleId, CancellationToken cancellationToken = default)
        {
            var logs = await _unitOfWork.ReadOnlyRepository<HrkBattleLog>().Query()
                .Where(b => b.BattleId == battleId)
                .Include(b => b.Skill)
                .OrderBy(b => b.Turn)
                .ToListAsync(cancellationToken);

            return logs.Select(b => new BattleLogDto
            {
                Id = b.Id,
                BattleId = b.BattleId,
                Turn = b.Turn,
                ActorHeroId = b.ActorHeroId,
                TargetHeroId = b.TargetHeroId,
                SkillId = b.SkillId,
                SkillName = b.Skill?.Name,
                Damage = b.Damage,
                IsCrit = b.IsCrit,
                BattleDetails = GameJsonHelper.ParseJson(b.BattleDetails),
                CreatedOn = b.CreatedOn
            }).ToList();
        }
    }
}
