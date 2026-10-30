using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Commands.Dungeon;

public sealed class ClaimDungeonChestCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IDungeonStarService starService,
    IItemStatCalculationService statCalculationService,
    IDungeonEquipmentRewardPolicy rewardPolicy, IGameTemplateCatalog? templates = null)
    : ICommandHandler<ClaimDungeonChestCommand, BaseResponse<ClaimStarChestResultDto>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly IDungeonStarService _starService = starService;
    private readonly IItemStatCalculationService _statCalculationService = statCalculationService;
    private readonly IDungeonEquipmentRewardPolicy _rewardPolicy = rewardPolicy;
    public async Task<BaseResponse<ClaimStarChestResultDto>> Handle(ClaimDungeonChestCommand message, CancellationToken ct) => BaseResponse<ClaimStarChestResultDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.ChestId, ct));
    private async Task<ClaimStarChestResultDto> ExecuteAsync(string userId, int chestId, CancellationToken cancellationToken = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var chest = await _unitOfWork.ReadOnlyRepository<HrkDungeonMapStarChest>().Query()
            .Include(x => x.DungeonMap)
            .SingleOrDefaultAsync(x => x.Id == chestId && x.IsActive, cancellationToken) ?? throw new BusinessException("DUNGEON_CHEST_NOT_FOUND", "Không tìm thấy rương sao này.", BusinessErrorKind.NotFound);
        var itemTemplates = await TemplateHydration.ItemTemplatesAsync(_templates, cancellationToken);
        chest.GuaranteedItemTemplate = chest.GuaranteedItemTemplateId.HasValue ? itemTemplates[chest.GuaranteedItemTemplateId.Value] : null;
        chest.DungeonMap.MaxEquipmentRarity = chest.DungeonMap.MaxEquipmentRarityId is { } maxRarityId
            ? (await _templates.GetAllAsync<HrkRarity>(cancellationToken)).Single(r => r.Id == maxRarityId) : null;
        // Check if already claimed (idempotency check)
        var alreadyClaimed = await _unitOfWork.ReadOnlyRepository<HrkPlayerDungeonStarChestClaim>().Query().AnyAsync(x => x.PlayerId == player.Id && x.StarChestId == chestId, cancellationToken);
        if (alreadyClaimed)
        {
            throw new BusinessException("REWARD_ALREADY_CLAIMED", "Rương này đã được nhận trước đó.", BusinessErrorKind.Conflict);
        }

        // Validate required stars
        int totalStars = await _starService.GetMapTotalStarsAsync(player.Id, chest.DungeonMapId, cancellationToken);
        if (totalStars < chest.RequiredStars)
        {
            throw BusinessException.Localized("INSUFFICIENT_DUNGEON_STARS", $"Chưa đủ số sao yêu cầu ({totalStars}/{chest.RequiredStars}) để mở rương.", BusinessErrorKind.Conflict);
        }

        ClaimStarChestResultDto? result = null;
        await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var claim = new HrkPlayerDungeonStarChestClaim
                {
                    PlayerId = player.Id,
                    StarChestId = chest.Id,
                    ClaimedOn = DateTime.UtcNow
                };
                await _unitOfWork.Repository<HrkPlayerDungeonStarChestClaim>().AddAsync(claim);
                // Add currencies to player wallet
                var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken) ?? throw new BusinessException("PLAYER_WALLET_NOT_FOUND", "Không tìm thấy ví người chơi.", BusinessErrorKind.NotFound);
                wallet.Gold += chest.GoldReward;
                wallet.Diamonds += chest.DiamondReward;
                wallet.UpgradeMaterials += chest.UpgradeMaterialsReward;
                wallet.UpdatedOn = DateTime.UtcNow;
                DungeonDroppedEquipmentDto? droppedEquipment = null;
                // Add guaranteed equipment if configured
                if (chest.GuaranteedItemTemplate != null)
                {
                    var item = chest.GuaranteedItemTemplate;
                    if (_rewardPolicy.CanReward(chest.DungeonMap, item))
                    {
                        string? currentStatsJson = null;
                        if (item.Category?.IsEquipment == true || !item.IsStackable)
                        {
                            currentStatsJson = _statCalculationService.CalculateCurrentStatsJson(item, 0, 0);
                        }

                        var newInvItem = new HrkPlayerInventory
                        {
                            PlayerId = player.Id,
                            ItemTemplateId = item.Id,
                            Count = 1,
                            Enhancement = 0,
                            Stars = 0,
                            IsEquipped = false,
                            EquippedHeroId = null,
                            IsLocked = false,
                            IsActive = true,
                            CurrentStats = currentStatsJson,
                            AcquiredOn = DateTime.UtcNow,
                            UpdatedOn = DateTime.UtcNow
                        };
                        await _unitOfWork.Repository<HrkPlayerInventory>().AddAsync(newInvItem);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        droppedEquipment = new DungeonDroppedEquipmentDto
                        {
                            InventoryItemId = newInvItem.Id,
                            ItemTemplateId = item.Id,
                            Code = item.Code,
                            Name = item.Name,
                            ImagePath = item.ImagePath,
                            RarityCode = item.Rarity.Code,
                            RarityName = item.Rarity.Name,
                            RarityColorHex = item.Rarity.ColorHex,
                            CategoryCode = item.Category?.Code ?? "EQUIPMENT",
                            Count = 1
                        };
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                result = new ClaimStarChestResultDto
                {
                    ChestId = chest.Id,
                    RequiredStars = chest.RequiredStars,
                    GoldGained = chest.GoldReward,
                    DiamondsGained = chest.DiamondReward,
                    UpgradeMaterialsGained = chest.UpgradeMaterialsReward,
                    DroppedEquipment = droppedEquipment,
                    Chest = new DungeonStarChestDto
                    {
                        Id = chest.Id,
                        DungeonMapId = chest.DungeonMapId,
                        RequiredStars = chest.RequiredStars,
                        State = "CLAIMED",
                        GoldReward = chest.GoldReward,
                        DiamondReward = chest.DiamondReward,
                        UpgradeMaterialsReward = chest.UpgradeMaterialsReward,
                        DisplayOrder = chest.DisplayOrder,
                        Description = chest.Description,
                        ClaimedOn = claim.ClaimedOn
                    }
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }, cancellationToken);
        return result ?? throw new InvalidOperationException("Giao dịch nhận rương phó bản không hoàn tất.");
    }
}
