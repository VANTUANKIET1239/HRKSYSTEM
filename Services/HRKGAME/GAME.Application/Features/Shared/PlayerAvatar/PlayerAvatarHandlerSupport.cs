using Core.Common.Errors;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;

namespace GAME.Application.Features.Shared.PlayerAvatar;

public abstract class PlayerAvatarHandlerSupport
{
    protected readonly IGamePlayerService _playerLookups;
    protected PlayerAvatarHandlerSupport(IGamePlayerService playerLookups)
    {
        this._playerLookups = playerLookups;
    }

    protected async Task<HrkPlayer> GetRequiredPlayerAsync(string userId, CancellationToken cancellationToken, bool tracked = false)
    {
        return await _playerLookups.GetPlayerByUserIdAsync(userId, cancellationToken, tracked) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy thông tin người chơi.", BusinessErrorKind.NotFound);
    }
}
