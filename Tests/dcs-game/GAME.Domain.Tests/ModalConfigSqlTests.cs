using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Features.Queries.GameConfig;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;

public class ModalConfigSqlTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>
{
    [SummonSqlFact]
    public async Task ProductionMigrationCreatesAndSeedsTheTableAndPreservesEditsOnRerun()
    {
        await using var db = database.Context();
        await using var uow = new UnitOfWork<GameDbContext>(db);
        // EnsureCreated includes the new model. Drop only the new table in this isolated fixture DB
        // to exercise the actual CREATE TABLE migration as well as rerunnable seeding.
        await db.Database.ExecuteSqlRawAsync("DROP TABLE dbo.HRK_ModalConfigs");
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        const string relative = "Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_ModalConfigs.sql";
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, relative))) directory = directory.Parent;
        var script = await File.ReadAllTextAsync(Path.Combine(directory!.FullName, relative));
        await uow.Sql.ExecuteAsync(script);
        Assert.Equal(26, await db.ModalConfigs.CountAsync());
        var edited = await db.ModalConfigs.SingleAsync(config => config.Code == "LOGOUT_CONFIRM");
        edited.Title = "Tiêu đề đã chỉnh sửa";
        edited.IsActive = false;
        edited.Revision = 4;
        await db.SaveChangesAsync();
        await uow.Sql.ExecuteAsync(script);
        db.ChangeTracker.Clear();
        var saved = await db.ModalConfigs.AsNoTracking().SingleAsync(config => config.Code == "LOGOUT_CONFIRM");
        Assert.Equal("Tiêu đề đã chỉnh sửa", saved.Title);
        Assert.False(saved.IsActive);
        Assert.Equal(4, saved.Revision);
        Assert.Equal(26, await db.ModalConfigs.CountAsync());

        var response = await new GetModalConfigsQueryHandler(uow).Handle(new(), default);
        Assert.Equal(25, response.Data!.Count);
        Assert.DoesNotContain(response.Data!, config => config.Code == "LOGOUT_CONFIRM");
        Assert.Contains(response.Data!, config => config.Code == "TOWER_START_QUICK_CLIMB" && config.MessageTemplate!.Contains("{limit}"));
        Assert.Empty(db.ChangeTracker.Entries());
    }
}
