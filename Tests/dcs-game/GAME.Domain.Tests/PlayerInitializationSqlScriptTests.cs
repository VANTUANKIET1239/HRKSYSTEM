using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class PlayerInitializationSqlScriptTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>
{
    [SummonSqlFact]
    public async Task CreatesActualSchemaAndSeedsOnceWithoutOverwritingReviewedValues()
    {
        await using var db = database.Context();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE dbo.HRK_PlayerInitializations; DROP TABLE dbo.HRK_PlayerInitializationHeroes; DROP TABLE dbo.HRK_PlayerInitializationItems; DROP TABLE dbo.HRK_PlayerInitializationConfigs;");
        var original = await db.HeroTemplates.AsNoTracking().SingleAsync();
        db.AddRange(Enumerable.Range(2, 2).Select(number => new HrkHeroTemplate
        {
            Name = "Starter " + number, Avatar = "/hero.png", RarityId = original.RarityId,
            FactionId = original.FactionId, ClassId = original.ClassId
        }));
        db.Add(new HrkFormationTemplate
        {
            Code = "LUC_DO", Name = "Starter", Slots = [new() { Slot = 1 }, new() { Slot = 2 }, new() { Slot = 3 }]
        });
        db.Add(new HrkAvatarTemplate { Code = "STARTER", Name = "Starter", ImagePath = "/hero.png", IsDefault = true });
        db.Add(new HrkItemTemplate
        {
            Code = "ENHANCEMENT_STONE_I", Name = "Stone", IsStackable = true, MaxStackSize = 99,
            CategoryId = await db.ItemCategories.Select(x => x.Id).FirstAsync(), RarityId = original.RarityId
        });
        // EnsureCreated's legacy model generates these integer keys; the scratch table starts at 1.
        db.Add(new HrkPlayerLevelConfig { Level = 0, RequiredExp = 100, IsActive = true });
        db.Add(new HrkHeroLevelConfig { Level = 0, RequiredExp = 100, IsActive = true });
        await db.SaveChangesAsync();
        await ApplyAsync("Migration_PlayerInitialization.sql");
        await ApplyAsync("Migration_PlayerInitialization.sql");
        await ApplyAsync("Seed_PlayerInitialization.sql");
        await ApplyAsync("Seed_PlayerInitialization.sql");
        var config = await db.Set<HrkPlayerInitializationConfig>().SingleAsync();
        Assert.True(config.IsActive);
        Assert.Equal(10000, config.InitialGold);
        Assert.Equal(3, await db.Set<HrkPlayerInitializationHero>().CountAsync());
        Assert.Equal(10, (await db.Set<HrkPlayerInitializationItem>().SingleAsync()).Quantity);
        config.InitialGold = 12345;
        config.IsActive = false;
        await db.SaveChangesAsync();
        await ApplyAsync("Seed_PlayerInitialization.sql");
        db.ChangeTracker.Clear();
        config = await db.Set<HrkPlayerInitializationConfig>().SingleAsync();
        Assert.Equal(12345, config.InitialGold);
        Assert.False(config.IsActive);
    }

    private async Task ApplyAsync(string file)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        var relative = "Services/HRKGAME/GAME.Infrastructure/Data/Scripts/" + file;
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, relative))) directory = directory.Parent;
        var script = (await File.ReadAllTextAsync(Path.Combine(directory!.FullName, relative))).Replace("USE HRK;", "");
        await using var db = database.Context();
        foreach (var batch in Regex.Split(script, @"(?m)^GO\s*$"))
            if (!string.IsNullOrWhiteSpace(batch)) await db.Database.ExecuteSqlRawAsync(batch);
    }
}
