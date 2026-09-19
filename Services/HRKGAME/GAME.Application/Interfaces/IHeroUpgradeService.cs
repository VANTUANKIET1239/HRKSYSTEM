using GAME.Application.DTOs;

namespace GAME.Application.Interfaces
{
    public interface IHeroUpgradeService
    {
        Task<HeroUpgradePreviewDto> GetPreviewAsync(string userId, long heroId, CancellationToken cancellationToken = default);
        Task<PlayerHeroDetailDto> UpgradeAsync(string userId, long heroId, int levels, CancellationToken cancellationToken = default);
    }
}
