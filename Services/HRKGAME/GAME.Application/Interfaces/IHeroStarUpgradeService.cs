using GAME.Application.DTOs;
namespace GAME.Application.Interfaces;

public interface IHeroStarUpgradeService
{
    Task<HeroStarUpgradePreviewDto> PreviewAsync(string userId, long heroId, CancellationToken ct = default);
    Task<HeroStarUpgradePreviewDto> UpgradeAsync(string userId, long heroId, Guid requestId, CancellationToken ct = default);
}
