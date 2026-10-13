using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.HeroStarUpgrade;

namespace GAME.Application.Features.Queries.HeroStarUpgrade;

public sealed record GetHeroStarUpgradePreviewQuery(string UserId, long HeroId) : IQuery<BaseResponse<HeroStarUpgradePreviewDto>>;

public sealed class GetHeroStarUpgradePreviewQueryHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IHeroProgressionStatService progressionStatService,
    ICombatPowerService combatPowerService)
    : HeroStarUpgradeHandlerSupport(unitOfWork, gamePlayerService, progressionStatService, combatPowerService), IQueryHandler<GetHeroStarUpgradePreviewQuery, BaseResponse<HeroStarUpgradePreviewDto>>
{
    public async Task<BaseResponse<HeroStarUpgradePreviewDto>> Handle(GetHeroStarUpgradePreviewQuery message, CancellationToken ct) => BaseResponse<HeroStarUpgradePreviewDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.HeroId, ct));
    private Task<HeroStarUpgradePreviewDto> ExecuteAsync(string userId, long heroId, CancellationToken cancellationToken = default) => PreviewAsync(userId, heroId, cancellationToken);
}
