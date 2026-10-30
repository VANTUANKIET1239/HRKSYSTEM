using GAME.Application.DTOs.Leaderboard;

namespace GAME.Application.Interfaces;

public interface ILeaderboardReader
{
    Task<LeaderboardResponse> GetAsync(string userId, string type, int page, int pageSize, CancellationToken ct);
    Task<PublicPlayerProfileResponse> GetProfileAsync(long playerId, CancellationToken ct);
    Task<PublicHeroDetailResponse> GetHeroAsync(long playerId, long heroId, CancellationToken ct);
}
