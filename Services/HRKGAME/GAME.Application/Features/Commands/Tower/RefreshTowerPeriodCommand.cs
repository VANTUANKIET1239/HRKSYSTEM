using Core.Application.CQRS;
using Core.Common.Errors;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Commands.Tower;

public sealed record RefreshTowerPeriodCommand(string UserId, string? EventCode = "TOWER_CLIMB") : ICommand<BaseResponse<bool>>;

public sealed class RefreshTowerPeriodCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService players,
    IEventPeriodService periods,
    TowerOperationRunner runner)
    : ICommandHandler<RefreshTowerPeriodCommand, BaseResponse<bool>>
{
    public async Task<BaseResponse<bool>> Handle(RefreshTowerPeriodCommand message, CancellationToken ct) => BaseResponse<bool>.SuccessResponse(await runner.RunAsync(message.UserId, async () =>
    {
        var player = await players.GetPlayerByUserIdAsync(message.UserId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Player was not found.", BusinessErrorKind.NotFound);
        var events = await unitOfWork.ReadOnlyRepository<HrkGameEvent>().Query()
            .Where(gameEvent => message.EventCode == null || gameEvent.Code == message.EventCode)
            .ToListAsync(ct);
        foreach (var gameEvent in events)
        {
            var period = await periods.GetOrCreateCurrentPeriodAsync(gameEvent, ct);
            await periods.GetOrCreatePlayerPeriodProgressAsync(player.Id, gameEvent, period, ct);
        }

        return true;
    }, ct));
}
