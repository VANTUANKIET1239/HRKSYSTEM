using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Dungeon;

namespace GAME.Application.Features.Queries.Dungeon;

public sealed class GetStaminaQueryHandler(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService) : DungeonHandlerSupport(unitOfWork), IQueryHandler<GetStaminaQuery, BaseResponse<DungeonStaminaDto>>
{
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    public async Task<BaseResponse<DungeonStaminaDto>> Handle(GetStaminaQuery message, CancellationToken ct) => BaseResponse<DungeonStaminaDto>.SuccessResponse(await ExecuteAsync(message.UserId, ct));
    private async Task<DungeonStaminaDto> ExecuteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        ApplyStaminaRecovery(player);
        ResetDailyPurchaseCount(player);
        return ToStaminaDto(player);
    }
}
