using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Dungeon;

namespace GAME.Application.Features.Commands.Dungeon;

public sealed record PurchaseStaminaCommand(string UserId) : ICommand<BaseResponse<DungeonStaminaDto>>;

public sealed class PurchaseStaminaCommandHandler(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService) : DungeonHandlerSupport(unitOfWork), ICommandHandler<PurchaseStaminaCommand, BaseResponse<DungeonStaminaDto>>
{
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    public async Task<BaseResponse<DungeonStaminaDto>> Handle(PurchaseStaminaCommand message, CancellationToken ct) => BaseResponse<DungeonStaminaDto>.SuccessResponse(await ExecuteAsync(message.UserId, ct));
    private async Task<DungeonStaminaDto> ExecuteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken, tracked: true) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        ApplyStaminaRecovery(player);
        ResetDailyPurchaseCount(player);
        if (player.DailyStaminaPurchaseCount >= 10)
            throw new BusinessException("STAMINA_PURCHASE_DAILY_LIMIT", "Đã đạt giới hạn mua thể lực hôm nay.", BusinessErrorKind.Conflict);
        var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy ví người chơi.");
        var cost = PurchaseCost(player.DailyStaminaPurchaseCount);
        if (wallet.Diamonds < cost)
            throw new BusinessException("INSUFFICIENT_DIAMONDS", $"Không đủ kim cương. Cần {cost} kim cương.", BusinessErrorKind.Conflict);
        wallet.Diamonds -= cost;
        wallet.UpdatedOn = DateTime.UtcNow;
        player.Stamina += 50;
        player.DailyStaminaPurchaseCount++;
        player.StaminaPurchaseDate = DateTime.UtcNow.Date;
        player.UpdatedOn = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToStaminaDto(player);
    }
}
