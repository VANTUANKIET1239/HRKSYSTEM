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

public sealed class TemplateCacheSqlTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>
{
    [SummonSqlFact]
    public async Task PublishedSqlTemplatesStayDetachedDuringSummonAndDbEditsRequireRefresh()
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new CacheEnvironment());
        services.AddSingleton<IDistributedCache>(new RedisProbe());
        services.AddScoped<GameDbContext>(_ => database.Context());
        services.AddScoped<IUnitOfWork>(sp => new UnitOfWork<GameDbContext>(sp.GetRequiredService<GameDbContext>()));
        services.AddGameTemplateCache(new ConfigurationBuilder().AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json")).AddInMemoryCollection(new Dictionary<string, string?>
        { ["GameTemplateCache:WarmupOnStartup"] = "false" }).Build());
        await using var node = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await node.GetRequiredService<IGameTemplateRefresh>().RefreshAsync();
        var player = await database.PlayerAsync();
        int originalTemplateCount;
        using (var scope = node.CreateScope())
        {
            var catalog = scope.ServiceProvider.GetRequiredService<IGameTemplateCatalog>();
            var store = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            originalTemplateCount = await db.HeroTemplates.CountAsync();
            var templates = (await catalog.GetAllAsync<HrkHeroTemplate>()).ToList();
            Assert.NotEmpty(templates);
            Assert.Empty(db.ChangeTracker.Entries());
            // The fixture does not seed level tables; an explicit test level service supplies them.
            var granter = new SummonRewardGranter(store, new HeroProgressionStatService(), new Power(), new Levels(), new Random(), catalog);
            var handler = new SummonCommandHandler(store, TestGamePlayers.Create(store), new Random(), granter, catalog);
            var result = await handler.Handle(new(player.UserId, new(Guid.NewGuid(), "NORMAL", 1)), default);
            Assert.True(result.Success);
            Assert.DoesNotContain(db.ChangeTracker.Entries(), entry => entry.Entity is HrkHeroTemplate or HrkRarity or HrkSkillTemplate or HrkAttributeType);
            Assert.Equal(originalTemplateCount, await db.HeroTemplates.CountAsync());
        }
        int heroId;
        using (var scope = node.CreateScope())
        {
            var catalog = scope.ServiceProvider.GetRequiredService<IGameTemplateCatalog>();
            var hero = (await catalog.GetAllAsync<HrkHeroTemplate>()).First();
            heroId = hero.Id;
        }
        await using (var edit = database.Context())
        {
            var hero = await edit.HeroTemplates.SingleAsync(x => x.Id == heroId);
            hero.Name = "Refreshed SQL template";
            await edit.SaveChangesAsync();
        }
        using (var oldScope = node.CreateScope())
            Assert.NotEqual("Refreshed SQL template", ((await oldScope.ServiceProvider.GetRequiredService<IGameTemplateCatalog>().GetAllAsync<HrkHeroTemplate>()).Single(x => x.Id == heroId)).Name);
        await node.GetRequiredService<IGameTemplateRefresh>().RefreshAsync();
        using var freshScope = node.CreateScope();
        Assert.Equal("Refreshed SQL template", ((await freshScope.ServiceProvider.GetRequiredService<IGameTemplateCatalog>().GetAllAsync<HrkHeroTemplate>()).Single(x => x.Id == heroId)).Name);
    }

    private sealed class Random : ISummonRandom { public int Next(int exclusiveMaximum) => 0; }
    private sealed class Levels : ILevelExperienceService
    {
        public Task<LevelExperienceRequirement> GetHeroRequirementAsync(int level, CancellationToken cancellationToken = default) => Task.FromResult(new LevelExperienceRequirement(level, 100, false));
        public Task<LevelExperienceRequirement> GetPlayerRequirementAsync(int level, CancellationToken cancellationToken = default) => GetHeroRequirementAsync(level, cancellationToken);
    }
    private sealed class Power : ICombatPowerService
    {
        public Task<List<GAME.Application.DTOs.CombatPowerConfigDto>> GetConfigsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<GAME.Application.DTOs.CombatPowerConfigDto>());
        public Task<int> CalculateAsync(GAME.Application.DTOs.CalculatedStatsDto stats, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public int Calculate(GAME.Application.DTOs.CalculatedStatsDto stats, IReadOnlyCollection<GAME.Application.DTOs.CombatPowerConfigDto> configs) => 1;
    }
}
