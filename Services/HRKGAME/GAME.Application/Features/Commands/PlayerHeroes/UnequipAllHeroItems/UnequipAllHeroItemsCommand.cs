using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.PlayerHeroes;

namespace GAME.Application.Features.Commands.PlayerHeroes.UnequipAllHeroItems;

public record UnequipAllHeroItemsCommand(long HeroId) : ICommand<BaseResponse<PlayerHeroDetailDto>>;

public sealed class UnequipAllHeroItemsCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IPlayerReadModels playerReadModels,
    IGameRequestContext actor)
    : PlayerHeroesHandlerSupport(), ICommandHandler<UnequipAllHeroItemsCommand, BaseResponse<PlayerHeroDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly IPlayerReadModels _playerReadModels = playerReadModels;
    private readonly IGameRequestContext _actor = actor;
    public async Task<BaseResponse<PlayerHeroDetailDto>> Handle(UnequipAllHeroItemsCommand request, CancellationToken cancellationToken)
    {
        {
            var userId = _actor.UserId;
            var detail = await ExecuteAsync(userId, request.HeroId, cancellationToken);
            return BaseResponse<PlayerHeroDetailDto>.SuccessResponse(detail, "Tháo toàn bộ trang bị thành công.");
        }
    }

    private async Task<PlayerHeroDetailDto> ExecuteAsync(string userId, long heroId, CancellationToken cancellationToken = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy thông tin người chơi.", BusinessErrorKind.NotFound);
        var hero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
            .FirstOrDefaultAsync(h => h.Id == heroId && h.PlayerId == player.Id && h.IsActive, cancellationToken) ?? throw new BusinessException("HERO_NOT_FOUND", "Không tìm thấy võ tướng hoặc võ tướng không thuộc tài khoản này.", BusinessErrorKind.NotFound);
        var equipment = await _unitOfWork.Repository<HrkPlayerEquipment>().Query()
            .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.HeroId == heroId, cancellationToken);
        if (equipment == null)
        {
            var detailWithoutEq = await _playerReadModels.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
            return detailWithoutEq ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng.");
        }

        var equippedItemIds = new List<long>();
        if (equipment.WeaponId.HasValue)
            equippedItemIds.Add(equipment.WeaponId.Value);
        if (equipment.ArmorId.HasValue)
            equippedItemIds.Add(equipment.ArmorId.Value);
        if (equipment.HelmetId.HasValue)
            equippedItemIds.Add(equipment.HelmetId.Value);
        if (equipment.BootsId.HasValue)
            equippedItemIds.Add(equipment.BootsId.Value);
        if (equipment.RingId.HasValue)
            equippedItemIds.Add(equipment.RingId.Value);
        if (equipment.ArtifactId.HasValue)
            equippedItemIds.Add(equipment.ArtifactId.Value);
        if (equippedItemIds.Count == 0)
        {
            var currentDetail = await _playerReadModels.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
            return currentDetail ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng.");
        }

        var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
        int currentBagItemsCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query().CountAsync(i => i.PlayerId == player.Id && i.IsActive && !i.IsEquipped, cancellationToken);
        int bagLimit = wallet?.MaxCapacity ?? 200;
        if (currentBagItemsCount + equippedItemIds.Count > bagLimit)
        {
            throw BusinessException.Localized("INVENTORY_FULL", $"Hành trang không đủ chỗ để tháo {equippedItemIds.Count} trang bị ({currentBagItemsCount}/{bagLimit} ô). Vui lòng dọn bớt hoặc mở rộng túi đồ.", BusinessErrorKind.Conflict);
        }

        await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                equipment.WeaponId = null;
                equipment.ArmorId = null;
                equipment.HelmetId = null;
                equipment.BootsId = null;
                equipment.RingId = null;
                equipment.ArtifactId = null;
                _unitOfWork.Repository<HrkPlayerEquipment>().Update(equipment);
                var items = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                    .Where(i => equippedItemIds.Contains(i.Id) && i.PlayerId == player.Id)
                    .ToListAsync(cancellationToken);
                foreach (var item in items)
                {
                    item.IsEquipped = false;
                    item.EquippedHeroId = null;
                    item.UpdatedOn = DateTime.UtcNow;
                    _unitOfWork.Repository<HrkPlayerInventory>().Update(item);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }, cancellationToken);
        var updated = await _playerReadModels.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
        return updated ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng sau khi tháo toàn bộ trang bị.");
    }
}
