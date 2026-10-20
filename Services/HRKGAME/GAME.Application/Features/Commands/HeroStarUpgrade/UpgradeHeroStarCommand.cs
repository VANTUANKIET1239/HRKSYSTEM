using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.HeroStarUpgrade;

namespace GAME.Application.Features.Commands.HeroStarUpgrade;

public sealed record UpgradeHeroStarCommand(

    string UserId,

    long HeroId,

    Guid RequestId,

    string? MaterialType

)

    : ICommand<BaseResponse<HeroStarUpgradePreviewDto>>;

public sealed class UpgradeHeroStarCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IHeroProgressionStatService progressionStatService,
    ICombatPowerService combatPowerService, IGameTemplateCatalog? templates = null)
    : HeroStarUpgradeHandlerSupport(unitOfWork, gamePlayerService, progressionStatService, combatPowerService, templates), ICommandHandler<UpgradeHeroStarCommand, BaseResponse<HeroStarUpgradePreviewDto>>
{
    public async Task<BaseResponse<HeroStarUpgradePreviewDto>> Handle(UpgradeHeroStarCommand message, CancellationToken ct) => BaseResponse<HeroStarUpgradePreviewDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.HeroId, message.RequestId, ct, message.MaterialType));
    private async Task<HeroStarUpgradePreviewDto> ExecuteAsync(string userId, long heroId, Guid requestId, CancellationToken cancellationToken = default, string? materialType = null)
    {
        if (requestId == Guid.Empty)
        {
            throw new BusinessException("INVALID_REQUEST_ID", "RequestId không hợp lệ.", BusinessErrorKind.Validation);
        }

        var existingHistory = await _unitOfWork.ReadOnlyRepository<HrkHeroStarUpgradeHistory>().Query()
            .FirstOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
        if (existingHistory != null)
        {
            return await PreviewAsync(userId, heroId, cancellationToken);
        }

        HeroStarUpgradePreviewDto? result = null;
        await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var data = await LoadUpgradeDataAsync(userId, heroId, cancellationToken);
                var preview = await BuildPreviewAsync(data, cancellationToken);
                if (!preview.CanUpgrade)
                {
                    throw new BusinessException("UPGRADE_NOT_ALLOWED", preview.Message ?? "Không thể tăng sao.", BusinessErrorKind.Conflict);
                }

                var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(data.Player.Id, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy ví người chơi.");
                wallet.DeductGold(data.Config.GoldCost);
                var useHeroStone = HeroStarMaterialPolicy.UseHeroStone(materialType, preview.UniversalStone, preview.HeroStone);
                var material = useHeroStone ? preview.HeroStone : preview.UniversalStone;
                await ConsumeMaterialAsync(data.Player.Id, material.ItemTemplateId, material.Required, cancellationToken);
                var rolledAttributes = new List<HeroBonusAttributeDto>();
                for (var index = 0; index < data.Config.ExtraAttributeRollCount; index++)
                {
                    rolledAttributes.Add(await RollAttributeAsync(data.Hero, data.Template.RarityId, data.Config.NextStar, requestId, index, cancellationToken));
                }

                data.Hero.Stars = data.Config.NextStar;
                // AuraTier is the legacy progression tier and is constrained by
                // CHK_HRK_PlayerHeroes_AuraTier to the range 1..4. Star visuals
                // use Stars/HRK_HeroStarAuraConfigs independently.
                data.Hero.AuraTier = (byte)Math.Clamp(data.Hero.Stars, 1, 4);
                var allBonuses = await GetBonusAttributesAsync(data.Hero.Id, cancellationToken);
                allBonuses.AddRange(rolledAttributes);
                var calculatedStats = _progressionStatService.Calculate(data.Template, data.Hero.Level, data.RarityConfig.StatGrowthRate, data.Config.GrowthBonusPercent, allBonuses.Select(x => (x.Code, x.Value)));
                data.Hero.CurrentStats = JsonSerializer.Serialize(calculatedStats);
                data.Hero.Power = await _combatPowerService.CalculateAsync(calculatedStats, cancellationToken);
                data.Hero.UpdatedOn = DateTime.UtcNow;
                await _unitOfWork.Repository<HrkHeroStarUpgradeHistory>().AddAsync(new HrkHeroStarUpgradeHistory { RequestId = requestId, PlayerId = data.Player.Id, PlayerHeroId = data.Hero.Id, OldStar = data.Config.CurrentStar, NewStar = data.Config.NextStar, GoldCost = data.Config.GoldCost, UniversalStoneQuantity = useHeroStone ? 0 : data.Config.UniversalStarStoneQuantity, HeroStoneItemTemplateId = data.StoneConfig.ItemTemplateId, HeroStoneQuantity = useHeroStone ? data.Config.HeroStoneQuantity : 0, RolledAttributesJson = JsonSerializer.Serialize(rolledAttributes) });
                _unitOfWork.Repository<HrkPlayerHero>().Update(data.Hero);
                _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                result = await BuildCompletedResultAsync(data.Hero, data.Config, wallet, calculatedStats, allBonuses, cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }, cancellationToken);
        return result ?? throw new InvalidOperationException("Giao dịch tăng sao không hoàn tất.");
    }

    private async Task<HeroStarUpgradePreviewDto> BuildCompletedResultAsync(HrkPlayerHero hero, HrkHeroStarUpgradeConfig config, HrkPlayerWallet wallet, CalculatedStatsDto calculatedStats, List<HeroBonusAttributeDto> bonuses, CancellationToken cancellationToken)
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
            CurrentCombatPower = await _combatPowerService.CalculateAsync(calculatedStats, cancellationToken),
            NextCombatPower = hero.Power,
            CanUpgrade = hero.Stars < 5,
            Message = $"Đã tăng võ tướng lên {config.NextStar} sao."
        };
    }

    private async Task ConsumeMaterialAsync(long playerId, int itemTemplateId, int quantity, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<HrkPlayerInventory>();
        var stacks = await repository.Query()
            .Where(x => x.PlayerId == playerId && x.ItemTemplateId == itemTemplateId && x.IsActive)
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

        throw new BusinessException("INSUFFICIENT_MATERIALS", "Nguyên liệu không đủ.", BusinessErrorKind.Conflict);
    }

    private async Task<HeroBonusAttributeDto> RollAttributeAsync(HrkPlayerHero hero, int rarityId, int star, Guid seed, int offset, CancellationToken cancellationToken)
    {
        var pool = (await _templates.GetAllAsync<HrkHeroStarAttributePool>(cancellationToken))
            .Where(x => x.RarityId == rarityId && x.IsEnabled)
            .ToList();
        if (pool.Count == 0)
        {
            throw new InvalidOperationException("Pool thuộc tính sao đang trống.");
        }

        var (selected, value) = HeroStarRollCalculator.Roll(pool, seed, offset);
        await _unitOfWork.Repository<HrkPlayerHeroBonusAttribute>().AddAsync(new HrkPlayerHeroBonusAttribute { PlayerHeroId = hero.Id, UnlockedAtStar = star, AttributeTypeId = selected.AttributeTypeId, Value = value, IsPercentage = selected.IsPercentage, RollSeed = seed });
        var attribute = (await _templates.GetAllAsync<HrkAttributeType>(cancellationToken))
            .Single(x => x.Id == selected.AttributeTypeId);
        return new HeroBonusAttributeDto
        {
            UnlockedAtStar = star,
            Code = attribute.Code,
            Name = attribute.Name,
            Value = value,
            IsPercentage = selected.IsPercentage
        };
    }
}
