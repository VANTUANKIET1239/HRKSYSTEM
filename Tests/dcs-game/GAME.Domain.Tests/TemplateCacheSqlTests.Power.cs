using Core.Persistence.Repositories;
using GAME.Application.Features.Commands.Summon;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Application.Templates;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GAME.Domain.Tests;
public sealed partial class TemplateCacheSqlTests
{
    private sealed class Power : ICombatPowerService
    {
        public Task<List<GAME.Application.DTOs.CombatPowerConfigDto>> GetConfigsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<GAME.Application.DTOs.CombatPowerConfigDto>());
        public Task<int> CalculateAsync(GAME.Application.DTOs.CalculatedStatsDto stats, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public int Calculate(GAME.Application.DTOs.CalculatedStatsDto stats, IReadOnlyCollection<GAME.Application.DTOs.CombatPowerConfigDto> configs) => 1;
    }
}
