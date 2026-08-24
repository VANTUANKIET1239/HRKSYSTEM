using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Infrastructure.Services
{
    public class FormationService : IFormationService
    {
        private readonly IUnitOfWork<GameDbContext> _unitOfWork;

        public FormationService(IUnitOfWork<GameDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<FormationDto> GetPlayerFormationAsync(string userId, string formationName = "Main Team", CancellationToken cancellationToken = default)
        {
            var player = await _unitOfWork.Repository<HrkPlayer>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            var positions = new List<FormationPositionDto>();
            int computedPower = 0;

            if (player != null)
            {
                var formation = await _unitOfWork.Repository<HrkPlayerFormation>().Query()
                    .AsNoTracking()
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)

                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)

                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)

                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)

                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)
                    .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.FormationName == formationName, cancellationToken);

                if (formation != null)
                {
                    var h1 = MapHero(formation.Hero1);
                    var h2 = MapHero(formation.Hero2);
                    var h3 = MapHero(formation.Hero3);
                    var h4 = MapHero(formation.Hero4);
                    var h5 = MapHero(formation.Hero5);

                    positions.Add(new FormationPositionDto { Slot = 1, Hero = h1 });
                    positions.Add(new FormationPositionDto { Slot = 2, Hero = h2 });
                    positions.Add(new FormationPositionDto { Slot = 3, Hero = h3 });
                    positions.Add(new FormationPositionDto { Slot = 4, Hero = h4 });
                    positions.Add(new FormationPositionDto { Slot = 5, Hero = h5 });

                    computedPower = (h1?.Power ?? 0) + (h2?.Power ?? 0) + (h3?.Power ?? 0) + (h4?.Power ?? 0) + (h5?.Power ?? 0);

                    return new FormationDto
                    {
                        PlayerId = player.Id,
                        FormationName = formationName,
                        TotalPower = formation.TotalPower > 0 ? formation.TotalPower : computedPower,
                        Positions = positions
                    };
                }
            }

            for (int i = 1; i <= 5; i++)
            {
                positions.Add(new FormationPositionDto { Slot = i, Hero = null });
            }

            return new FormationDto
            {
                PlayerId = player?.Id ?? 0,
                FormationName = formationName,
                TotalPower = 0,
                Positions = positions
            };
        }

        private static PlayerHeroDto? MapHero(HrkPlayerHero? ph)
        {
            if (ph == null) return null;
            var ht = ph.HeroTemplate;

            return new PlayerHeroDto
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
                    CostTypeCode = hs.Skill.CostType?.Code ?? "",
                    CostTypeName = hs.Skill.CostType?.Name ?? "",
                    DamageMultiplier = hs.Skill.DamageMultiplier,
                    TargetType = hs.Skill.TargetType,
                    Cooldown = hs.Skill.Cooldown
                }).ToList() ?? new List<SkillTemplateDto>()
            };
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
    }
}
