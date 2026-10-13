using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.HeroUpgrade;

namespace GAME.Application.Features.Queries.HeroUpgrade;

public sealed record GetHeroUpgradePreviewQuery(string UserId, long HeroId) : IQuery<BaseResponse<HeroUpgradePreviewDto>>;

public sealed class GetHeroUpgradePreviewQueryHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IHeroProgressionStatService progressionStats)
    : HeroUpgradeHandlerSupport(unitOfWork, gamePlayerService), IQueryHandler<GetHeroUpgradePreviewQuery, BaseResponse<HeroUpgradePreviewDto>>
{
    private readonly IHeroProgressionStatService _progressionStats = progressionStats;
    public async Task<BaseResponse<HeroUpgradePreviewDto>> Handle(GetHeroUpgradePreviewQuery message, CancellationToken ct) => BaseResponse<HeroUpgradePreviewDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.HeroId, ct));
    private async Task<HeroUpgradePreviewDto> ExecuteAsync(string userId, long heroId, CancellationToken cancellationToken = default)
    {
        var (_, hero, config) = await LoadHeroAsync(userId, heroId, cancellationToken);
        var starBonus = await GetStarBonusAsync(hero, cancellationToken);
        return BuildPreview(hero, config, starBonus);
    }

    private HeroUpgradePreviewDto BuildPreview(HrkPlayerHero hero, HrkHeroRarityUpgradeConfig config, decimal starBonus)
    {
        var nextLevel = Math.Min(hero.Level + 1, config.MaxLevel);
        var current = _progressionStats.Calculate(hero.HeroTemplate, hero.Level, config.StatGrowthRate, starBonus);
        var next = _progressionStats.Calculate(hero.HeroTemplate, nextLevel, config.StatGrowthRate, starBonus);
        return new HeroUpgradePreviewDto
        {
            HeroId = hero.Id,
            CurrentLevel = hero.Level,
            NextLevel = nextLevel,
            MaxLevel = config.MaxLevel,
            GoldCost = hero.Level >= config.MaxLevel ? 0 : GetGoldCost(hero.Level, config),
            MaterialCost = hero.Level >= config.MaxLevel ? 0 : GetMaterialCost(hero.Level, config),
            CurrentStats = current,
            NextStats = next,
            StatIncrease = _progressionStats.Subtract(next, current)
        };
    }
}
