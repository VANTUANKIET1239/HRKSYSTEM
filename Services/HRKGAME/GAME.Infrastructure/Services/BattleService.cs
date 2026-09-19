using Core.Common.Repositories;
using GAME.Application.Common.Helpers;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services
{
    public class BattleService : IBattleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;

        public BattleService(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService)
        {
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
        }

        public async Task<BattleInitialStateDto> GetBattleInitialStateAsync(string userId, CancellationToken cancellationToken = default)
        {
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);

            var leftTeam = new List<PlayerHeroDto>();

            if (player != null)
            {
                var formation = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
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
                    .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.FormationName == "Main Team" && f.IsActive, cancellationToken);

                if (formation != null)
                {
                    AddHeroIfActive(leftTeam, formation.Hero1);
                    AddHeroIfActive(leftTeam, formation.Hero2);
                    AddHeroIfActive(leftTeam, formation.Hero3);
                    AddHeroIfActive(leftTeam, formation.Hero4);
                    AddHeroIfActive(leftTeam, formation.Hero5);
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
                .Take(5)
                .ToListAsync(cancellationToken);

            long enemyIdCounter = 9001;
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

        private static void AddHeroIfActive(List<PlayerHeroDto> list, HrkPlayerHero? ph)
        {
            if (ph == null || !ph.IsActive) return;
            var dto = GameDtoMapper.MapPlayerHero(ph);
            if (dto != null) list.Add(dto);
        }
    }
}
