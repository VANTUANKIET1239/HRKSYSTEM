using System.Text.Json;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Application.Services;
using GAME.Application.Templates;
using GAME.Infrastructure.Templates;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GAME.Domain.Tests;
public sealed partial class SummonDatabaseFixture
{
    private sealed class TestPower : ICombatPowerService
    {
        public Task<List<CombatPowerConfigDto>> GetConfigsAsync(CancellationToken ct = default) => Task.FromResult(new List<CombatPowerConfigDto>());
        public int Calculate(CalculatedStatsDto stats, IReadOnlyCollection<CombatPowerConfigDto> configs) => 999;
        public Task<int> CalculateAsync(CalculatedStatsDto stats, CancellationToken ct = default) => Task.FromResult(999);
    }
}
