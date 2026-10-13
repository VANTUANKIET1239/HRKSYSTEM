using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Dungeon;

namespace GAME.Application.Features.Queries.Dungeon;

public sealed record GetMapsQuery(string UserId) : IQuery<BaseResponse<List<DungeonMapDto>>>;

public sealed class GetMapsQueryHandler(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService) : DungeonHandlerSupport(unitOfWork), IQueryHandler<GetMapsQuery, BaseResponse<List<DungeonMapDto>>>
{
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    public async Task<BaseResponse<List<DungeonMapDto>>> Handle(GetMapsQuery message, CancellationToken ct) => BaseResponse<List<DungeonMapDto>>.SuccessResponse(await ExecuteAsync(message.UserId, ct));
    private async Task<List<DungeonMapDto>> ExecuteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        ApplyStaminaRecovery(player);
        ResetDailyPurchaseCount(player);
        var maps = await _unitOfWork.ReadOnlyRepository<HrkDungeonMap>().Query()
            .Where(x => x.IsActive)
            .Include(x => x.Stages)
            .OrderBy(x => x.DisplayOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var cleared = await ClearedStageIds(player.Id, cancellationToken);
        return maps.Select(map => ToMapDto(map, player, cleared, maps)).ToList();
    }
}
