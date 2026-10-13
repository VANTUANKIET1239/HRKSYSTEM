using GAME.Application.DTOs;
using GAME.Domain.Entities;

namespace GAME.Application.Interfaces;
/// <summary>Read-only projections reused by queries and command result builders.</summary>
public interface IPlayerReadModels
{
    Task<PlayerProfileDto?> GetPlayerProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<PlayerWalletDto?> GetPlayerWalletAsync(string userId, CancellationToken cancellationToken = default);
    Task<PlayerGameInfoDto?> GetPlayerGameInfoAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<PlayerHeroDto>> GetPlayerHeroesAsync(string userId, CancellationToken cancellationToken = default);
    Task<PlayerHeroDetailDto?> GetPlayerHeroDetailAsync(string userId, long heroId, CancellationToken cancellationToken = default);
    Task<List<PlayerAvatarTemplateDto>> GetAvatarTemplatesAsync(string userId, CancellationToken cancellationToken = default);
    Task<PlayerCustomAvatarDto?> GetCustomAvatarAsync(long playerId, CancellationToken cancellationToken = default);
    Task<PlayerProfileDto> BuildProfileAsync(HrkPlayer player, CancellationToken cancellationToken, int? knownFormationPower = null);
}
