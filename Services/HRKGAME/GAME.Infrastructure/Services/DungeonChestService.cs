using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services;

public sealed class DungeonChestService : IDungeonChestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDungeonStarService _starService;
    private readonly IItemStatCalculationService _statCalculationService;
    private readonly IDungeonEquipmentRewardPolicy _rewardPolicy;

    public DungeonChestService(
        IUnitOfWork unitOfWork,
        IDungeonStarService starService,
        IItemStatCalculationService statCalculationService,
        IDungeonEquipmentRewardPolicy rewardPolicy)
    {
        _unitOfWork = unitOfWork;
        _starService = starService;
        _statCalculationService = statCalculationService;
        _rewardPolicy = rewardPolicy;
    }

    public async Task<List<DungeonStarChestDto>> GetMapChestsAsync(long playerId, int mapId, int totalStars, CancellationToken cancellationToken = default)
    {
        var chests = await _unitOfWork.ReadOnlyRepository<HrkDungeonMapStarChest>().Query()
            .Where(x => x.DungeonMapId == mapId && x.IsActive)
            .Include(x => x.GuaranteedItemTemplate).ThenInclude(t => t!.Category)
            .Include(x => x.GuaranteedItemTemplate).ThenInclude(t => t!.Rarity)
            .OrderBy(x => x.DisplayOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var claims = await _unitOfWork.ReadOnlyRepository<HrkPlayerDungeonStarChestClaim>().Query()
            .Where(x => x.PlayerId == playerId && chests.Select(c => c.Id).Contains(x.StarChestId))
            .ToDictionaryAsync(x => x.StarChestId, x => x.ClaimedOn, cancellationToken);

        return chests.Select(chest =>
        {
            bool isClaimed = claims.ContainsKey(chest.Id);
            string state = isClaimed ? "CLAIMED" : totalStars >= chest.RequiredStars ? "CLAIMABLE" : "LOCKED";

            DungeonPossibleDropDto? itemPreview = null;
            if (chest.GuaranteedItemTemplate != null)
            {
                itemPreview = new DungeonPossibleDropDto
                {
                    ItemTemplateId = chest.GuaranteedItemTemplate.Id,
                    Code = chest.GuaranteedItemTemplate.Code,
                    Name = chest.GuaranteedItemTemplate.Name,
                    ImagePath = chest.GuaranteedItemTemplate.ImagePath,
                    RarityCode = chest.GuaranteedItemTemplate.Rarity.Code,
                    RarityName = chest.GuaranteedItemTemplate.Rarity.Name,
                    RarityColorHex = chest.GuaranteedItemTemplate.Rarity.ColorHex,
                    CategoryCode = chest.GuaranteedItemTemplate.Category.Code,
                    CategoryName = chest.GuaranteedItemTemplate.Category.Name,
                    DropRatePercent = 100m
                };
            }

            return new DungeonStarChestDto
            {
                Id = chest.Id,
                DungeonMapId = chest.DungeonMapId,
                RequiredStars = chest.RequiredStars,
                State = state,
                GoldReward = chest.GoldReward,
                DiamondReward = chest.DiamondReward,
                UpgradeMaterialsReward = chest.UpgradeMaterialsReward,
                GuaranteedItem = itemPreview,
                DisplayOrder = chest.DisplayOrder,
                Description = chest.Description,
                ClaimedOn = isClaimed ? claims[chest.Id] : null
            };
        }).ToList();
    }

    public async Task<ClaimStarChestResultDto> ClaimStarChestAsync(string userId, int chestId, CancellationToken cancellationToken = default)
    {
        var player = await _unitOfWork.Repository<HrkPlayer>().Query()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy người chơi.");

        var chest = await _unitOfWork.Repository<HrkDungeonMapStarChest>().Query()
            .Include(x => x.DungeonMap).ThenInclude(x => x.MaxEquipmentRarity)
            .Include(x => x.GuaranteedItemTemplate).ThenInclude(t => t!.Category)
            .Include(x => x.GuaranteedItemTemplate).ThenInclude(t => t!.Rarity)
            .Include(x => x.GuaranteedItemTemplate).ThenInclude(t => t!.Attributes).ThenInclude(a => a.AttributeType)
            .SingleOrDefaultAsync(x => x.Id == chestId && x.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy rương sao này.");

        // Check if already claimed (idempotency check)
        var alreadyClaimed = await _unitOfWork.ReadOnlyRepository<HrkPlayerDungeonStarChestClaim>().Query()
            .AnyAsync(x => x.PlayerId == player.Id && x.StarChestId == chestId, cancellationToken);

        if (alreadyClaimed)
        {
            throw new InvalidOperationException("Rương này đã được nhận trước đó.");
        }

        // Validate required stars
        int totalStars = await _starService.GetMapTotalStarsAsync(player.Id, chest.DungeonMapId, cancellationToken);
        if (totalStars < chest.RequiredStars)
        {
            throw new InvalidOperationException($"Chưa đủ số sao yêu cầu ({totalStars}/{chest.RequiredStars}) để mở rương.");
        }

        ClaimStarChestResultDto? result = null;

        await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync();
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
                var wallet = await _unitOfWork.Repository<HrkPlayerWallet>().Query()
                    .SingleAsync(x => x.PlayerId == player.Id, cancellationToken);

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
                await _unitOfWork.CommitTransactionAsync();

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
        });

        return result ?? throw new InvalidOperationException("Giao dịch nhận rương phó bản không hoàn tất.");
    }
}
