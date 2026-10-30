using System.Text.RegularExpressions;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.Services;
using GAME.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;

internal static class GameOperationSqlScript
{
    public static async Task ApplyAsync(GameDbContext db)
    {
        const string relative = "Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Create_GameOperationProcedures.sql";
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, relative)))
            directory = directory.Parent;
        if (directory == null)
            throw new FileNotFoundException("Cannot locate the GAME operation procedure deployment script.", relative);

        var script = await File.ReadAllTextAsync(Path.Combine(directory.FullName, relative));
        foreach (var batch in Regex.Split(script, @"^[ \t]*GO[ \t]*\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            if (!string.IsNullOrWhiteSpace(batch))
                await db.Database.ExecuteSqlRawAsync(batch);
    }
}
