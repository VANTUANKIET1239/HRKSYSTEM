using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Configuration;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.Extensions.Options;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Commands.Inventory.ExpandCapacity;

public record ExpandInventoryCapacityCommand(int SlotsToAdd) : ICommand<BaseResponse<PlayerWalletDto>>;

public sealed class ExpandInventoryCapacityCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IOptions<InventorySettings>? options,
    IGameRequestContext actor)
    : ICommandHandler<ExpandInventoryCapacityCommand, BaseResponse<PlayerWalletDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly InventorySettings _settings = options?.Value ?? new InventorySettings();
    private readonly IGameRequestContext _actor = actor;
    public async Task<BaseResponse<PlayerWalletDto>> Handle(ExpandInventoryCapacityCommand request, CancellationToken cancellationToken)
    {
        {
            var userId = _actor.UserId;
            var result = await ExecuteAsync(userId, request.SlotsToAdd, cancellationToken);
            return BaseResponse<PlayerWalletDto>.SuccessResponse(result, "Mở rộng ô chứa hành trang thành công.");
        }
    }

    private async Task<PlayerWalletDto> ExecuteAsync(string userId, int slotsToAdd, CancellationToken cancellationToken = default)
    {
        if (slotsToAdd <= 0 || slotsToAdd % _settings.SlotStepIncrement != 0)
        {
            throw new BusinessException("INVALID_REQUEST", $"Số ô mở rộng phải là bội số của {_settings.SlotStepIncrement} và lớn hơn 0.", BusinessErrorKind.Validation);
        }

        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
        {
            throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy thông tin người chơi.", BusinessErrorKind.NotFound);
        }

        var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
        if (wallet == null)
        {
            throw new KeyNotFoundException("Không tìm thấy ví người chơi.");
        }

        if (wallet.MaxCapacity + slotsToAdd > _settings.MaxBagCapacity)
        {
            throw new BusinessException("MAX_INVENTORY_CAPACITY", $"Hành trang đã đạt giới hạn tối đa cho phép ({_settings.MaxBagCapacity} ô).", BusinessErrorKind.Conflict);
        }

        int diamondCost = slotsToAdd * _settings.DiamondCostPerSlot;
        if (wallet.Diamonds < diamondCost)
        {
            throw new BusinessException("INSUFFICIENT_DIAMONDS", $"Không đủ Kim Cương để mở rộng hành trang. Cần {diamondCost} Kim Cương (hiện có {wallet.Diamonds}).");
        }

        wallet.Diamonds -= diamondCost;
        wallet.MaxCapacity += slotsToAdd;
        wallet.UpdatedOn = DateTime.UtcNow;
        _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new PlayerWalletDto
        {
            PlayerId = wallet.PlayerId,
            Gold = wallet.Gold,
            Diamonds = wallet.Diamonds,
            UpgradeMaterials = wallet.UpgradeMaterials,
            MaxCapacity = wallet.MaxCapacity,
            UpdatedOn = wallet.UpdatedOn
        };
    }
}
