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

        public BattleService(
            IUnitOfWork unitOfWork,
            IGamePlayerService gamePlayerService,
            IFormationStatService formationStatService,
            IHeroStatCalculationService heroStatCalculationService,
            ICombatPowerService combatPowerService,
            IBattleSimulationEngine battleSimulationEngine)
        {
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
            _formationStatService = formationStatService;
            _heroStatCalculationService = heroStatCalculationService;
            _combatPowerService = combatPowerService;
            _battleSimulationEngine = battleSimulationEngine;
        }

        public async Task<StartBattleResultDto> StartBattleAsync(string userId, StartBattleRequestDto request, CancellationToken cancellationToken = default)
        {
            var initialState = await GetBattleInitialStateAsync(userId, cancellationToken);
            if (initialState.LeftTeam.Count == 0)
                throw new InvalidOperationException("The selected formation has no active heroes.");
            if (initialState.RightTeam.Count == 0)
                throw new InvalidOperationException("The enemy formation has no active heroes.");

            var seed = request.RandomSeed ?? System.Random.Shared.Next(1, int.MaxValue);
            var combatants = initialState.LeftTeam.Select(h => MapCombatant(h, 0))
                .Concat(initialState.RightTeam.Select(h => MapCombatant(h, 1)))
                .ToList();
            var simulation = _battleSimulationEngine.Simulate(new BattleSimulationRequest
            {
                RandomSeed = seed,
                MaxRounds = 100,
                Combatants = combatants
            });

            return new StartBattleResultDto
            {
                BattleId = initialState.BattleId,
                RandomSeed = seed,
                Winner = simulation.Winner,
                InitialState = initialState,
                Events = simulation.Events.Select(e => new BattleEventDto
                {
                    Sequence = e.Sequence, Round = e.Round, Turn = e.Turn, EventType = e.EventType,
                    ActorId = e.ActorId, TargetId = e.TargetId, SkillId = e.SkillId,
                    EffectTypeCode = e.EffectTypeCode, DamageSchoolCode = e.DamageSchoolCode,
                    Value = e.Value, HpBefore = e.HpBefore, HpAfter = e.HpAfter,
                    EnergyBefore = e.EnergyBefore, EnergyAfter = e.EnergyAfter,
                    IsCrit = e.IsCrit, RemainingTurns = e.RemainingTurns
                }).ToList()
            };
        }


        private static BattleCombatant MapCombatant(PlayerHeroDto hero, int team)
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
                Energy = 0,
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
                Scalings = effect.Scalings.Select(s =>
                    new BattleEffectScaling(s.AttributeTypeCode, s.Coefficient, s.FlatValue)).ToList(),
                StatModifiers = effect.StatModifiers.Select(m =>
                    new BattleStatModifier(m.AttributeTypeCode, m.ValueType, m.Value)).ToList()
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

        public async Task<BattleInitialStateDto> GetBattleInitialStateAsync(string userId, CancellationToken cancellationToken = default)
        {
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
            var leftTeam = new List<PlayerHeroDto>();

            if (player != null)
            {
                // Lấy formation được chọn (IsSelected = true)
                var formation = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
                    .Where(f => f.PlayerId == player.Id && f.IsSelected && f.IsActive)
                    .Include(f => f.FormationTemplate).ThenInclude(t => t.LevelConfigs)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)

                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)

                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)

                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)

                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
                    .FirstOrDefaultAsync(cancellationToken);

                // Fallback nếu chưa có formation nào IsSelected
                if (formation == null)
                {
                    formation = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
                        .Where(f => f.PlayerId == player.Id && f.IsActive)
                        .Include(f => f.FormationTemplate).ThenInclude(t => t.LevelConfigs)
                        .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                        .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                        .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                        .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)

                        .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                        .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                        .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                        .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)

                        .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                        .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                        .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                        .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)

                        .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                        .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                        .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                        .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)

                        .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                        .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                        .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                        .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                if (formation != null)
                {
                    // Load the full active skill graph once; EF relationship fix-up enriches
                    // the already tracked heroes without repeating this graph for five slots.
                    var formationTemplateIds = new[]
                    {
                        formation.Hero1?.HeroTemplateId, formation.Hero2?.HeroTemplateId,
                        formation.Hero3?.HeroTemplateId, formation.Hero4?.HeroTemplateId,
                        formation.Hero5?.HeroTemplateId
                    }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
                    var fullHeroSkills = await _unitOfWork.ReadOnlyRepository<HrkHeroSkill>().Query()
                        .Where(hs => formationTemplateIds.Contains(hs.HeroTemplateId) && hs.Skill.IsActive)
                        .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.EffectType)
                        .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.TargetType)
                        .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.Scalings).ThenInclude(s => s.AttributeType)
                        .Include(hs => hs.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.StatModifiers).ThenInclude(m => m.AttributeType)
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
                    // Đọc bonus từ cấp độ trận pháp
                    var levelCfg = formation.FormationTemplate?.LevelConfigs?.FirstOrDefault(c => c.Level == formation.Level);
                    var bonus = _formationStatService.ParseBonus(levelCfg?.StatBonusJson);

                    var activeHeroes = new (int Slot, HrkPlayerHero? Hero)[]
                    {
                        (1, formation.Hero1),
                        (2, formation.Hero2),
                        (3, formation.Hero3),
                        (4, formation.Hero4),
                        (5, formation.Hero5)
                    }.Where(x => x.Hero != null && x.Hero.IsActive).ToList();

                    var heroIds = activeHeroes.Select(x => x.Hero!.Id).ToList();

                    // Batch load equipment của các tướng
                    var equipments = await _unitOfWork.ReadOnlyRepository<HrkPlayerEquipment>().Query()
                        .Where(e => e.PlayerId == player.Id && heroIds.Contains(e.HeroId))
                        .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                        .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                        .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                        .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                        .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                        .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                        .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                        .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                        .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                        .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                        .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                        .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                        .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                        .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                        .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                        .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                        .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                        .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                    var eqByHeroId = equipments.ToDictionary(e => e.HeroId);
                    var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);

                    foreach (var (slot, hero) in activeHeroes)
                    {
                        var dto = GameDtoMapper.MapPlayerHero(hero);
                        if (dto == null) continue;

                        eqByHeroId.TryGetValue(hero!.Id, out var eq);
                        var baseStats = _heroStatCalculationService.CalculateStats(hero, eq).FinalStats;
                        var formationStats = _formationStatService.ApplyFormationBonus(baseStats, bonus);

                        dto.Stats = formationStats;
                        dto.Power = _combatPowerService.Calculate(formationStats, powerConfigs);
                        dto.Position = slot;
                        dto.Skills = skillsByTemplateId.GetValueOrDefault(
                            hero.HeroTemplateId, new List<SkillTemplateDto>());

                        leftTeam.Add(dto);
                    }
                }
            }

            var enemyTemplates = await _unitOfWork.ReadOnlyRepository<HrkHeroTemplate>().Query()
                .Include(ht => ht.Faction)
                .Include(ht => ht.Class)
                .Include(ht => ht.Rarity)
                .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
                .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
                .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
                .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
                // Temporary test campaign enemy formation. Templates 1 and 2 only use
                // DAMAGE effects, which are fully supported by the current engine.
                .OrderBy(ht => ht.Id)
                .Take(2)
                .ToListAsync(cancellationToken);

            long enemyIdCounter = 9001;
            int enemyPosCounter = 1;
            var rightTeam = enemyTemplates.Select(ht => new PlayerHeroDto
            {
                Id = enemyIdCounter++,
                HeroTemplateId = ht.Id,
                Name = $"[Địch] {ht.Name}",
                Avatar = ht.Avatar,
                FactionCode = ht.Faction?.Code ?? "",
                FactionName = ht.Faction?.Name ?? "",
                ClassCode = ht.Class?.Code ?? "",
                ClassName = ht.Class?.Name ?? "",
                RarityCode = ht.Rarity?.Code ?? "",
                RarityName = ht.Rarity?.Name ?? "",
                RarityColorHex = ht.Rarity?.ColorHex,
                Level = 10,
                Exp = 0,
                MaxExp = 1000,
                Stars = 2,
                Power = (int)(ht.BaseAtk * 3.5),
                Position = enemyPosCounter++,
                AuraTier = 1,
                IsLocked = false,
                IsFavorite = false,
                Stats = new CalculatedStatsDto
                {
                    Hp = ht.BaseHp * 2,
                    Atk = (int)(ht.BaseAtk * 1.5),
                    Def = (int)(ht.BaseDef * 1.2),
                    Spd = ht.BaseSpd,
                    Crit = ht.BaseCrit,
                    CritDmg = ht.BaseCritDmg,
                    Lifesteal = ht.BaseLifesteal,
                    Accuracy = ht.BaseAccuracy,
                    Resistance = ht.BaseResistance,
                    MagicDamage = (int)(ht.BaseMagicDamage * 1.5),
                    MagicResistance = (int)(ht.BaseMagicResistance * 1.2)
                },
                Skills = ht.HeroSkills
                    .OrderBy(hs => hs.SkillOrder)
                    .Select(hs => GameDtoMapper.MapSkillTemplate(hs.Skill)!)
                    .Where(s => s != null)
                    .ToList()
            }).ToList();

            return new BattleInitialStateDto
            {
                BattleId = Guid.NewGuid().ToString("N"),
                LeftTeam = leftTeam,
                RightTeam = rightTeam
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
