using Core.Common.Errors;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Application.Services;
using Xunit;

namespace GAME.Domain.Tests;
public partial class TowerReliabilityTests
{
    private sealed class Levels : ILevelExperienceService
    {
        public Task<LevelExperienceRequirement> GetHeroRequirementAsync(int level, CancellationToken cancellationToken = default) => Task.FromResult(new LevelExperienceRequirement(level, 100, level >= 10));
        public Task<LevelExperienceRequirement> GetPlayerRequirementAsync(int level, CancellationToken cancellationToken = default) => GetHeroRequirementAsync(level, cancellationToken);
    }
}
