using GAME.Application.DTOs;
using Core.Common.Errors;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Application.Services;
using Xunit;

namespace GAME.Domain.Tests;
public sealed partial class BattleLabBuildTests
{
    private sealed class Power : ICombatPowerService
    {
        public Task<List<CombatPowerConfigDto>> GetConfigsAsync(CancellationToken ct = default) => Task.FromResult(new List<CombatPowerConfigDto>());
        public int Calculate(CalculatedStatsDto stats, IReadOnlyCollection<CombatPowerConfigDto> configs) => stats.Atk;
        public Task<int> CalculateAsync(CalculatedStatsDto stats, CancellationToken ct = default) => Task.FromResult(stats.Atk);
    }
}
