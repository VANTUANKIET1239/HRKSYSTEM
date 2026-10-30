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
    private sealed class Power : ICombatPowerService
    {
        public Task<List<CombatPowerConfigDto>> GetConfigsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<CombatPowerConfigDto>());
        public int Calculate(CalculatedStatsDto stats, IReadOnlyCollection<CombatPowerConfigDto> configs) => stats.Hp;
        public Task<int> CalculateAsync(CalculatedStatsDto stats, CancellationToken cancellationToken = default) => Task.FromResult(stats.Hp);
    }
}
