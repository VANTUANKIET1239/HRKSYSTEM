using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Configuration;
using Core.TransactionalMessaging.Outbox;
using Language.Application;
using Language.Domain;
using Language.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Language.Tests;

public sealed class LanguageSqlFactAttribute : FactAttribute
{
    public LanguageSqlFactAttribute()
    { if (Environment.GetEnvironmentVariable("HRK_LANGUAGE_SQL_TESTS") != "1") Skip = "Set HRK_LANGUAGE_SQL_TESTS=1 to use an isolated LocalDB test database."; }
}

public sealed class LanguageSqlTests : IAsyncLifetime
{
    private readonly string database = "HRK_LanguageTests_" + Guid.NewGuid().ToString("N");
    private const string Master = "Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=true";
    private string Connection => new SqlConnectionStringBuilder(Master) { InitialCatalog = database }.ConnectionString;
    private LanguageDbContext Context() => new(new DbContextOptionsBuilder<LanguageDbContext>().UseSqlServer(Connection).Options);

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("HRK_LANGUAGE_SQL_TESTS") != "1") return;
        await using var master = new SqlConnection(Master);
        await master.OpenAsync();
        await new SqlCommand($"CREATE DATABASE [{database}]", master).ExecuteNonQueryAsync();
        await Script("Migration_Language.sql");
    }

    public async Task DisposeAsync()
    {
        if (Environment.GetEnvironmentVariable("HRK_LANGUAGE_SQL_TESTS") != "1") return;
        if (!Regex.IsMatch(database, "^HRK_LanguageTests_[a-f0-9]{32}$")) throw new InvalidOperationException("Unsafe test database name.");
        SqlConnection.ClearAllPools();
        await using var master = new SqlConnection(Master);
        await master.OpenAsync();
        await new SqlCommand($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]", master).ExecuteNonQueryAsync();
    }

    [LanguageSqlFact]
    public async Task MigrationAndSeedAreRerunnableAndPublishedSnapshotsAreImmutable()
    {
        await Script("Migration_Language.sql");
        await Script("Seed_Language.sql");
        await Script("Seed_Language.sql");
        await using var db = Context();
        Assert.Equal(8, await db.Set<Language.Domain.LanguagePublication>().CountAsync());
        Assert.Equal(8, await db.Set<Core.TransactionalMessaging.Entities.OutboxMessage>().CountAsync());
        Assert.Equal(0, await Scalar("SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.HRK_LanguageRevisions') AND name=N'TranslationsJson'"));
        var reader = new PublishedLanguageReader(new UnitOfWork<LanguageDbContext>(db), new MemoryTranslations(), NullLogger<PublishedLanguageReader>.Instance);
        var bundle = await reader.GetFromDatabaseAsync("game", "en-US", default);
        Assert.Equal("Monthly check-in", bundle!.Translations[Core.Localization.TranslationIdentity.TextKey("Điểm danh tháng")]);
        Assert.Empty((await reader.GetFromDatabaseAsync("hrm", "vi-VN", default))!.Translations);
        Assert.Null(await reader.GetFromDatabaseAsync("unknown", "en-US", default));
        var expectedKeys = await AssertCatalogMatches(reader);
        Assert.Equal(expectedKeys, await db.Set<LanguageKey>().CountAsync());
        Assert.Equal(expectedKeys * 2, await db.Set<LanguageRevisionText>().CountAsync());
        await Script("Publish_Language.sql");
        Assert.Equal(8, await db.Set<LanguageRevision>().CountAsync());
        await Assert.ThrowsAsync<SqlException>(() => Execute("UPDATE dbo.HRK_LanguageRevisions SET PublishedBy=N'changed'"));
        await Assert.ThrowsAsync<SqlException>(() => Execute("UPDATE dbo.HRK_LanguageRevisionTexts SET [Value]=N'changed'"));
        await Assert.ThrowsAsync<SqlException>(() => Execute("DELETE FROM dbo.HRK_LanguageRevisionTexts"));
        await Assert.ThrowsAsync<SqlException>(() => Execute("UPDATE dbo.HRK_LanguageKeys SET [Key]=N'changed' WHERE Id=(SELECT MIN(Id) FROM dbo.HRK_LanguageKeys)"));
        await Assert.ThrowsAsync<SqlException>(() => Execute("INSERT dbo.HRK_LanguageKeys ([Group],[Key]) VALUES (N'game',N'EXTRA'); INSERT dbo.HRK_LanguageRevisionTexts SELECT RevisionId,(SELECT Id FROM dbo.HRK_LanguageKeys WHERE [Group]=N'game' AND [Key]=N'EXTRA'),N'Late edit' FROM dbo.HRK_LanguagePublications WHERE [Group]=N'game' AND Culture=N'en-US'"));
    }

    [LanguageSqlFact]
    public async Task PublishIsIdempotentAndWritesRevisionPointerAndOutboxInOneTransaction()
    {
        await Publish(("HELLO", "First"));
        await Publish(("HELLO", "First"));
        await Publish(("HELLO", "Second"));
        await using var db = Context();
        Assert.Equal(2, await db.Set<Language.Domain.LanguageRevision>().CountAsync());
        Assert.Equal(2, await db.Set<Core.TransactionalMessaging.Entities.OutboxMessage>().CountAsync());
        Assert.Equal(2, (await db.Set<Language.Domain.LanguagePublication>().SingleAsync()).RevisionId);
        await Execute("CREATE TRIGGER dbo.Test_RejectOutbox ON dbo.Language_OutboxMessages AFTER INSERT AS THROW 51010, 'Simulated outbox failure', 1;");
        await Assert.ThrowsAsync<SqlException>(() => Publish(("HELLO", "Must roll back"), ("NEW_KEY", "Must roll back too")));
        Assert.Equal(2, await db.Set<Language.Domain.LanguageRevision>().CountAsync());
        Assert.Equal(2, await db.Set<LanguageRevisionText>().CountAsync());
        Assert.Equal(1, await db.Set<LanguageKey>().CountAsync());
        Assert.Equal(2, await db.Set<Core.TransactionalMessaging.Entities.OutboxMessage>().CountAsync());
        Assert.Equal(2, (await db.Set<Language.Domain.LanguagePublication>().AsNoTracking().SingleAsync()).RevisionId);
    }

    [LanguageSqlFact]
    public async Task DelayedOutboxLoadsLatestRevisionAndRetryRemainsPendingAfterCacheFailure()
    {
        await Publish(("HELLO", "Old"));
        await Publish(("HELLO", "Current"));
        await using var db = Context();
        var store = new SqlServerOutboxStore<LanguageDbContext>(db);
        var first = Assert.Single(await store.ClaimAsync(1, "first-owner", 30, default));
        var identity = JsonSerializer.Deserialize<JsonElement>(first.Payload);
        var reader = new PublishedLanguageReader(new UnitOfWork<LanguageDbContext>(db), new MemoryTranslations(), NullLogger<PublishedLanguageReader>.Instance);
        var latest = await reader.GetFromDatabaseAsync(identity.GetProperty("group").GetString()!, identity.GetProperty("culture").GetString()!, default);
        Assert.Equal("Current", latest!.Translations["HELLO"]);
        await store.MarkFailedAttemptAsync(first, "first-owner", new OutboxOptions { RetryDelaysMs = [1] }, new IOException("Redis unavailable"), false, default);
        await Execute("UPDATE dbo.Language_OutboxMessages SET NextAttemptAt=DATEADD(second,-1,SYSUTCDATETIME()) WHERE Status=1;");
        var replay = await store.ClaimAsync(2, "second-owner", 30, default);
        Assert.Equal(2, replay.Count);
        foreach (var message in replay) Assert.Equal(1, await store.MarkPublishedAsync(message.Id, "second-owner", default));
        Assert.All(await db.Set<Core.TransactionalMessaging.Entities.OutboxMessage>().AsNoTracking().ToListAsync(), item => Assert.Equal(Core.TransactionalMessaging.Entities.OutboxStatus.Published, item.Status));
    }

    [LanguageSqlFact]
    public async Task TextRowsShareKeysAcrossCulturesAndPreserveCaseWhitespaceAndHistory()
    {
        await Publish(("HELLO", "First"), ("hello", "Lower case"));
        await Publish(("hello", "Lower case"), ("HELLO", "First")); // Order does not create a revision.
        await using var db = Context();
        Assert.Equal(1, await db.Set<LanguageRevision>().CountAsync());
        await PublishIn("game", "vi-VN", ("HELLO", "Xin chào"));
        await PublishIn("auth", "en-US", ("HELLO", "Auth greeting"));
        Assert.Equal(3, await db.Set<LanguageKey>().CountAsync()); // Two game keys, one auth key.
        await Publish(("HELLO", "First ")); // Trailing whitespace is a content change.
        await Publish(("HELLO", "first ")); // Case is a content change too.
        Assert.Equal(5, await db.Set<LanguageRevision>().CountAsync());
        var reader = new PublishedLanguageReader(new UnitOfWork<LanguageDbContext>(db), new MemoryTranslations(), NullLogger<PublishedLanguageReader>.Instance);
        var latest = await reader.GetFromDatabaseAsync("game", "en-US", default);
        Assert.Single(latest!.Translations);
        Assert.Equal("first ", latest.Translations["HELLO"]);
        Assert.Equal("Xin chào", (await reader.GetFromDatabaseAsync("game", "vi-VN", default))!.Translations["HELLO"]);
        Assert.Equal("Auth greeting", (await reader.GetFromDatabaseAsync("auth", "en-US", default))!.Translations["HELLO"]);
        Assert.Equal("First", await db.Set<LanguageRevisionText>().Where(x => x.RevisionId == 1)
            .Join(db.Set<LanguageKey>(), x => x.LanguageKeyId, key => key.Id, (x, key) => new { key.Key, x.Value })
            .Where(x => x.Key == "HELLO").Select(x => x.Value).SingleAsync());
        await Publish(); // An empty bundle is a valid new revision.
        Assert.Empty((await reader.GetFromDatabaseAsync("game", "en-US", default))!.Translations);
        await Publish();
        Assert.Equal(6, await db.Set<LanguageRevision>().CountAsync());
    }

    [LanguageSqlFact]
    public async Task PublishRejectsDuplicateKeysEmptyValuesAndUnsealedOrWrongGroupRows()
    {
        await Assert.ThrowsAsync<SqlException>(() => Publish(("DUP", "One"), ("DUP", "Two")));
        await Assert.ThrowsAsync<SqlException>(() => Publish(("EMPTY", "")));
        await Assert.ThrowsAsync<SqlException>(() => Publish((" PADDED ", "Text")));
        await Assert.ThrowsAsync<SqlException>(() => PublishIn("Game", "en-US", ("HELLO", "Text")));
        await Assert.ThrowsAsync<SqlException>(() => PublishIn("game", "EN-US", ("HELLO", "Text")));
        await Execute("INSERT dbo.HRK_LanguageRevisions ([Group],Culture,PublishedBy) VALUES (N'game',N'en-US',N'test'); INSERT dbo.HRK_LanguageKeys ([Group],[Key]) VALUES (N'auth',N'WRONG_GROUP')");
        await Assert.ThrowsAsync<SqlException>(() => Execute("INSERT dbo.HRK_LanguageRevisionTexts SELECT (SELECT MAX(Id) FROM dbo.HRK_LanguageRevisions),Id,N'Text' FROM dbo.HRK_LanguageKeys"));
        await Assert.ThrowsAsync<SqlException>(() => Execute("INSERT dbo.HRK_LanguagePublications SELECT [Group],Culture,Id FROM dbo.HRK_LanguageRevisions"));
        await using var db = Context();
        Assert.Empty(await db.Set<LanguageRevisionText>().ToListAsync());
        Assert.Empty(await db.Set<LanguagePublication>().ToListAsync());
        Assert.Empty(await db.Set<Core.TransactionalMessaging.Entities.OutboxMessage>().ToListAsync());
    }

    [LanguageSqlFact]
    public async Task MigrationConvertsAllLegacyJsonRevisionsWithoutChangingIdentityPointerOrOutbox()
    {
        await PrepareLegacySchema();
        await Execute("""
            SET IDENTITY_INSERT dbo.HRK_LanguageRevisions ON;
            INSERT dbo.HRK_LanguageRevisions (Id,[Group],Culture,TranslationsJson,PublishedAtUtc,PublishedBy)
                VALUES (41,N'game',N'vi-VN',@old,'2026-01-01',N'legacy-author'),
                       (42,N'game',N'vi-VN',@current,'2026-01-02',N'legacy-author'),
                       (43,N'game',N'en-US',N'{}','2026-01-03',N'legacy-author');
            SET IDENTITY_INSERT dbo.HRK_LanguageRevisions OFF;
            INSERT dbo.HRK_LanguagePublications VALUES (N'game',N'vi-VN',42),(N'game',N'en-US',43);
            INSERT dbo.Language_OutboxMessages (Id,EventName,PublisherName,RoutingKey,Payload,Sequence)
                VALUES ('00000000-0000-0000-0000-000000000042',N'language.published.v1',N'LanguageCache',N'language.published.v1',N'{"group":"game","culture":"vi-VN","revision":42}',42);
            """, new SqlParameter("@old", JsonSerializer.Serialize(new Dictionary<string, string> { ["HELLO"] = "Xin chào {{name}}!", ["hello"] = "Dòng 1\nDòng 2 'quoted'" })),
            new SqlParameter("@current", JsonSerializer.Serialize(new Dictionary<string, string> { ["HELLO"] = "Đã thay đổi " })));
        await Script("Migration_Language.sql");
        await Script("Migration_Language.sql");
        await using var db = Context();
        Assert.Equal(3, await db.Set<LanguageRevision>().CountAsync());
        Assert.Equal(2, await db.Set<LanguageKey>().CountAsync());
        Assert.Equal(3, await db.Set<LanguageRevisionText>().CountAsync());
        Assert.Equal(new DateTime(2026, 1, 1), (await db.Set<LanguageRevision>().SingleAsync(x => x.Id == 41)).PublishedAtUtc);
        Assert.All(await db.Set<LanguageRevision>().ToListAsync(), row => Assert.Equal("legacy-author", row.PublishedBy));
        var reader = new PublishedLanguageReader(new UnitOfWork<LanguageDbContext>(db), new MemoryTranslations(), NullLogger<PublishedLanguageReader>.Instance);
        var current = await reader.GetFromDatabaseAsync("game", "vi-VN", default);
        Assert.Equal(42, current!.Revision);
        Assert.Equal("Đã thay đổi ", current.Translations["HELLO"]);
        Assert.Empty((await reader.GetFromDatabaseAsync("game", "en-US", default))!.Translations);
        Assert.Equal(2, await db.Set<LanguageRevisionText>().CountAsync(x => x.RevisionId == 41));
        Assert.Contains(await db.Set<LanguageRevisionText>().Where(x => x.RevisionId == 41).Select(x => x.Value).ToListAsync(), value => value == "Dòng 1\nDòng 2 'quoted'");
        Assert.Equal(42, (await db.Set<Core.TransactionalMessaging.Entities.OutboxMessage>().SingleAsync()).Sequence);
        Assert.Equal(0, await Scalar("SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.HRK_LanguageRevisions') AND name=N'TranslationsJson'"));
        await Publish(("HELLO", "After migration"));
        Assert.Equal(44, (await reader.GetFromDatabaseAsync("game", "en-US", default))!.Revision);
    }

    [LanguageSqlFact]
    public async Task InvalidLegacyDataRollsBackConversionAndRetainsOriginalJson()
    {
        await PrepareLegacySchema();
        await Execute("INSERT dbo.HRK_LanguageRevisions ([Group],Culture,TranslationsJson,PublishedAtUtc,PublishedBy) VALUES (N'game',N'en-US',N'{\"DUP\":\"One\",\"DUP\":\"Two\"}',SYSUTCDATETIME(),N'legacy')");
        await Assert.ThrowsAsync<SqlException>(() => Script("Migration_Language.sql"));
        Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.HRK_LanguageRevisions') AND name=N'TranslationsJson'"));
        Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM dbo.HRK_LanguageRevisions WHERE ISJSON(TranslationsJson)=1"));
        Assert.Equal(0, await Scalar("SELECT COUNT(*) FROM sys.tables WHERE name=N'HRK_LanguageRevisionTexts'"));
    }

    private async Task PrepareLegacySchema()
    {
        // Only the isolated scratch database is altered; recreate the former schema shape.
        await Execute("""
            DROP TABLE dbo.HRK_LanguageRevisionTexts;
            DROP TABLE dbo.HRK_LanguageKeys;
            ALTER TABLE dbo.HRK_LanguageRevisions ADD TranslationsJson NVARCHAR(MAX) NOT NULL;
            """);
        await Execute("""
            ALTER TABLE dbo.HRK_LanguageRevisions ADD CONSTRAINT CK_LanguageRevisions_Json
                CHECK (ISJSON(TranslationsJson)=1 AND LEFT(LTRIM(TranslationsJson),1)='{');
            ALTER TABLE dbo.HRK_LanguageRevisions ALTER COLUMN PublishedAtUtc DATETIME2 NOT NULL;
            ALTER TABLE dbo.HRK_LanguageRevisions ADD CONSTRAINT DF_TestLegacyPublishedAt DEFAULT SYSUTCDATETIME() FOR PublishedAtUtc;
            """);
    }

    private static async Task<int> AssertCatalogMatches(PublishedLanguageReader reader)
    {
        var totalKeys = 0;
        using var catalog = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(RepositoryRoot(), "tools/localization/catalog.json")));
        foreach (var group in catalog.RootElement.EnumerateObject().Where(x => x.Name != "keys"))
        foreach (var culture in new[] { "vi-VN", "en-US" })
        {
            var expected = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in group.Value.EnumerateArray())
                expected.Add(Core.Localization.TranslationIdentity.TextKey(row[0].GetString()!),
                    row[culture == "en-US" ? 1 : row.GetArrayLength() > 2 ? 2 : 0].GetString()!);
            if (catalog.RootElement.GetProperty("keys").TryGetProperty(group.Name, out var keys))
                foreach (var key in keys.EnumerateObject()) expected.Add(key.Name, key.Value[culture == "vi-VN" ? 0 : 1].GetString()!);
            var actual = (await reader.GetFromDatabaseAsync(group.Name, culture, default))!.Translations;
            if (culture == "vi-VN") totalKeys += expected.Count;
            Assert.Equal(expected.Count, actual.Count);
            foreach (var (key, value) in expected) Assert.Equal(value, actual[key]);
        }
        return totalKeys;
    }

    private Task Publish(params (string Key, string Value)[] texts) => PublishIn("game", "en-US", texts);
    private Task PublishIn(string group, string culture, params (string Key, string Value)[] texts)
    {
        var rows = new DataTable();
        rows.Columns.Add("Key", typeof(string));
        rows.Columns.Add("Value", typeof(string));
        foreach (var (key, value) in texts) rows.Rows.Add(key, value);
        return Execute("EXEC dbo.HRK_PublishLanguageBundle @Group=@group, @Culture=@culture, @Texts=@texts",
            new SqlParameter("@group", group), new SqlParameter("@culture", culture),
            new SqlParameter("@texts", SqlDbType.Structured) { TypeName = "dbo.HRK_LanguageTextTable", Value = rows });
    }

    private async Task<int> Scalar(string sql)
    {
        await using var connection = new SqlConnection(Connection);
        await connection.OpenAsync();
        return Convert.ToInt32(await new SqlCommand(sql, connection).ExecuteScalarAsync());
    }
    private async Task Execute(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(Connection);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }
    private async Task Script(string name)
    {
        var text = await File.ReadAllTextAsync(Path.Combine(RepositoryRoot(), "Services/HRKLANGUAGE/Language.Infrastructure/Data/Scripts", name));
        foreach (var batch in Regex.Split(text, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            if (!string.IsNullOrWhiteSpace(batch)) await Execute(batch);
    }

    private static string RepositoryRoot()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "HRK.sln"))) root = root.Parent;
        return root!.FullName;
    }
}
