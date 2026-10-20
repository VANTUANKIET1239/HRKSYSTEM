using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;

namespace GAME.Application.Services;

public sealed class LevelExperienceService : ILevelExperienceService
{
    private readonly IGameTemplateCatalog _templates;
    private readonly IUnitOfWork _unitOfWork;
    private IReadOnlyDictionary<int, LevelExperienceRequirement>? _heroRequirements;
    private IReadOnlyDictionary<int, LevelExperienceRequirement>? _playerRequirements;
    public LevelExperienceService(IUnitOfWork unitOfWork, IGameTemplateCatalog? templates = null)
    {
        _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
        _unitOfWork = unitOfWork;
    }

    public async Task<LevelExperienceRequirement> GetHeroRequirementAsync(int level, CancellationToken cancellationToken = default)
    {
        _heroRequirements ??= (await _templates.GetAllAsync<HrkHeroLevelConfig>(cancellationToken))
            .Where(x => x.IsActive)
            .Select(x => new LevelExperienceRequirement(x.Level, x.RequiredExp, x.IsMaxLevel))
            .ToDictionary(x => x.Level);
        return GetRequired(_heroRequirements, level, "hero");
    }

    public async Task<LevelExperienceRequirement> GetPlayerRequirementAsync(int level, CancellationToken cancellationToken = default)
    {
        _playerRequirements ??= (await _templates.GetAllAsync<HrkPlayerLevelConfig>(cancellationToken))
            .Where(x => x.IsActive)
            .Select(x => new LevelExperienceRequirement(x.Level, x.RequiredExp, x.IsMaxLevel))
            .ToDictionary(x => x.Level);
        return GetRequired(_playerRequirements, level, "player");
    }

    private static LevelExperienceRequirement GetRequired(IReadOnlyDictionary<int, LevelExperienceRequirement> requirements, int level, string subject)
    {
        if (requirements.TryGetValue(level, out var requirement))
            return requirement;
        throw new InvalidOperationException($"Missing active {subject} experience configuration for level {level}.");
    }
}
