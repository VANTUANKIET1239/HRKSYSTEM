using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.PlayerHeroes;

namespace GAME.Application.Features.Commands.PlayerHeroes.UnequipHeroItem;

public record UnequipHeroItemCommand(long HeroId, string SlotCode) : ICommand<BaseResponse<PlayerHeroDetailDto>>;

public sealed class UnequipHeroItemCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IPlayerReadModels playerReadModels,
    IGameRequestContext actor)
    : PlayerHeroesHandlerSupport(), ICommandHandler<UnequipHeroItemCommand, BaseResponse<PlayerHeroDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private static readonly HashSet<string> ValidSlotCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "WEAPON",
        "ARMOR",
        "HELMET",
        "BOOTS",
        "RING",
        "ARTIFACT"
    };
    private readonly IPlayerReadModels _playerReadModels = playerReadModels;
    private readonly IGameRequestContext _actor = actor;
    public async Task<BaseResponse<PlayerHeroDetailDto>> Handle(UnequipHeroItemCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SlotCode))
        {
            return BaseResponse<PlayerHeroDetailDto>.FailResponse("Vị trí trang bị (SlotCode) không được để trống.", statusCode: 400);
        }

        {
            var userId = _actor.UserId;
            var detail = await ExecuteAsync(userId, request.HeroId, request.SlotCode, cancellationToken);
            return BaseResponse<PlayerHeroDetailDto>.SuccessResponse(detail, $"Tháo trang bị ở vị trí {request.SlotCode.ToUpper()} thành công.");
        }
    }

    private async Task<PlayerHeroDetailDto> ExecuteAsync(string userId, long heroId, string slotCode, CancellationToken cancellationToken = default)
    {
        var cleanSlot = (slotCode ?? "").Trim().ToUpperInvariant();
        if (!ValidSlotCodes.Contains(cleanSlot))
        {
            throw new BusinessException("INVALID_REQUEST", $"Vị trí trang bị '{slotCode}' không hợp lệ. Cho phép: {string.Join(", ", ValidSlotCodes)}.", BusinessErrorKind.Validation);
        }

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

        long? currentItemId = GetSlotItemId(equipment, cleanSlot);
        if (!currentItemId.HasValue)
        {
            var currentDetail = await _playerReadModels.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
            return currentDetail ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng.");
        }

        var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
        int currentBagItemsCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query().CountAsync(i => i.PlayerId == player.Id && i.IsActive && !i.IsEquipped, cancellationToken);
        int bagLimit = wallet?.MaxCapacity ?? 200;
        if (currentBagItemsCount + 1 > bagLimit)
        {
            throw new BusinessException("INVENTORY_FULL", $"Hành trang đã đầy ({currentBagItemsCount}/{bagLimit} ô). Vui lòng dọn bớt hoặc mở rộng túi đồ trước khi tháo.", BusinessErrorKind.Conflict);
        }

        await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                SetSlotItemId(equipment, cleanSlot, null);
                _unitOfWork.Repository<HrkPlayerEquipment>().Update(equipment);
                var item = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                    .FirstOrDefaultAsync(i => i.Id == currentItemId.Value && i.PlayerId == player.Id, cancellationToken);
                if (item != null)
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
        var updatedDetail = await _playerReadModels.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
        return updatedDetail ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng sau khi tháo trang bị.");
    }
}
