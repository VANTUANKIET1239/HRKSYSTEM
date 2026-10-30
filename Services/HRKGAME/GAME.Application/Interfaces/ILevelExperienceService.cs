namespace GAME.Application.Interfaces;

public interface ILevelExperienceService
{
    Task<LevelExperienceRequirement> GetHeroRequirementAsync(int level, CancellationToken cancellationToken = default);
    Task<LevelExperienceRequirement> GetPlayerRequirementAsync(int level, CancellationToken cancellationToken = default);
}
