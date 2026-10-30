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

public sealed class ExpandInventoryCapacityCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IOptions<InventorySettings>? options,
    IGameRequestContext actor, IDiamondSpendingService spending)
    : ICommandHandler<ExpandInventoryCapacityCommand, BaseResponse<PlayerWalletDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly InventorySettings _settings = options?.Value ?? new InventorySettings();
    private readonly IGameRequestContext _actor = actor;
    public Task<BaseResponse<PlayerWalletDto>> Handle(ExpandInventoryCapacityCommand request, CancellationToken cancellationToken)
    {
        var requestId = request.RequestId == Guid.Empty ? Guid.NewGuid() : request.RequestId;
        return HandleCore(request with { RequestId = requestId }, cancellationToken);
    }
    private async Task<BaseResponse<PlayerWalletDto>> HandleCore(ExpandInventoryCapacityCommand request, CancellationToken cancellationToken)
    {
        {
            var userId = _actor.UserId;
            var result = await ExecuteAsync(userId, request.SlotsToAdd, request.RequestId, cancellationToken);
            return BaseResponse<PlayerWalletDto>.SuccessResponse(result, "Mở rộng ô chứa hành trang thành công.");
        }
    }

    private async Task<PlayerWalletDto> ExecuteAsync(string userId, int slotsToAdd, Guid requestId, CancellationToken cancellationToken = default)
    {
        if (slotsToAdd <= 0 || slotsToAdd % _settings.SlotStepIncrement != 0)
        {
            throw BusinessException.Localized("INVALID_REQUEST", $"Số ô mở rộng phải là bội số của {_settings.SlotStepIncrement} và lớn hơn 0.", BusinessErrorKind.Validation);
        }

        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
        {
            throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy thông tin người chơi.", BusinessErrorKind.NotFound);
        }

        var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
        if (wallet == null)
        {
            throw new BusinessException("PLAYER_WALLET_NOT_FOUND", "Không tìm thấy ví người chơi.", BusinessErrorKind.NotFound);
        }

        if (await spending.IsReplayAsync(player.Id, requestId, "EXPAND_CAPACITY", slotsToAdd.ToString(System.Globalization.CultureInfo.InvariantCulture), cancellationToken))
            return ToDto(wallet);
        if ((long)wallet.MaxCapacity + slotsToAdd > _settings.MaxBagCapacity)
        {
            throw BusinessException.Localized("MAX_INVENTORY_CAPACITY", $"Hành trang đã đạt giới hạn tối đa cho phép ({_settings.MaxBagCapacity} ô).", BusinessErrorKind.Conflict);
        }

        var diamondCost = checked((long)slotsToAdd * _settings.DiamondCostPerSlot);
        await spending.SpendAsync(wallet, diamondCost, "EXPAND_CAPACITY", slotsToAdd.ToString(System.Globalization.CultureInfo.InvariantCulture), requestId, cancellationToken);
        wallet.MaxCapacity += slotsToAdd;
        wallet.UpdatedOn = DateTime.UtcNow;
        _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(wallet);
    }

    private static PlayerWalletDto ToDto(HrkPlayerWallet wallet) => new()
    {
        PlayerId = wallet.PlayerId,
        Gold = wallet.Gold,
        Diamonds = wallet.Diamonds,
        UpgradeMaterials = wallet.UpgradeMaterials,
        MaxCapacity = wallet.MaxCapacity,
        UpdatedOn = wallet.UpdatedOn
    };
}
