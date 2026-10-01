using Core.Common.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;

namespace GAME.Infrastructure.Services;

public class HeroStarUpgradeService : IHeroStarUpgradeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHeroProgressionStatService _progressionStatService;
    private readonly ICombatPowerService _combatPowerService;

    public HeroStarUpgradeService(
        IUnitOfWork unitOfWork,
        IHeroProgressionStatService progressionStatService,
        ICombatPowerService combatPowerService)
    {
        _unitOfWork = unitOfWork;
        _progressionStatService = progressionStatService;
        _combatPowerService = combatPowerService;
    }

    public async Task<HeroStarUpgradePreviewDto> PreviewAsync(
        string userId,
        long heroId,
        CancellationToken cancellationToken = default)
    {
        var data = await LoadUpgradeDataAsync(userId, heroId, cancellationToken);
        return await BuildPreviewAsync(data, cancellationToken);
    }

    public async Task<HeroStarUpgradePreviewDto> UpgradeAsync(
        string userId,
        long heroId,
        Guid requestId,
        CancellationToken cancellationToken = default,
        string? materialType = null)
    {
        if (requestId == Guid.Empty)
        {
            throw new InvalidOperationException("RequestId không hợp lệ.");
        }

        var existingHistory = await _unitOfWork
            .ReadOnlyRepository<HrkHeroStarUpgradeHistory>()
            .Query()
            .FirstOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);

        if (existingHistory != null)
        {
            return await PreviewAsync(userId, heroId, cancellationToken);
        }

        HeroStarUpgradePreviewDto? result = null;

        await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var data = await LoadUpgradeDataAsync(
                    userId, heroId, cancellationToken);
                var preview = await BuildPreviewAsync(data, cancellationToken);

                if (!preview.CanUpgrade)
                {
                    throw new InvalidOperationException(
                        preview.Message ?? "Không thể tăng sao.");
                }

                var wallet = await _unitOfWork.Repository<HrkPlayerWallet>()
                    .Query()
                    .SingleAsync(
                        x => x.PlayerId == data.Player.Id,
                        cancellationToken);

                wallet.DeductGold(data.Config.GoldCost);

                var useHeroStone = HeroStarMaterialPolicy.UseHeroStone(
                    materialType, preview.UniversalStone, preview.HeroStone);
                var material = useHeroStone ? preview.HeroStone : preview.UniversalStone;

                await ConsumeMaterialAsync(
                    data.Player.Id,
                    material.ItemTemplateId,
                    material.Required,
                    cancellationToken);

                var rolledAttributes = new List<HeroBonusAttributeDto>();
                for (var index = 0;
                     index < data.Config.ExtraAttributeRollCount;
                     index++)
                {
                    rolledAttributes.Add(await RollAttributeAsync(
                        data.Hero,
                        data.Config.NextStar,
                        requestId,
                        index,
                        cancellationToken));
                }

                data.Hero.Stars = data.Config.NextStar;
                // AuraTier is the legacy progression tier and is constrained by
                // CHK_HRK_PlayerHeroes_AuraTier to the range 1..4. Star visuals
                // use Stars/HRK_HeroStarAuraConfigs independently.
                data.Hero.AuraTier = (byte)Math.Clamp(data.Hero.Stars, 1, 4);

                var allBonuses = await GetBonusAttributesAsync(
                    data.Hero.Id, cancellationToken);
                allBonuses.AddRange(rolledAttributes);

                var calculatedStats = _progressionStatService.Calculate(
                    data.Hero.HeroTemplate,
                    data.Hero.Level,
                    data.RarityConfig.StatGrowthRate,
                    data.Config.GrowthBonusPercent,
                    allBonuses.Select(x => (x.Code, x.Value)));

                data.Hero.CurrentStats = JsonSerializer.Serialize(calculatedStats);
                data.Hero.Power = await _combatPowerService.CalculateAsync(
                    calculatedStats, cancellationToken);
                data.Hero.UpdatedOn = DateTime.UtcNow;

                await _unitOfWork.Repository<HrkHeroStarUpgradeHistory>()
                    .AddAsync(new HrkHeroStarUpgradeHistory
                    {
                        RequestId = requestId,
                        PlayerId = data.Player.Id,
                        PlayerHeroId = data.Hero.Id,
                        OldStar = data.Config.CurrentStar,
                        NewStar = data.Config.NextStar,
                        GoldCost = data.Config.GoldCost,
                        UniversalStoneQuantity =
                            useHeroStone ? 0 : data.Config.UniversalStarStoneQuantity,
                        HeroStoneItemTemplateId = data.StoneConfig.ItemTemplateId,
                        HeroStoneQuantity = useHeroStone ? data.Config.HeroStoneQuantity : 0,
                        RolledAttributesJson = JsonSerializer.Serialize(
                            rolledAttributes)
                    });

                _unitOfWork.Repository<HrkPlayerHero>().Update(data.Hero);
                _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                result = await BuildCompletedResultAsync(
                    data.Hero,
                    data.Config,
                    wallet,
                    calculatedStats,
                    allBonuses,
                    cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        });

        return result ?? throw new InvalidOperationException(
            "Giao dịch tăng sao không hoàn tất.");
    }

    private async Task<UpgradeData> LoadUpgradeDataAsync(
        string userId,
        long heroId,
        CancellationToken cancellationToken)
    {
        var player = await _unitOfWork.Repository<HrkPlayer>()
            .Query()
            .SingleOrDefaultAsync(
                x => x.UserId == userId && x.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy người chơi.");

        var hero = await _unitOfWork.Repository<HrkPlayerHero>()
            .Query()
            .Include(x => x.HeroTemplate)
            .SingleOrDefaultAsync(
                x => x.Id == heroId &&
                     x.PlayerId == player.Id &&
                     x.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy võ tướng.");

        if (hero.Stars >= 5)
        {
            throw new InvalidOperationException("Võ tướng đã đạt 5 sao.");
        }

        var rarityConfig = await _unitOfWork
            .ReadOnlyRepository<HrkHeroRarityUpgradeConfig>()
            .Query()
            .SingleAsync(
                x => x.RarityId == hero.HeroTemplate.RarityId,
                cancellationToken);

        var config = await _unitOfWork
            .ReadOnlyRepository<HrkHeroStarUpgradeConfig>()
            .Query()
            .SingleOrDefaultAsync(
                x => x.RarityId == hero.HeroTemplate.RarityId &&
                     x.CurrentStar == hero.Stars &&
                     x.NextStar == hero.Stars + 1 &&
                     x.IsEnabled,
                cancellationToken)
            ?? throw new InvalidOperationException("Thiếu cấu hình tăng sao.");

        var stoneConfig = await _unitOfWork
            .ReadOnlyRepository<HrkHeroStoneConfig>()
            .Query()
            .SingleOrDefaultAsync(
                x => x.HeroTemplateId == hero.HeroTemplateId && x.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException("Thiếu cấu hình đá nhân vật.");

        return new UpgradeData(
            player, hero, rarityConfig, config, stoneConfig);
    }

    private async Task<HeroStarUpgradePreviewDto> BuildPreviewAsync(
        UpgradeData data,
        CancellationToken cancellationToken)
    {
        var wallet = await _unitOfWork.ReadOnlyRepository<HrkPlayerWallet>()
            .Query()
            .SingleAsync(
                x => x.PlayerId == data.Player.Id,
                cancellationToken);
        var bonuses = await GetBonusAttributesAsync(
            data.Hero.Id, cancellationToken);

        var previousBonus = await GetGrowthBonusAsync(
            data.Hero.HeroTemplate.RarityId,
            data.Hero.Stars,
            cancellationToken);

        var currentStats = _progressionStatService.Calculate(
            data.Hero.HeroTemplate,
            data.Hero.Level,
            data.RarityConfig.StatGrowthRate,
            previousBonus,
            bonuses.Select(x => (x.Code, x.Value)));
        var nextStats = _progressionStatService.Calculate(
            data.Hero.HeroTemplate,
            data.Hero.Level,
            data.RarityConfig.StatGrowthRate,
            data.Config.GrowthBonusPercent,
            bonuses.Select(x => (x.Code, x.Value)));

        var universalStone = await GetMaterialRequirementAsync(
            data.Player.Id,
            data.Config.UniversalStarStoneItemTemplateId,
            data.Config.UniversalStarStoneQuantity,
            cancellationToken);
        var heroStone = await GetMaterialRequirementAsync(
            data.Player.Id,
            data.StoneConfig.ItemTemplateId,
            data.Config.HeroStoneQuantity,
            cancellationToken);

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
            StatIncrease = _progressionStatService.Subtract(
                nextStats, currentStats),
            CurrentGrowthRate = data.RarityConfig.StatGrowthRate *
                                (1 + previousBonus),
            NextGrowthRate = data.RarityConfig.StatGrowthRate *
                             (1 + data.Config.GrowthBonusPercent),
            CurrentBonusAttributes = bonuses,
            WillUnlockBonusAttribute = data.Config.ExtraAttributeRollCount > 0,
            CurrentCombatPower = await _combatPowerService.CalculateAsync(
                currentStats, cancellationToken),
            NextCombatPower = await _combatPowerService.CalculateAsync(
                nextStats, cancellationToken)
        };

        preview.CanUpgrade = HeroStarMaterialPolicy.CanUpgrade(
            wallet.Gold, data.Config.GoldCost, universalStone, heroStone);

        if (!preview.CanUpgrade)
        {
            preview.ReasonCode = "INSUFFICIENT_RESOURCES";
            preview.Message = BuildMissingResourceMessage(
                wallet.Gold < data.Config.GoldCost,
                !HeroStarMaterialPolicy.IsEnough(universalStone),
                !HeroStarMaterialPolicy.IsEnough(heroStone));
        }

        return preview;
    }

    private async Task<HeroStarUpgradePreviewDto> BuildCompletedResultAsync(
        HrkPlayerHero hero,
        HrkHeroStarUpgradeConfig config,
        HrkPlayerWallet wallet,
        CalculatedStatsDto calculatedStats,
        List<HeroBonusAttributeDto> bonuses,
        CancellationToken cancellationToken)
    {
        return new HeroStarUpgradePreviewDto
        {
            HeroId = hero.Id,
            CurrentStar = hero.Stars,
            NextStar = hero.Stars,
            CurrentLevel = hero.Level,
            GoldRequired = 0,
            GoldOwned = wallet.Gold,
            CurrentStats = calculatedStats,
            NextStats = calculatedStats,
            CurrentBonusAttributes = bonuses,
            CurrentCombatPower = await _combatPowerService.CalculateAsync(
                calculatedStats, cancellationToken),
            NextCombatPower = hero.Power,
            CanUpgrade = hero.Stars < 5,
            Message = $"Đã tăng võ tướng lên {config.NextStar} sao."
        };
    }

    private async Task<decimal> GetGrowthBonusAsync(
        int rarityId,
        int star,
        CancellationToken cancellationToken)
    {
        if (star <= 1)
        {
            return 0m;
        }

        return await _unitOfWork
            .ReadOnlyRepository<HrkHeroStarUpgradeConfig>()
            .Query()
            .Where(x => x.RarityId == rarityId &&
                        x.NextStar == star &&
                        x.IsEnabled)
            .Select(x => x.GrowthBonusPercent)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<StarMaterialRequirementDto> GetMaterialRequirementAsync(
        long playerId,
        int itemTemplateId,
        int required,
        CancellationToken cancellationToken)
    {
        var template = await _unitOfWork
            .ReadOnlyRepository<HrkItemTemplate>()
            .Query()
            .SingleAsync(x => x.Id == itemTemplateId, cancellationToken);
        var owned = await _unitOfWork
            .ReadOnlyRepository<HrkPlayerInventory>()
            .Query()
            .Where(x => x.PlayerId == playerId &&
                        x.ItemTemplateId == itemTemplateId &&
                        x.IsActive)
            .SumAsync(x => (int?)x.Count, cancellationToken) ?? 0;

        return new StarMaterialRequirementDto
        {
            ItemTemplateId = itemTemplateId,
            Name = template.Name,
            ImagePath = template.ImagePath,
            Required = required,
            Owned = owned
        };
    }

    private async Task ConsumeMaterialAsync(
        long playerId,
        int itemTemplateId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<HrkPlayerInventory>();
        var stacks = await repository.Query()
            .Where(x => x.PlayerId == playerId &&
                        x.ItemTemplateId == itemTemplateId &&
                        x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var remaining = quantity;
        foreach (var stack in stacks)
        {
            var consumed = Math.Min(remaining, stack.Count);
            stack.ConsumeQuantity(consumed);
            repository.Update(stack);
            remaining -= consumed;

            if (remaining == 0)
            {
                return;
            }
        }

        throw new InvalidOperationException("Nguyên liệu không đủ.");
    }

    private async Task<List<HeroBonusAttributeDto>> GetBonusAttributesAsync(
        long heroId,
        CancellationToken cancellationToken)
    {
        return await (
            from bonus in _unitOfWork
                .ReadOnlyRepository<HrkPlayerHeroBonusAttribute>().Query()
            join attribute in _unitOfWork
                .ReadOnlyRepository<HrkAttributeType>().Query()
                on bonus.AttributeTypeId equals attribute.Id
            where bonus.PlayerHeroId == heroId
            orderby bonus.UnlockedAtStar
            select new HeroBonusAttributeDto
            {
                UnlockedAtStar = bonus.UnlockedAtStar,
                Code = attribute.Code,
                Name = attribute.Name,
                Value = bonus.Value,
                IsPercentage = bonus.IsPercentage
            }).ToListAsync(cancellationToken);
    }

    private async Task<HeroBonusAttributeDto> RollAttributeAsync(
        HrkPlayerHero hero,
        int star,
        Guid seed,
        int offset,
        CancellationToken cancellationToken)
    {
        var pool = await _unitOfWork
            .ReadOnlyRepository<HrkHeroStarAttributePool>()
            .Query()
            .Where(x => x.RarityId == hero.HeroTemplate.RarityId &&
                        x.IsEnabled)
            .ToListAsync(cancellationToken);

        if (pool.Count == 0)
        {
            throw new InvalidOperationException(
                "Pool thuộc tính sao đang trống.");
        }

        var (selected, value) = HeroStarRollCalculator.Roll(pool, seed, offset);

        await _unitOfWork.Repository<HrkPlayerHeroBonusAttribute>()
            .AddAsync(new HrkPlayerHeroBonusAttribute
            {
                PlayerHeroId = hero.Id,
                UnlockedAtStar = star,
                AttributeTypeId = selected.AttributeTypeId,
                Value = value,
                IsPercentage = selected.IsPercentage,
                RollSeed = seed
            });

        var attribute = await _unitOfWork
            .ReadOnlyRepository<HrkAttributeType>()
            .Query()
            .SingleAsync(
                x => x.Id == selected.AttributeTypeId,
                cancellationToken);

        return new HeroBonusAttributeDto
        {
            UnlockedAtStar = star,
            Code = attribute.Code,
            Name = attribute.Name,
            Value = value,
            IsPercentage = selected.IsPercentage
        };
    }

    private static string BuildMissingResourceMessage(
        bool missingGold,
        bool missingUniversalStone,
        bool missingHeroStone)
    {
        var missing = new List<string>();
        if (missingGold) missing.Add("vàng");
        if (missingUniversalStone && missingHeroStone)
            missing.Add("đủ một trong hai loại: Đá Tăng Sao hoặc đá nhân vật");
        return "Thiếu " + string.Join(", ", missing);
    }

    private sealed record UpgradeData(
        HrkPlayer Player,
        HrkPlayerHero Hero,
        HrkHeroRarityUpgradeConfig RarityConfig,
        HrkHeroStarUpgradeConfig Config,
        HrkHeroStoneConfig StoneConfig);
}
