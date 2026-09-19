using Core.Common.Repositories;
using GAME.Application.Common.Helpers;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CatalogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<HeroTemplateDto>> GetHeroTemplatesAsync(CancellationToken cancellationToken = default)
        {
            var heroes = await _unitOfWork.ReadOnlyRepository<HrkHeroTemplate>().Query()
                .Include(h => h.Faction)
                .Include(h => h.Class)
                .Include(h => h.Rarity)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
                .ToListAsync(cancellationToken);

            return heroes
                .Select(h => GameDtoMapper.MapHeroTemplate(h)!)
                .Where(dto => dto != null)
                .ToList();
        }

        public async Task<HeroTemplateDto?> GetHeroTemplateByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var hero = await _unitOfWork.ReadOnlyRepository<HrkHeroTemplate>().Query()
                .Include(h => h.Faction)
                .Include(h => h.Class)
                .Include(h => h.Rarity)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
                .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
                .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

            return hero != null ? GameDtoMapper.MapHeroTemplate(hero) : null;
        }

        public async Task<List<SkillTemplateDto>> GetSkillTemplatesAsync(CancellationToken cancellationToken = default)
        {
            var skills = await _unitOfWork.ReadOnlyRepository<HrkSkillTemplate>().Query()
                .Include(s => s.Effects).ThenInclude(e => e.EffectType)
                .Include(s => s.Effects).ThenInclude(e => e.TargetType)
                .Include(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
                .Include(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
                .ToListAsync(cancellationToken);

            return skills
                .Select(s => GameDtoMapper.MapSkillTemplate(s)!)
                .Where(dto => dto != null)
                .ToList();
        }

        public async Task<List<ItemTemplateDto>> GetItemTemplatesAsync(int? categoryId, int? rarityId, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.ReadOnlyRepository<HrkItemTemplate>().Query()
                .Include(i => i.Category)
                .Include(i => i.Rarity)
                .Include(i => i.Attributes).ThenInclude(a => a.AttributeType)
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
                Code = i.Code ?? "",
                CategoryId = i.CategoryId,
                CategoryCode = i.Category?.Code ?? "",
                CategoryName = i.Category?.Name ?? "",
                RarityId = i.RarityId,
                RarityCode = i.Rarity?.Code ?? "",
                RarityName = i.Rarity?.Name ?? "",
                Name = i.Name,
                ImagePath = i.ImagePath,
                Icon = i.ImagePath ?? i.Icon ?? "",
                LevelReq = i.LevelReq,
                Description = i.Description,
                MetadataJson = i.MetadataJson,
                Attributes = i.Attributes?.Select(GameDtoMapper.MapItemAttribute).ToList() ?? new List<ItemAttributeDto>(),
                BaseStats = i.Attributes != null && i.Attributes.Count > 0 
                    ? i.Attributes.ToDictionary(a => a.AttributeType?.Code ?? $"ATTR_{a.AttributeTypeId}", a => (object)a.Value)
                    : GameJsonHelper.ParseJson(i.MetadataJson),
                IsStackable = i.IsStackable,
                MaxStackSize = i.MaxStackSize,
                SellPrice = i.SellPrice
            }).ToList();
        }
    }
}
