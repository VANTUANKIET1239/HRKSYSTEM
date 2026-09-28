namespace GAME.Application.Interfaces;

public sealed record LevelExperienceRequirement(int Level, int RequiredExp, bool IsMaxLevel);

public interface ILevelExperienceService
{
    Task<LevelExperienceRequirement> GetHeroRequirementAsync(int level, CancellationToken cancellationToken = default);
    Task<LevelExperienceRequirement> GetPlayerRequirementAsync(int level, CancellationToken cancellationToken = default);
}
