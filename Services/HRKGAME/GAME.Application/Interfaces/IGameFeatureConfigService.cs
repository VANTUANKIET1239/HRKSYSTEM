using GAME.Application.DTOs;

namespace GAME.Application.Interfaces
{
    public interface IGameFeatureConfigService
    {
        Task<List<GameFeatureConfigDto>> GetFeatureTreeAsync(CancellationToken cancellationToken = default);
    }
}
