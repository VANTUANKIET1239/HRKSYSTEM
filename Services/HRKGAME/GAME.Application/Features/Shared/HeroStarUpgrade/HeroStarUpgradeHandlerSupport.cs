using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Shared.HeroStarUpgrade;

public abstract class HeroStarUpgradeHandlerSupport
{
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IGamePlayerService _gamePlayerService;
    protected readonly IHeroProgressionStatService _progressionStatService;
    protected readonly ICombatPowerService _combatPowerService;
    protected HeroStarUpgradeHandlerSupport(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService, IHeroProgressionStatService progressionStatService, ICombatPowerService combatPowerService)
    {
        this._unitOfWork = unitOfWork;
        this._gamePlayerService = gamePlayerService;
        this._progressionStatService = progressionStatService;
        this._combatPowerService = combatPowerService;
    }

    protected sealed record UpgradeData(

        HrkPlayer Player,

        HrkPlayerHero Hero,

        HrkHeroRarityUpgradeConfig RarityConfig,

        HrkHeroStarUpgradeConfig Config,

        HrkHeroStoneConfig StoneConfig

    );
    protected async Task<UpgradeData> LoadUpgradeDataAsync(string userId, long heroId, CancellationToken cancellationToken)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var hero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
            .Include(x => x.HeroTemplate)
            .SingleOrDefaultAsync(x => x.Id == heroId && x.PlayerId == player.Id && x.IsActive, cancellationToken) ?? throw new BusinessException("HERO_NOT_FOUND", "Không tìm thấy võ tướng.", BusinessErrorKind.NotFound);
        if (hero.Stars >= 5)
        {
            throw new BusinessException("MAX_STARS_REACHED", "Võ tướng đã đạt 5 sao.", BusinessErrorKind.Conflict);
        }

