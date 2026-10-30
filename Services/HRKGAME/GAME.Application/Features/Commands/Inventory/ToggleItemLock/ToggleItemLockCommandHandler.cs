using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Commands.Inventory.ToggleItemLock;

public sealed class ToggleItemLockCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IGameRequestContext actor)
    : ICommandHandler<ToggleItemLockCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly IGameRequestContext _actor = actor;
    public async Task<BaseResponse<bool>> Handle(ToggleItemLockCommand request, CancellationToken cancellationToken)
    {
        {
            var userId = _actor.UserId;
            var result = await ExecuteAsync(userId, request.InventoryItemId, request.IsLocked, cancellationToken);
            return BaseResponse<bool>.SuccessResponse(result, result ? "Đã khóa vật phẩm thành công." : "Đã mở khóa vật phẩm thành công.");
        }
    }

    private async Task<bool> ExecuteAsync(string userId, long inventoryItemId, bool isLocked, CancellationToken cancellationToken = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
        {
            throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy thông tin người chơi.", BusinessErrorKind.NotFound);
        }

        var item = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
            .FirstOrDefaultAsync(i => i.Id == inventoryItemId && i.PlayerId == player.Id && i.IsActive, cancellationToken);
        if (item == null)
        {
            throw new BusinessException("ITEM_NOT_FOUND", "Không tìm thấy vật phẩm trong hành trang.", BusinessErrorKind.NotFound);
        }

        item.IsLocked = isLocked;
        item.UpdatedOn = DateTime.UtcNow;
        _unitOfWork.Repository<HrkPlayerInventory>().Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return item.IsLocked;
    }
}
