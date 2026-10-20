using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using GAME.Application.Services;

namespace GAME.Application.Features.Shared.BattleLab;

public abstract class BattleLabHandlerSupport
{
    protected readonly IGameTemplateCatalog _templates;
    protected readonly IUnitOfWork _unitOfWork;
    protected BattleLabHandlerSupport(IUnitOfWork unitOfWork, IGameTemplateCatalog? templates = null)
    {
        _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
        this._unitOfWork = unitOfWork;
    }

    protected async Task<Dictionary<string, decimal>> ConfigsAsync(CancellationToken ct) => (await _templates.GetAllAsync<HrkBattleConfig>(ct))
        .Where(c => c.IsEnabled)
        .ToDictionary(c => c.Code, c => c.Value, StringComparer.OrdinalIgnoreCase);
    protected async Task<BattleLabCatalogDto> GetCatalogAsync(CancellationToken ct)
    {
        var templates = (await _templates.GetAllAsync<HrkHeroTemplate>(ct))
            .OrderBy(h => h.Id)
            .ToList();
        var skills = (await _templates.GetAllAsync<HrkHeroSkill>(ct))
            .Where(h => h.Skill.IsActive)
            .ToList();
        var configs = await ConfigsAsync(ct);
        return new BattleLabCatalogDto
        {
            Equipment = (await _templates.GetAllAsync<HrkItemTemplate>(ct))
            .Where(i => i.Category.IsEquipment)
            .OrderBy(i => i.CategoryId).ThenBy(i => i.Id)
            .Select(i => new BattleLabItemOptionDto { Id = i.Id, Name = i.Name, Category = i.Category.Code, LevelReq = i.LevelReq })
            .ToList(),
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