        var rarityConfig = await _unitOfWork.ReadOnlyRepository<HrkHeroRarityUpgradeConfig>().Query()
            .SingleAsync(x => x.RarityId == hero.HeroTemplate.RarityId, cancellationToken);
        var config = await _unitOfWork.ReadOnlyRepository<HrkHeroStarUpgradeConfig>().Query()
            .SingleOrDefaultAsync(x => x.RarityId == hero.HeroTemplate.RarityId && x.CurrentStar == hero.Stars && x.NextStar == hero.Stars + 1 && x.IsEnabled, cancellationToken) ?? throw new InvalidOperationException("Thiếu cấu hình tăng sao.");
        var stoneConfig = await _unitOfWork.ReadOnlyRepository<HrkHeroStoneConfig>().Query()
            .SingleOrDefaultAsync(x => x.HeroTemplateId == hero.HeroTemplateId && x.IsActive, cancellationToken) ?? throw new InvalidOperationException("Thiếu cấu hình đá nhân vật.");
        return new UpgradeData(player, hero, rarityConfig, config, stoneConfig);
    }

    protected async Task<HeroStarUpgradePreviewDto> BuildPreviewAsync(UpgradeData data, CancellationToken cancellationToken)
    {
        var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(data.Player.Id, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy ví người chơi.");
        var bonuses = await GetBonusAttributesAsync(data.Hero.Id, cancellationToken);
        var previousBonus = await GetGrowthBonusAsync(data.Hero.HeroTemplate.RarityId, data.Hero.Stars, cancellationToken);
        var currentStats = _progressionStatService.Calculate(data.Hero.HeroTemplate, data.Hero.Level, data.RarityConfig.StatGrowthRate, previousBonus, bonuses.Select(x => (x.Code, x.Value)));
        var nextStats = _progressionStatService.Calculate(data.Hero.HeroTemplate, data.Hero.Level, data.RarityConfig.StatGrowthRate, data.Config.GrowthBonusPercent, bonuses.Select(x => (x.Code, x.Value)));
        var universalStone = await GetMaterialRequirementAsync(data.Player.Id, data.Config.UniversalStarStoneItemTemplateId, data.Config.UniversalStarStoneQuantity, cancellationToken);
        var heroStone = await GetMaterialRequirementAsync(data.Player.Id, data.StoneConfig.ItemTemplateId, data.Config.HeroStoneQuantity, cancellationToken);
        var preview = new HeroStarUpgradePreviewDto
        {
            HeroId = data.Hero.Id,
            CurrentStar = data.Hero.Stars,
            NextStar = data.Config.NextStar,
            CurrentLevel = data.Hero.Level,
            GoldRequired = data.Config.GoldCost,
            GoldOwned = wallet.Gold,
            UniversalStone = universalStone,
            HeroStone = heroStone,
            CurrentStats = currentStats,
            NextStats = nextStats,
            StatIncrease = _progressionStatService.Subtract(nextStats, currentStats),
            CurrentGrowthRate = data.RarityConfig.StatGrowthRate * (1 + previousBonus),
            NextGrowthRate = data.RarityConfig.StatGrowthRate * (1 + data.Config.GrowthBonusPercent),
            CurrentBonusAttributes = bonuses,
            WillUnlockBonusAttribute = data.Config.ExtraAttributeRollCount > 0,
            CurrentCombatPower = await _combatPowerService.CalculateAsync(currentStats, cancellationToken),
            NextCombatPower = await _combatPowerService.CalculateAsync(nextStats, cancellationToken)
        };
        preview.CanUpgrade = HeroStarMaterialPolicy.CanUpgrade(wallet.Gold, data.Config.GoldCost, universalStone, heroStone);
        if (!preview.CanUpgrade)
        {
            preview.ReasonCode = "INSUFFICIENT_RESOURCES";
            preview.Message = BuildMissingResourceMessage(wallet.Gold < data.Config.GoldCost, !HeroStarMaterialPolicy.IsEnough(universalStone), !HeroStarMaterialPolicy.IsEnough(heroStone));
        }

        return preview;
    }

    protected async Task<decimal> GetGrowthBonusAsync(int rarityId, int star, CancellationToken cancellationToken)
    {
        if (star <= 1)
        {
            return 0m;
        }

        return await _unitOfWork.ReadOnlyRepository<HrkHeroStarUpgradeConfig>().Query()
            .Where(x => x.RarityId == rarityId && x.NextStar == star && x.IsEnabled)
            .Select(x => x.GrowthBonusPercent)
            .FirstOrDefaultAsync(cancellationToken);
    }

    protected async Task<StarMaterialRequirementDto> GetMaterialRequirementAsync(long playerId, int itemTemplateId, int required, CancellationToken cancellationToken)
    {
        var template = await _unitOfWork.ReadOnlyRepository<HrkItemTemplate>().Query()
            .SingleAsync(x => x.Id == itemTemplateId, cancellationToken);
        var owned = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
            .Where(x => x.PlayerId == playerId && x.ItemTemplateId == itemTemplateId && x.IsActive).SumAsync(x => (int?)x.Count, cancellationToken) ?? 0;
        return new StarMaterialRequirementDto
        {
            ItemTemplateId = itemTemplateId,
            Name = template.Name,
            ImagePath = template.ImagePath,
            Required = required,
            Owned = owned
        };
    }

    protected async Task<List<HeroBonusAttributeDto>> GetBonusAttributesAsync(long heroId, CancellationToken cancellationToken)
    {
        return await (
            from bonus in _unitOfWork.ReadOnlyRepository<HrkPlayerHeroBonusAttribute>().Query()
            join attribute in _unitOfWork.ReadOnlyRepository<HrkAttributeType>().Query() on bonus.AttributeTypeId equals attribute.Id
            where bonus.PlayerHeroId == heroId
            orderby bonus.UnlockedAtStar
            select new HeroBonusAttributeDto
            {
                UnlockedAtStar = bonus.UnlockedAtStar,
                Code = attribute.Code,
                Name = attribute.Name,
                Value = bonus.Value,
                IsPercentage = bonus.IsPercentage
            }

        ).ToListAsync(cancellationToken);
    }

    protected static string BuildMissingResourceMessage(bool missingGold, bool missingUniversalStone, bool missingHeroStone)
    {
        var missing = new List<string>();
        if (missingGold)
            missing.Add("vàng");
        if (missingUniversalStone && missingHeroStone)
            missing.Add("đủ một trong hai loại: Đá Tăng Sao hoặc đá nhân vật");
        return "Thiếu " + string.Join(", ", missing);
    }

    protected async Task<HeroStarUpgradePreviewDto> PreviewAsync(string userId, long heroId, CancellationToken cancellationToken = default)
    {
        var data = await LoadUpgradeDataAsync(userId, heroId, cancellationToken);
        return await BuildPreviewAsync(data, cancellationToken);
    }
}
