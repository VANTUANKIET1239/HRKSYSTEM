using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Shared.HeroUpgrade;

public abstract class HeroUpgradeHandlerSupport
{
    protected readonly IGameTemplateCatalog _templates;
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IGamePlayerService _gamePlayerService;
    protected HeroUpgradeHandlerSupport(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService, IGameTemplateCatalog? templates = null)
    {
        _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
        this._unitOfWork = unitOfWork;
        this._gamePlayerService = gamePlayerService;
    }

    protected async Task<(HrkPlayer Player, HrkPlayerHero Hero, HrkHeroRarityUpgradeConfig Config, HrkHeroTemplate Template)> LoadHeroAsync(string userId, long heroId, CancellationToken ct)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Khong tim thay nguoi choi.", BusinessErrorKind.NotFound);
        var hero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
            .FirstOrDefaultAsync(x => x.Id == heroId && x.PlayerId == player.Id && x.IsActive, ct) ?? throw new BusinessException("HERO_NOT_FOUND", "Khong tim thay vo tuong thuoc tai khoan nay.", BusinessErrorKind.NotFound);
        var template = (await _templates.GetAllAsync<HrkHeroTemplate>(ct)).Single(x => x.Id == hero.HeroTemplateId);
        var config = (await _templates.GetAllAsync<HrkHeroRarityUpgradeConfig>(ct))
            .FirstOrDefault(x => x.RarityId == template.RarityId) ?? throw new InvalidOperationException("Chua co cau hinh nang cap cho pham chat cua vo tuong.");
        return (player, hero, config, template);
    }

    protected async Task<decimal> GetStarBonusAsync(HrkPlayerHero hero, HrkHeroTemplate template, CancellationToken ct)
    {
        if (hero.Stars <= 1)
            return 0m;
        return (await _templates.GetAllAsync<HrkHeroStarUpgradeConfig>(ct))
            .Where(x => x.RarityId == template.RarityId && x.NextStar == hero.Stars && x.IsEnabled)
            .Select(x => x.GrowthBonusPercent)
            .FirstOrDefault();
    }

    protected static long GetGoldCost(int currentLevel, HrkHeroRarityUpgradeConfig c) => c.BaseGoldCost + (long)Math.Max(0, currentLevel - 1) * c.GoldCostPerLevel;
    protected static int GetMaterialCost(int currentLevel, HrkHeroRarityUpgradeConfig c) => c.BaseMaterialCost + Math.Max(0, currentLevel - 1) * c.MaterialCostPerLevel;
}
