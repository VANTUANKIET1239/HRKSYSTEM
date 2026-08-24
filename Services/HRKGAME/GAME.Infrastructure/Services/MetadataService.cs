using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services
{
    public class MetadataService : IMetadataService
    {
        private readonly IUnitOfWork<GameDbContext> _unitOfWork;

        public MetadataService(IUnitOfWork<GameDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<RarityDto>> GetRaritiesAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Repository<HrkRarity>().Query()
                .AsNoTracking()
                .OrderBy(r => r.DisplayOrder)
                .Select(r => new RarityDto
                {
                    Id = r.Id,
                    Code = r.Code,
                    Name = r.Name,
                    ColorHex = r.ColorHex,
                    DisplayOrder = r.DisplayOrder
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<HeroFactionDto>> GetFactionsAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Repository<HrkHeroFaction>().Query()
                .AsNoTracking()
                .OrderBy(f => f.DisplayOrder)
                .Select(f => new HeroFactionDto
                {
                    Id = f.Id,
                    Code = f.Code,
                    Name = f.Name,
                    Icon = f.Icon,
                    DisplayOrder = f.DisplayOrder
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<HeroClassDto>> GetClassesAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Repository<HrkHeroClass>().Query()
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new HeroClassDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Icon = c.Icon,
                    DisplayOrder = c.DisplayOrder
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ItemCategoryDto>> GetItemCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Repository<HrkItemCategory>().Query()
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new ItemCategoryDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Icon = c.Icon,
                    DisplayOrder = c.DisplayOrder,
                    IsEquipment = c.IsEquipment,
                    Description = c.Description
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<SkillEnumsDto> GetSkillEnumsAsync(CancellationToken cancellationToken = default)
        {
            var costTypes = await _unitOfWork.Repository<HrkSkillCostType>().Query()
                .AsNoTracking()
                .Select(x => new LookupItemDto { Id = x.Id, Code = x.Code, Name = x.Name })
                .ToListAsync(cancellationToken);

            var categories = await _unitOfWork.Repository<HrkSkillCategory>().Query()
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new LookupItemDto { Id = x.Id, Code = x.Code, Name = x.Name, DisplayOrder = x.DisplayOrder })
                .ToListAsync(cancellationToken);

            var damageTypes = await _unitOfWork.Repository<HrkSkillDamageType>().Query()
                .AsNoTracking()
                .Select(x => new LookupItemDto { Id = x.Id, Code = x.Code, Name = x.Name })
                .ToListAsync(cancellationToken);

            var effectTypes = await _unitOfWork.Repository<HrkSkillEffectType>().Query()
                .AsNoTracking()
                .Select(x => new LookupItemDto { Id = x.Id, Code = x.Code, Name = x.Name, IsDebuff = x.IsDebuff })
                .ToListAsync(cancellationToken);

            return new SkillEnumsDto
            {
                CostTypes = costTypes,
                Categories = categories,
                DamageTypes = damageTypes,
                EffectTypes = effectTypes
            };
        }
    }
}
