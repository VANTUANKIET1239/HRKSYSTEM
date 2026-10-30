using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Formation;

namespace GAME.Application.Features.Queries.Formation;

public sealed class GetPlayerFormationsQueryHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationStatService formationStatService,
    ICombatPowerService combatPowerService,
    IHeroStatCalculationService heroStatCalculationService,
    IFormationPowerQueryService formationPowerQueryService, IGameTemplateCatalog? templates = null)
    : FormationHandlerSupport(unitOfWork, gamePlayerService, formationStatService, combatPowerService, heroStatCalculationService, templates), IQueryHandler<GetPlayerFormationsQuery, BaseResponse<List<PlayerFormationSummaryDto>>>
{
    private readonly IFormationPowerQueryService _formationPowerQueryService = formationPowerQueryService;
    public async Task<BaseResponse<List<PlayerFormationSummaryDto>>> Handle(GetPlayerFormationsQuery message, CancellationToken ct) => BaseResponse<List<PlayerFormationSummaryDto>>.SuccessResponse(await ExecuteAsync(message.UserId, ct));
    private async Task<List<PlayerFormationSummaryDto>> ExecuteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await GetPlayerRequiredAsync(userId, cancellationToken);
        var templates = (await _templates.GetAllAsync<HrkFormationTemplate>(cancellationToken))
            .Where(t => t.IsEnabled)
            .OrderBy(t => t.DisplayOrder)
            .ToList();
        var playerFormations = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
            .Where(f => f.PlayerId == player.Id && f.IsActive)
            .ToListAsync(cancellationToken);
        var pfByTemplateId = playerFormations.ToDictionary(f => f.FormationTemplateId);
        var result = new List<PlayerFormationSummaryDto>();
        foreach (var tmpl in templates)
        {
            pfByTemplateId.TryGetValue(tmpl.Id, out var pf);
            int level = pf?.Level ?? 1;
            bool isSelected = pf?.IsSelected ?? (tmpl.Code == "LUC_DO");
            var currentLevelCfg = tmpl.LevelConfigs.FirstOrDefault(c => c.Level == level);
            var currentBonus = _formationStatService.ParseBonus(currentLevelCfg?.StatBonusJson);
            var powerResult = await _formationPowerQueryService.CalculateFormationPowerAsync(player.Id, tmpl.Code, null, cancellationToken);
            result.Add(new PlayerFormationSummaryDto { TemplateId = tmpl.Id, Code = tmpl.Code, Name = tmpl.Name, Description = tmpl.Description, ImagePath = tmpl.ImagePath, Level = level, MaxLevel = tmpl.MaxLevel, IsUnlocked = true, IsSelected = isSelected, BaseHeroPower = powerResult.BaseHeroPower, FormationBonusPower = powerResult.FormationBonusPower, TotalPower = powerResult.TotalPower, HeroCount = powerResult.HeroCount, CurrentBonus = currentBonus, DisplayOrder = tmpl.DisplayOrder });
        }

        return result;
    }
}
