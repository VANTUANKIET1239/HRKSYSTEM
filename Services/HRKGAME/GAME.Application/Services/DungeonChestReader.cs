using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Services;

public sealed class DungeonChestReader(IUnitOfWork unitOfWork) : IDungeonChestReader
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
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
}
