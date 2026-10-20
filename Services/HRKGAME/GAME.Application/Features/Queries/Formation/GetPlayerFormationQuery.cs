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

public record GetPlayerFormationQuery(string FormationName = "Main Team") : IQuery<BaseResponse<FormationDto>>;

public sealed class GetPlayerFormationQueryHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationStatService formationStatService,
    ICombatPowerService combatPowerService,
    IHeroStatCalculationService heroStatCalculationService,
    IGameRequestContext actor, IGameTemplateCatalog? templates = null)
    : FormationHandlerSupport(unitOfWork, gamePlayerService, formationStatService, combatPowerService, heroStatCalculationService, templates), IQueryHandler<GetPlayerFormationQuery, BaseResponse<FormationDto>>
{
    private readonly IGameRequestContext _actor = actor;
    public async Task<BaseResponse<FormationDto>> Handle(GetPlayerFormationQuery request, CancellationToken cancellationToken)
    {
        var userId = _actor.UserId;
        var formation = await ExecuteAsync(userId, request.FormationName, cancellationToken);
        return BaseResponse<FormationDto>.SuccessResponse(formation, "Retrieved player formation successfully.");
    }

    private async Task<FormationDto> ExecuteAsync(string userId, string formationName = "Main Team", CancellationToken cancellationToken = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
        {
            return new FormationDto
            {
                PlayerId = 0,
                FormationName = formationName,
                TotalPower = 0,
                Positions = Enumerable.Range(1, 5)
                .Select(i => new FormationPositionDto { Slot = i, Hero = null }).ToList()
            };
        }

        var selectedFormation = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
            .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.IsSelected && f.IsActive, cancellationToken);
        if (selectedFormation == null)
        {
            selectedFormation = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
                .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.IsActive, cancellationToken);
        }

        if (selectedFormation != null)
        {
            var template = (await _templates.GetAllAsync<HrkFormationTemplate>(cancellationToken))
                .Single(t => t.Id == selectedFormation.FormationTemplateId);
            var detail = await GetFormationDetailAsync(userId, template.Code, cancellationToken);
            if (detail != null)
            {
                return new FormationDto
                {
                    PlayerId = player.Id,
                    FormationName = detail.Name,
                    TotalPower = detail.TotalPower,
                    Positions = detail.Slots.Select(s => new FormationPositionDto { Slot = s.Slot, Hero = s.Hero }).ToList()
                };
            }
        }

        return new FormationDto
        {
            PlayerId = player.Id,
            FormationName = formationName,
            TotalPower = 0,
            Positions = Enumerable.Range(1, 5)
            .Select(i => new FormationPositionDto { Slot = i, Hero = null }).ToList()
        };
    }
}
