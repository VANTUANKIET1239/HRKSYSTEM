using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using GAME.Application.Services;

namespace GAME.Application.Features.Shared.BattleLab;

public abstract class BattleLabHandlerSupport
{
    protected readonly IUnitOfWork _unitOfWork;
    protected BattleLabHandlerSupport(IUnitOfWork unitOfWork)
    {
        this._unitOfWork = unitOfWork;
    }

    protected Task<Dictionary<string, decimal>> ConfigsAsync(CancellationToken ct) => _unitOfWork.ReadOnlyRepository<HrkBattleConfig>().Query()
        .Where(c => c.IsEnabled)
        .ToDictionaryAsync(c => c.Code, c => c.Value, StringComparer.OrdinalIgnoreCase, ct);
    protected async Task<BattleLabCatalogDto> GetCatalogAsync(CancellationToken ct)
    {
        var templates = await _unitOfWork.ReadOnlyRepository<HrkHeroTemplate>().Query()
            .AsNoTracking()
            .OrderBy(h => h.Id)
            .ToListAsync(ct);
        var skills = await _unitOfWork.ReadOnlyRepository<HrkHeroSkill>().Query()
            .Where(h => h.Skill.IsActive)
            .Include(h => h.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.EffectType)
            .Include(h => h.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.TargetType)
            .Include(h => h.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.Scalings).ThenInclude(s => s.AttributeType)
            .Include(h => h.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.StatModifiers).ThenInclude(s => s.AttributeType)
            .Include(h => h.Skill).ThenInclude(s => s.Effects.Where(e => e.IsActive)).ThenInclude(e => e.Parameters)
            .Include(h => h.Skill).ThenInclude(s => s.AnimationConfig).ThenInclude(a => a!.Phases)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(ct);
        var configs = await ConfigsAsync(ct);
        return new BattleLabCatalogDto
        {
            Equipment = await _unitOfWork.ReadOnlyRepository<HrkItemTemplate>().Query()
            .AsNoTracking()
            .Where(i => i.Category.IsEquipment)
            .OrderBy(i => i.CategoryId).ThenBy(i => i.Id)
            .Select(i => new BattleLabItemOptionDto { Id = i.Id, Name = i.Name, Category = i.Category.Code, LevelReq = i.LevelReq })
            .ToListAsync(ct),
            DefenseConstant = BattleMitigationConfig.Read(configs),
            Heroes = templates.Select(t => new PlayerHeroDto
            {
                Id = t.Id,
                HeroTemplateId = t.Id,
                Name = t.Name,
                Avatar = t.Avatar,
                Level = 1,
                Position = 1,
                Stars = 0,
                Stats = new CalculatedStatsDto { Hp = t.BaseHp, Atk = t.BaseAtk, Def = t.BaseDef, Spd = t.BaseSpd, MagicDamage = t.BaseMagicDamage, MagicResistance = t.BaseMagicResistance, Crit = t.BaseCrit, CritDmg = t.BaseCritDmg, Accuracy = t.BaseAccuracy, Resistance = t.BaseResistance, Lifesteal = t.BaseLifesteal },
                Skills = skills.Where(s => s.HeroTemplateId == t.Id)
            .OrderBy(s => s.SkillOrder)
            .Select(s => GameDtoMapper.MapSkillTemplate(s.Skill)).OfType<SkillTemplateDto>().ToList()
            }).ToList()
        };
    }
}
