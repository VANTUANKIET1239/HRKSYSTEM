using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Shared.HeroUpgrade;

public abstract class HeroUpgradeHandlerSupport
{
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IGamePlayerService _gamePlayerService;
    protected HeroUpgradeHandlerSupport(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService)
    {
        this._unitOfWork = unitOfWork;
        this._gamePlayerService = gamePlayerService;
    }

    protected async Task<(HrkPlayer Player, HrkPlayerHero Hero, HrkHeroRarityUpgradeConfig Config)> LoadHeroAsync(string userId, long heroId, CancellationToken ct)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Khong tim thay nguoi choi.", BusinessErrorKind.NotFound);
        var hero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
            .Include(x => x.HeroTemplate)
            .FirstOrDefaultAsync(x => x.Id == heroId && x.PlayerId == player.Id && x.IsActive, ct) ?? throw new BusinessException("HERO_NOT_FOUND", "Khong tim thay vo tuong thuoc tai khoan nay.", BusinessErrorKind.NotFound);
        var config = await _unitOfWork.ReadOnlyRepository<HrkHeroRarityUpgradeConfig>().Query()
            .FirstOrDefaultAsync(x => x.RarityId == hero.HeroTemplate.RarityId, ct) ?? throw new InvalidOperationException("Chua co cau hinh nang cap cho pham chat cua vo tuong.");
        return (player, hero, config);
    }

    protected async Task<decimal> GetStarBonusAsync(HrkPlayerHero hero, CancellationToken ct)
    {
        if (hero.Stars <= 1)
            return 0m;
        return await _unitOfWork.ReadOnlyRepository<HrkHeroStarUpgradeConfig>().Query()
            .Where(x => x.RarityId == hero.HeroTemplate.RarityId && x.NextStar == hero.Stars && x.IsEnabled)
            .Select(x => x.GrowthBonusPercent)
            .FirstOrDefaultAsync(ct);
    }

    protected static long GetGoldCost(int currentLevel, HrkHeroRarityUpgradeConfig c) => c.BaseGoldCost + (long)Math.Max(0, currentLevel - 1) * c.GoldCostPerLevel;
    protected static int GetMaterialCost(int currentLevel, HrkHeroRarityUpgradeConfig c) => c.BaseMaterialCost + Math.Max(0, currentLevel - 1) * c.MaterialCostPerLevel;
}
