using GAME.Application.DTOs;
using GAME.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IGamePlayerService
    {
        Task<PlayerProfileDto?> GetPlayerProfileAsync(string userId, CancellationToken cancellationToken = default);
        Task<PlayerWalletDto?> GetPlayerWalletAsync(string userId, CancellationToken cancellationToken = default);
        Task<PlayerGameInfoDto?> GetPlayerGameInfoAsync(string userId, CancellationToken cancellationToken = default);
        Task<List<PlayerHeroDto>> GetPlayerHeroesAsync(string userId, CancellationToken cancellationToken = default);
        Task<PlayerHeroDetailDto?> GetPlayerHeroDetailAsync(string userId, long heroId, CancellationToken cancellationToken = default);
        Task<bool> SetPlayerHeroLockAsync(string userId, long heroId, bool isLocked, CancellationToken cancellationToken = default);
        Task<bool> SetPlayerHeroFavoriteAsync(string userId, long heroId, bool isFavorite, CancellationToken cancellationToken = default);
        Task<List<PlayerAvatarTemplateDto>> GetAvatarTemplatesAsync(string userId, CancellationToken cancellationToken = default);
        Task<PlayerProfileDto> SelectAvatarTemplateAsync(string userId, int avatarTemplateId, CancellationToken cancellationToken = default);
        Task<PlayerProfileDto> SaveCustomAvatarAsync(string userId, CustomAvatarUploadDto upload, CancellationToken cancellationToken = default);
        Task<PlayerCustomAvatarDto?> GetCustomAvatarAsync(long playerId, CancellationToken cancellationToken = default);
        Task<PlayerProfileDto> DeleteCustomAvatarAsync(string userId, CancellationToken cancellationToken = default);

        Task<HrkPlayer?> GetPlayerByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<HrkPlayerWallet?> GetWalletByPlayerIdAsync(long playerId, CancellationToken cancellationToken = default);
    }
}
