using Core.Common.Repositories;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services;

public sealed class LevelExperienceService : ILevelExperienceService
{
    private readonly IUnitOfWork _unitOfWork;
    private IReadOnlyDictionary<int, LevelExperienceRequirement>? _heroRequirements;
    private IReadOnlyDictionary<int, LevelExperienceRequirement>? _playerRequirements;

    public LevelExperienceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LevelExperienceRequirement> GetHeroRequirementAsync(
        int level, CancellationToken cancellationToken = default)
    {
        _heroRequirements ??= await _unitOfWork.ReadOnlyRepository<HrkHeroLevelConfig>().Query()
            .Where(x => x.IsActive)
            .AsNoTracking()
            .Select(x => new LevelExperienceRequirement(x.Level, x.RequiredExp, x.IsMaxLevel))
            .ToDictionaryAsync(x => x.Level, cancellationToken);

        return GetRequired(_heroRequirements, level, "hero");
    }

    public async Task<LevelExperienceRequirement> GetPlayerRequirementAsync(
        int level, CancellationToken cancellationToken = default)
    {
        _playerRequirements ??= await _unitOfWork.ReadOnlyRepository<HrkPlayerLevelConfig>().Query()
            .Where(x => x.IsActive)
            .AsNoTracking()
            .Select(x => new LevelExperienceRequirement(x.Level, x.RequiredExp, x.IsMaxLevel))
            .ToDictionaryAsync(x => x.Level, cancellationToken);

        return GetRequired(_playerRequirements, level, "player");
    }

    private static LevelExperienceRequirement GetRequired(
        IReadOnlyDictionary<int, LevelExperienceRequirement> requirements,
        int level,
        string subject)
    {
        if (requirements.TryGetValue(level, out var requirement)) return requirement;

        throw new InvalidOperationException(
            $"Missing active {subject} experience configuration for level {level}.");
    }
}
