using AUTH.Domain.Entities;
using AUTH.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AUTH.Tests;

public sealed class AuthTestDatabase : IAsyncDisposable
{
    private const string Master = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=true";
    public string Source { get; } = "HRK_AuthTests_" + Guid.NewGuid().ToString("N");
    public string Target { get; } = "HRK_AuthTests_" + Guid.NewGuid().ToString("N");
    private string Connection(string name) => new SqlConnectionStringBuilder(Master) { InitialCatalog = name }.ConnectionString;
    public AuthDbContext Auth(string? name = null) => new(new DbContextOptionsBuilder<AuthDbContext>().UseSqlServer(Connection(name ?? Source)).Options);
    public ApplicationDbContext Application() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(Connection(Source)).Options);

    public static async Task<AuthTestDatabase> CreateAsync()
    {
        var database = new AuthTestDatabase();
        try
        {
            await using var db = database.Auth();
            await db.Database.EnsureCreatedAsync();
            await using var application = database.Application();
            await database.ExecuteAsync(application.Database.GenerateCreateScript());
            application.ApplicationRouteConfigs.Add(new HRK_ApplicationRouteConfig
            {
                AppCode = "dcs-game", Audience = "game-api", RoutePrefix = "/dcs-game", ApiPrefix = "/gateway/dcs-game",
                DefaultRoute = "/dcs-game/home", LoginTitle = "Game", IsActive = true
            });
            await application.SaveChangesAsync();
            return database;
        }
        catch { await database.DisposeAsync(); throw; }
    }

    public async Task ExecuteAsync(string sql, string? name = null)
    {
        await using var connection = new SqlConnection(Connection(name ?? Source));
        await connection.OpenAsync();
        foreach (var batch in Regex.Split(sql, @"(?m)^GO\s*$"))
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            await using var command = connection.CreateCommand();
            command.CommandText = batch;
            command.CommandTimeout = 60;
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task ApplyScriptAsync(string file)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        var relative = "Services/AUTH/AUTH.Infrastructure/Data/Scripts/" + file;
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, relative))) directory = directory.Parent;
        var script = await File.ReadAllTextAsync(Path.Combine(directory!.FullName, relative));
        // Replace only standalone database identifiers; never table names such as HRK_Users.
        script = Regex.Replace(script, @"\bHRK_AUTH\b", Target);
        script = Regex.Replace(script, @"\bHRK\b", Source);
        await ExecuteAsync(script);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var name in new[] { Source, Target })
        {
            if (!Regex.IsMatch(name, @"^HRK_AuthTests_[a-f0-9]{32}$")) throw new InvalidOperationException("Unsafe test database name.");
            using (var pooled = new SqlConnection(Connection(name))) SqlConnection.ClearPool(pooled);
            await using var connection = new SqlConnection(Master);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"IF DB_ID('{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END";
            await command.ExecuteNonQueryAsync();
        }
    }
}
