using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Persistence.Repositories;
using GAME.Infrastructure.Data;
using Xunit;

namespace GAME.Domain.Tests;

public partial class GameAssetPathTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>
{
    private static string RepositoryFile(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relative))) directory = directory.Parent;
        return Path.Combine(directory?.FullName ?? throw new FileNotFoundException(relative), relative);
    }

    [Fact]
    public async Task SqlMigrationUsesTheSameAliasesAsTheFrontendCatalog()
    {
        using var catalog = JsonDocument.Parse(await File.ReadAllTextAsync(RepositoryFile("HrkUi/src/assets/catalog/asset-paths.json")));
        var expected = catalog.RootElement.GetProperty("aliases").EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetString()!);
        var sql = await File.ReadAllTextAsync(RepositoryFile("Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_NormalizeGameAssetPaths.sql"));
        var actual = Regex.Matches(sql, @"\(N'(images/[^']+)', N'(images/[^']+)'\)")
            .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value);
        Assert.Equal(expected.Count, actual.Count);
        foreach (var (key, value) in expected) Assert.Equal(value, actual[key]);
    }

    [SummonSqlFact]
    public async Task MigrationNormalizesImageColumnsPreservesOtherDataAndCanBeRerun()
    {
        await using var db = database.Context();
        await using var unitOfWork = new UnitOfWork<GameDbContext>(db);
        await unitOfWork.Sql.ExecuteAsync("""
            CREATE TABLE dbo.HRK_AssetPathMigrationProbe (
                Id INT PRIMARY KEY, Avatar NVARCHAR(500), ImagePath NVARCHAR(500), BackgroundPath NVARCHAR(500),
                Icon NVARCHAR(100), BattleJson NVARCHAR(MAX), ImageData VARBINARY(4));
            INSERT INTO dbo.HRK_AssetPathMigrationProbe VALUES
                (1, @Avatar, @ImagePath, @BackgroundPath, 'bi-gem', @BattleJson, 0x01020304),
                (2, 'https://images.example.com/portrait.png', '/assets/equipments/combat-broom-mk2.png', NULL, NULL, NULL, NULL),
                (3, '/api/player/avatar/custom/1?v=3', NULL, NULL, NULL, NULL, NULL);
            """, new
        {
            Avatar = "/assets/images/dcs-game/kiet.png?v=7#portrait",
            ImagePath = "assets/images/dcs-game/items/formation_stone.png",
            BackgroundPath = "/assets/images/dcs-game/dungeon/maps/bug-forest.jpg",
            BattleJson = """{"avatar":"/assets/images/dcs-game/kiet.png"}"""
        });
        var sql = await File.ReadAllTextAsync(RepositoryFile("Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_NormalizeGameAssetPaths.sql"));
        await unitOfWork.Sql.ExecuteAsync(sql);
        await unitOfWork.Sql.ExecuteAsync(sql);
        var rows = (await unitOfWork.Sql.QueryAsync<AssetRow>("SELECT * FROM dbo.HRK_AssetPathMigrationProbe ORDER BY Id")).ToArray();
        Assert.Equal("images/dcs-game/heroes/kiet.png?v=7#portrait", rows[0].Avatar);
        Assert.Equal("images/dcs-game/items/formation-stone.png", rows[0].ImagePath);
        Assert.Equal("images/dcs-game/dungeons/bug-forest/map-background.jpg", rows[0].BackgroundPath);
        Assert.Equal("bi-gem", rows[0].Icon);
        Assert.Equal("""{"avatar":"/assets/images/dcs-game/kiet.png"}""", rows[0].BattleJson);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, rows[0].ImageData);
        Assert.Equal("https://images.example.com/portrait.png", rows[1].Avatar);
        Assert.Equal("images/dcs-game/equipments/combat-broom-mk2.png", rows[1].ImagePath);
        Assert.Null(rows[1].BackgroundPath);
        Assert.Equal("/api/player/avatar/custom/1?v=3", rows[2].Avatar);
    }
}
