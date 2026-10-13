using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.HeroUpgrade;

namespace GAME.Application.Features.Commands.HeroUpgrade;

public sealed record UpgradeHeroCommand(

    string UserId,

    long HeroId,

    int Levels

)

    : ICommand<BaseResponse<PlayerHeroDetailDto>>;

public sealed class UpgradeHeroCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    ILevelExperienceService levelExperienceService,
    IHeroProgressionStatService progressionStats,
    IPlayerReadModels playerReadModels)
    : HeroUpgradeHandlerSupport(unitOfWork, gamePlayerService), ICommandHandler<UpgradeHeroCommand, BaseResponse<PlayerHeroDetailDto>>
{
    private readonly ILevelExperienceService _levelExperienceService = levelExperienceService;
    private readonly IHeroProgressionStatService _progressionStats = progressionStats;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly IPlayerReadModels _playerReadModels = playerReadModels;
    public async Task<BaseResponse<PlayerHeroDetailDto>> Handle(UpgradeHeroCommand message, CancellationToken ct) => BaseResponse<PlayerHeroDetailDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.HeroId, message.Levels, ct));
    private async Task<PlayerHeroDetailDto> ExecuteAsync(string userId, long heroId, int levels, CancellationToken cancellationToken = default)
    {
        if (levels is < 1 or > 50)
            throw new BusinessException("INVALID_UPGRADE_LEVELS", "So cap nang phai nam trong khoang 1 den 50.", BusinessErrorKind.Validation);
        var (player, hero, config) = await LoadHeroAsync(userId, heroId, cancellationToken);
        if (hero.Level >= config.MaxLevel)
            throw new BusinessException("MAX_LEVEL_REACHED", "Vo tuong da dat cap toi da.", BusinessErrorKind.Conflict);
        var actualLevels = Math.Min(levels, config.MaxLevel - hero.Level);
        long goldCost = 0;
        var materialCost = 0;
        for (var i = 0; i < actualLevels; i++)
        {
            goldCost += GetGoldCost(hero.Level + i, config);
            materialCost += GetMaterialCost(hero.Level + i, config);
        }

        var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken) ?? throw new InvalidOperationException("Khong tim thay vi nguoi choi.");
        if (!wallet.HasEnoughGold(goldCost))
            throw new BusinessException("INSUFFICIENT_GOLD", "Khong du Vang de nang cap vo tuong.", BusinessErrorKind.Conflict);
        if (wallet.UpgradeMaterials < materialCost)
            throw new BusinessException("INSUFFICIENT_MATERIALS", "Khong du Da nang cap vo tuong.", BusinessErrorKind.Conflict);
        var starBonus = await GetStarBonusAsync(hero, cancellationToken);
        var nextStats = _progressionStats.Calculate(hero.HeroTemplate, hero.Level + actualLevels, config.StatGrowthRate, starBonus);
        wallet.DeductGold(goldCost);
        wallet.UpgradeMaterials -= materialCost;
        wallet.UpdatedOn = DateTime.UtcNow;
        hero.Level += actualLevels;
        hero.Exp = 0;
        var expRequirement = await _levelExperienceService.GetHeroRequirementAsync(hero.Level, cancellationToken);
        hero.MaxExp = expRequirement.IsMaxLevel ? 0 : expRequirement.RequiredExp;
        hero.CurrentStats = JsonSerializer.Serialize(nextStats, JsonOptions);
        hero.UpdatedOn = DateTime.UtcNow;
        _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);
        _unitOfWork.Repository<HrkPlayerHero>().Update(hero);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await _playerReadModels.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken) ?? throw new KeyNotFoundException("Khong tim thay vo tuong sau khi nang cap.");
    }
}
