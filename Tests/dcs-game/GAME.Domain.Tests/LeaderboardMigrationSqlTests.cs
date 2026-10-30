using System.Text.RegularExpressions;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class LeaderboardMigrationSqlTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>
{
    [SummonSqlFact]
    public async Task ProductionMigrationCreatesTablesAndPreservesExistingRecordsWhenRerun()
    {
        var player = await database.PlayerAsync();
        await using var db = database.Context();
        // This fixture owns a generated LocalDB database, never the application's HRK database.
        Assert.StartsWith("HRK_SummonTests_", db.Database.GetDbConnection().Database);
        await db.Database.ExecuteSqlRawAsync("DROP TABLE dbo.HRK_PlayerDiamondSpendTransactions; DROP TABLE dbo.HRK_PlayerRankingStats;");
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        const string path = "Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_PlayerLeaderboards.sql";
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, path))) dir = dir.Parent;
        var sql = (await File.ReadAllTextAsync(Path.Combine(dir!.FullName, path))).Replace("USE HRK;", "");
        async Task Apply()
        {
            foreach (var batch in Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                if (!string.IsNullOrWhiteSpace(batch)) await db.Database.ExecuteSqlRawAsync(batch);
        }
        await Apply();
        var row = await db.Set<HrkPlayerRankingStats>().SingleAsync(x => x.PlayerId == player.Id);
        Assert.False(row.PowerInitialized);
        row.TotalDiamondsSpent = 321;
        await db.SaveChangesAsync();
        await Apply();
        Assert.Equal(321, (await db.Set<HrkPlayerRankingStats>().AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).TotalDiamondsSpent);
        Assert.Equal(1, await db.Set<HrkGameFeatureConfig>().CountAsync(x => x.Code == "LEADERBOARD" && x.IsEnabled && !x.IsLocked));
    }
}
