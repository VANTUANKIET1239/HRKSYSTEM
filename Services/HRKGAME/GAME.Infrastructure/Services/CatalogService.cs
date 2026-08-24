using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Infrastructure.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly IUnitOfWork<GameDbContext> _unitOfWork;

        public CatalogService(IUnitOfWork<GameDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<HeroTemplateDto>> GetHeroTemplatesAsync(CancellationToken cancellationToken = default)
        {
            var heroes = await _unitOfWork.Repository<HrkHeroTemplate>().Query()
                .AsNoTracking()
                .Include(h => h.Faction)
                .Include(h => h.Class)
                .Include(h => h.Rarity)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Category)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.DamageType)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.EffectType)
                .ToListAsync(cancellationToken);

            return heroes.Select(h => MapHeroTemplate(h)).ToList();
        }

        public async Task<HeroTemplateDto?> GetHeroTemplateByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var hero = await _unitOfWork.Repository<HrkHeroTemplate>().Query()
                .AsNoTracking()
                .Include(h => h.Faction)
                .Include(h => h.Class)
                .Include(h => h.Rarity)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Category)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.DamageType)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.EffectType)
                .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

            return hero != null ? MapHeroTemplate(hero) : null;
        }

        public async Task<List<SkillTemplateDto>> GetSkillTemplatesAsync(CancellationToken cancellationToken = default)
        {
            var skills = await _unitOfWork.Repository<HrkSkillTemplate>().Query()
                .AsNoTracking()
                .Include(s => s.CostType)
                .Include(s => s.Category)
                .Include(s => s.DamageType)
                .Include(s => s.EffectType)
                .ToListAsync(cancellationToken);

            return skills.Select(s => MapSkillTemplate(s)).ToList();
        }

        public async Task<List<ItemTemplateDto>> GetItemTemplatesAsync(int? categoryId, int? rarityId, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Repository<HrkItemTemplate>().Query()
                .AsNoTracking()
                .Include(i => i.Category)
                .Include(i => i.Rarity)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(i => i.CategoryId == categoryId.Value);
            }

            if (rarityId.HasValue)
            {
                query = query.Where(i => i.RarityId == rarityId.Value);
            }

            var items = await query.ToListAsync(cancellationToken);

            return items.Select(i => new ItemTemplateDto
            {
                Id = i.Id,
                CategoryId = i.CategoryId,
                CategoryCode = i.Category?.Code ?? "",
                CategoryName = i.Category?.Name ?? "",
                RarityId = i.RarityId,
                RarityCode = i.Rarity?.Code ?? "",
                RarityName = i.Rarity?.Name ?? "",
                Name = i.Name,
                Icon = i.Icon,
                LevelReq = i.LevelReq,
                Description = i.Description,
                BaseStats = ParseJson(i.BaseStats),
                IsStackable = i.IsStackable,
                MaxStackSize = i.MaxStackSize,
                SellPrice = i.SellPrice
            }).ToList();
        }

        private static HeroTemplateDto MapHeroTemplate(HrkHeroTemplate h)
        {
            return new HeroTemplateDto
            {
                Id = h.Id,
                Name = h.Name,
                Avatar = h.Avatar,
                FactionId = h.FactionId,
                FactionCode = h.Faction?.Code ?? "",
                FactionName = h.Faction?.Name ?? "",
                ClassId = h.ClassId,
                ClassCode = h.Class?.Code ?? "",
                ClassName = h.Class?.Name ?? "",
                RarityId = h.RarityId,
                RarityCode = h.Rarity?.Code ?? "",
                RarityName = h.Rarity?.Name ?? "",
                RarityColorHex = h.Rarity?.ColorHex,
                BaseHp = h.BaseHp,
                BaseAtk = h.BaseAtk,
                BaseDef = h.BaseDef,
                BaseSpd = h.BaseSpd,
                BaseCrit = h.BaseCrit,
                BaseCritDmg = h.BaseCritDmg,
                BaseLifesteal = h.BaseLifesteal,
                BaseAccuracy = h.BaseAccuracy,
                BaseResistance = h.BaseResistance,
                Skills = h.HeroSkills.OrderBy(hs => hs.SkillOrder).Select(hs => MapSkillTemplate(hs.Skill)).ToList()
            };
        }

        private static SkillTemplateDto MapSkillTemplate(HrkSkillTemplate s)
        {
            return new SkillTemplateDto
            {
                Id = s.Id,
                Name = s.Name,
                Icon = s.Icon,
                Description = s.Description,
                Cost = s.Cost,
                CostTypeCode = s.CostType?.Code ?? "",
                CostTypeName = s.CostType?.Name ?? "",
                CategoryCode = s.Category?.Code ?? "",
                CategoryName = s.Category?.Name ?? "",
                DamageTypeCode = s.DamageType?.Code ?? "",
                DamageTypeName = s.DamageType?.Name ?? "",
                EffectTypeCode = s.EffectType?.Code ?? "",
                EffectTypeName = s.EffectType?.Name ?? "",
                IsDebuff = s.EffectType?.IsDebuff ?? false,
                DamageMultiplier = s.DamageMultiplier,
                TargetType = s.TargetType,
                Cooldown = s.Cooldown,
                PhaseDurations = ParsePhaseDurations(s.PhaseDurations)
            };
        }

        private static PhaseDurationDto? ParsePhaseDurations(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<PhaseDurationDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        private static object? ParseJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonSerializer.Deserialize<object>(json); }
            catch { return json; }
        }
    }
}
