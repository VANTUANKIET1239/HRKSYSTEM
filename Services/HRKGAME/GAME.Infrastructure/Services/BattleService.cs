using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Infrastructure.Services
{
    public class BattleService : IBattleService
    {
        private readonly IUnitOfWork<GameDbContext> _unitOfWork;

        public BattleService(IUnitOfWork<GameDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BattleInitialStateDto> GetBattleInitialStateAsync(string userId, CancellationToken cancellationToken = default)
        {
            var player = await _unitOfWork.Repository<HrkPlayer>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            var leftTeam = new List<PlayerHeroDto>();

            if (player != null)
            {
                var formation = await _unitOfWork.Repository<HrkPlayerFormation>().Query()
                    .AsNoTracking()
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill)

                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill)

                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill)

                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill)

                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill)
                    .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.FormationName == "Main Team", cancellationToken);

                if (formation != null)
                {
                    AddIfNotNull(leftTeam, formation.Hero1);
                    AddIfNotNull(leftTeam, formation.Hero2);
                    AddIfNotNull(leftTeam, formation.Hero3);
                    AddIfNotNull(leftTeam, formation.Hero4);
                    AddIfNotNull(leftTeam, formation.Hero5);
                }
            }

            var enemyTemplates = await _unitOfWork.Repository<HrkHeroTemplate>().Query()
                .AsNoTracking()
                .Include(ht => ht.Faction)
                .Include(ht => ht.Class)
                .Include(ht => ht.Rarity)
                .Include(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill)
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
                    Resistance = ht.BaseResistance
                },
                Skills = ht.HeroSkills.OrderBy(hs => hs.SkillOrder).Select(hs => new SkillTemplateDto
                {
                    Id = hs.Skill.Id,
                    Name = hs.Skill.Name,
                    Icon = hs.Skill.Icon,
                    Description = hs.Skill.Description,
                    Cost = hs.Skill.Cost,
                    DamageMultiplier = hs.Skill.DamageMultiplier,
                    TargetType = hs.Skill.TargetType,
                    Cooldown = hs.Skill.Cooldown
                }).ToList()
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
            var logs = await _unitOfWork.Repository<HrkBattleLog>().Query()
                .AsNoTracking()
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
                BattleDetails = ParseJson(b.BattleDetails),
                CreatedOn = b.CreatedOn
            }).ToList();
        }

        private static void AddIfNotNull(List<PlayerHeroDto> list, HrkPlayerHero? ph)
        {
            if (ph == null) return;
            var ht = ph.HeroTemplate;
            list.Add(new PlayerHeroDto
            {
                Id = ph.Id,
                HeroTemplateId = ph.HeroTemplateId,
                Name = ht?.Name ?? "",
                Avatar = ht?.Avatar ?? "",
                FactionCode = ht?.Faction?.Code ?? "",
                FactionName = ht?.Faction?.Name ?? "",
                ClassCode = ht?.Class?.Code ?? "",
                ClassName = ht?.Class?.Name ?? "",
                RarityCode = ht?.Rarity?.Code ?? "",
                RarityName = ht?.Rarity?.Name ?? "",
                RarityColorHex = ht?.Rarity?.ColorHex,
                Level = ph.Level,
                Exp = ph.Exp,
                MaxExp = ph.MaxExp,
                Stars = ph.Stars,
                Power = ph.Power,
                AuraTier = ph.AuraTier,
                IsLocked = ph.IsLocked,
                IsFavorite = ph.IsFavorite,
                Stats = CalculateStats(ph),
                Skills = ht?.HeroSkills.OrderBy(hs => hs.SkillOrder).Select(hs => new SkillTemplateDto
                {
                    Id = hs.Skill.Id,
                    Name = hs.Skill.Name,
                    Icon = hs.Skill.Icon,
                    Description = hs.Skill.Description,
                    Cost = hs.Skill.Cost,
                    DamageMultiplier = hs.Skill.DamageMultiplier,
                    TargetType = hs.Skill.TargetType,
                    Cooldown = hs.Skill.Cooldown
                }).ToList() ?? new List<SkillTemplateDto>()
            });
        }

        private static CalculatedStatsDto CalculateStats(HrkPlayerHero ph)
        {
            if (!string.IsNullOrWhiteSpace(ph.CurrentStats))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<CalculatedStatsDto>(ph.CurrentStats, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed != null) return parsed;
                }
                catch { }
            }

            var ht = ph.HeroTemplate;
            if (ht == null) return new CalculatedStatsDto();

            decimal levelMultiplier = 1.0m + (ph.Level - 1) * 0.05m;
            decimal starMultiplier = 1.0m + (ph.Stars - 1) * 0.15m;
            decimal totalMultiplier = levelMultiplier * starMultiplier;

            return new CalculatedStatsDto
            {
                Hp = (int)(ht.BaseHp * totalMultiplier),
                Atk = (int)(ht.BaseAtk * totalMultiplier),
                Def = (int)(ht.BaseDef * totalMultiplier),
                Spd = (int)(ht.BaseSpd * (1.0m + (ph.Level - 1) * 0.01m)),
                Crit = ht.BaseCrit,
                CritDmg = ht.BaseCritDmg,
                Lifesteal = ht.BaseLifesteal,
                Accuracy = ht.BaseAccuracy,
                Resistance = ht.BaseResistance
            };
        }

        private static object? ParseJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonSerializer.Deserialize<object>(json); }
            catch { return json; }
        }
    }
}
