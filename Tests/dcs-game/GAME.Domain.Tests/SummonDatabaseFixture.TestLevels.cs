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
    private sealed class TestLevels : ILevelExperienceService
    {
        public Task<LevelExperienceRequirement> GetHeroRequirementAsync(int level, CancellationToken ct = default) => Task.FromResult(new LevelExperienceRequirement(level, 123, false));
        public Task<LevelExperienceRequirement> GetPlayerRequirementAsync(int level, CancellationToken ct = default) => Task.FromResult(new LevelExperienceRequirement(level, 100, false));
    }
}
