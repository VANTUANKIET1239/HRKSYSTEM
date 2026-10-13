using System.Data;
using Core.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Core.Modules.Tests;

public sealed class ExecutorSqlFactAttribute : FactAttribute
{
    public ExecutorSqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("HRK_SQL_EXECUTOR_TESTS") != "1")
            Skip = "Set HRK_SQL_EXECUTOR_TESTS=1 to run isolated SQL Server LocalDB executor tests.";
    }
}

public class SqlExecutorIntegrationTests(SqlExecutorDatabase database) : IClassFixture<SqlExecutorDatabase>
{
    [ExecutorSqlFact]
    public async Task ViewAndProcedureMapBufferedDtosWithoutTrackingOrOwningConnection()
    {
        var id = Guid.NewGuid();
        await using var db = database.Context();
        await using var uow = new UnitOfWork<SqlExecutorDatabase.ProbeContext>(db);
        await uow.Sql.ExecuteProcedureAsync("probe.usp_Insert", new { Id = id, Label = "Tướng hiếm", Amount = 12.34m });
        Assert.Equal(ConnectionState.Closed, db.Database.GetDbConnection().State);

        var rows = await uow.Sql.QueryViewAsync<SqlExecutorDatabase.Probe>("probe.vw_Records");
        var row = Assert.Single(rows, row => row.Id == id);
        Assert.Equal("Tướng hiếm", row.Label);
        Assert.Equal(12.34m, row.Amount);
        Assert.Empty(db.ChangeTracker.Entries());

        var echo = Assert.Single(await uow.Sql.QueryProcedureAsync<FunctionRow>("probe.usp_Echo", new { Value = 17 }));
        Assert.Equal(17, echo.Value);
        Assert.Null(echo.Label);
        Assert.Equal(18, Assert.Single(await uow.Sql.QueryStoredProcAsync<int>("probe.usp_Echo", new { Value = 18 })));

        await db.Database.OpenConnectionAsync();
        await uow.Sql.QueryViewAsync<SqlExecutorDatabase.Probe>("probe.vw_Records");
        Assert.Equal(ConnectionState.Open, db.Database.GetDbConnection().State);
        await db.Database.CloseConnectionAsync();
        // Results remain usable after the command/connection has closed.
        Assert.Contains(rows, row => row.Id == id);
    }

    [ExecutorSqlFact]
    public async Task TvfUsesDatabaseParameterOrderAndBindsValuesSafely()
    {
        await using var db = database.Context();
        await using var uow = new UnitOfWork<SqlExecutorDatabase.ProbeContext>(db);
        const string text = "Tướng '; DROP TABLE dbo.Records;--";
        var row = Assert.Single(await uow.Sql.QueryFunctionAsync<FunctionRow>("probe.fn_Combine",
            new { Label = text, RightValue = 3, LeftValue = 2 }));
        Assert.Equal(203, row.Value);
        Assert.Equal(text, row.Label);

        var dictionaryRow = Assert.Single(await uow.Sql.QueryFunctionAsync<FunctionRow>("probe.fn_Combine",
            new Dictionary<string, object?> { ["@label"] = null, ["rightvalue"] = 5, ["LEFTVALUE"] = 4 }));
        Assert.Equal(405, dictionaryRow.Value);
        Assert.Null(dictionaryRow.Label);
        Assert.True(await uow.CanConnectAsync());
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [ExecutorSqlFact]
    public async Task SupportsZeroArgumentAndMultiStatementTvfs()
    {
        await using var db = database.Context();
        await using var uow = new UnitOfWork<SqlExecutorDatabase.ProbeContext>(db);
        Assert.Equal(7, Assert.Single(await uow.Sql.QueryFunctionAsync<int>("fn_Zero")));
        Assert.Equal(8, Assert.Single(await uow.Sql.QueryFunctionAsync<int>("probe.fn_Multi", new { Value = 8 })));
    }

    [ExecutorSqlFact]
    public async Task MissingExtraAndUnknownFunctionArgumentsFailClearly()
    {
        await using var db = database.Context();
        await using var uow = new UnitOfWork<SqlExecutorDatabase.ProbeContext>(db);
        var error = await Assert.ThrowsAsync<ArgumentException>(() => uow.Sql.QueryFunctionAsync<int>("probe.fn_Combine",
            new { LeftValue = 1, Typo = 2 }));
        Assert.Contains("RightValue", error.Message);
        Assert.Contains("Label", error.Message);
        Assert.Contains("Typo", error.Message);
        await Assert.ThrowsAsync<ArgumentException>(() => uow.Sql.QueryFunctionAsync<int>("fn_Zero", new { Unexpected = 1 }));
        await Assert.ThrowsAsync<ArgumentException>(() => uow.Sql.QueryFunctionAsync<int>("fn_Zero", 1));
        await Assert.ThrowsAsync<ArgumentException>(() => uow.Sql.QueryFunctionAsync<int>("fn_Zero",
            new Dictionary<string, object?> { ["Value"] = 1, ["@value"] = 2 }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.Sql.QueryFunctionAsync<int>("probe.fn_Missing"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.Sql.QueryFunctionAsync<int>("probe.usp_Echo"));
    }

    [ExecutorSqlFact]
    public async Task MetadataCacheCanBeRefreshedAfterChangingFunctionSignature()
    {
        await using var db = database.Context();
        await using var uow = new UnitOfWork<SqlExecutorDatabase.ProbeContext>(db);
        await uow.Sql.ExecuteAsync("CREATE FUNCTION probe.fn_Changing(@First int) RETURNS TABLE AS RETURN SELECT @First AS Value");
        Assert.Equal(1, Assert.Single(await uow.Sql.QueryFunctionAsync<int>("probe.fn_Changing", new { First = 1 })));
        await uow.Sql.ExecuteAsync("ALTER FUNCTION probe.fn_Changing(@Second int) RETURNS TABLE AS RETURN SELECT @Second AS Value");
        await Assert.ThrowsAsync<ArgumentException>(() => uow.Sql.QueryFunctionAsync<int>("probe.fn_Changing", new { Second = 2 }));
        uow.Sql.ClearFunctionMetadataCache();
        Assert.Equal(2, Assert.Single(await uow.Sql.QueryFunctionAsync<int>("probe.fn_Changing", new { Second = 2 })));
    }

    [ExecutorSqlFact]
    public async Task EfAndProcedureWritesRollbackTogetherOnBusinessFailure()
    {
        await using var db = database.Context();
        await using var uow = new UnitOfWork<SqlExecutorDatabase.ProbeContext>(db);
        var efId = Guid.NewGuid();
        var sqlId = Guid.NewGuid();
        var original = new Core.Common.Errors.BusinessException("TEST_FAILURE", "Expected failure");
        var error = await Assert.ThrowsAsync<Core.Common.Errors.BusinessException>(() => uow.ExecuteInTransactionAsync(async () =>
        {
            await uow.Repository<SqlExecutorDatabase.Probe>().AddAsync(new() { Id = efId, Amount = 1 });
            await uow.SaveChangesAsync();
            await uow.Sql.ExecuteProcedureAsync("probe.usp_Insert", new { Id = sqlId, Label = "SQL", Amount = 2m });
            var counts = await uow.Sql.QueryAsync<int>("SELECT COUNT(*) FROM dbo.Records WHERE Id IN (@efId, @sqlId)", new { efId, sqlId });
            Assert.Equal(2, Assert.Single(counts));
            throw original;
        }));
        Assert.Same(original, error);
        Assert.False(uow.HasActiveTransaction);
        Assert.Empty(db.ChangeTracker.Entries());
        await using var verification = database.Context();
        Assert.False(await verification.Records.AnyAsync(row => row.Id == efId || row.Id == sqlId));
    }

    [ExecutorSqlFact]
    public async Task RetryUsesReplacementTransactionAndSuccessfulCommitPersistsBothWrites()
    {
        await using var db = database.Context(retries: true);
        await using var uow = new UnitOfWork<SqlExecutorDatabase.ProbeContext>(db);
        var executor = uow.Sql;
        var efId = Guid.NewGuid();
        var sqlId = Guid.NewGuid();
        var attempts = 0;
        var transactions = new List<Guid>();
        await uow.ExecuteInTransactionAsync(async () =>
        {
            transactions.Add(uow.CurrentTransactionId!.Value);
            await uow.Repository<SqlExecutorDatabase.Probe>().AddAsync(new() { Id = efId, Amount = 3 });
            await executor.ExecuteProcedureAsync("probe.usp_Insert", new { Id = sqlId, Label = "Retry", Amount = 4m });
            Assert.Equal(102, Assert.Single(await executor.QueryFunctionAsync<int>("probe.fn_Combine", new { LeftValue = 1, RightValue = 2, Label = (string?)null })));
            if (++attempts == 1)
                throw new TimeoutException("Retry the whole operation.");
        });
        Assert.Equal(2, attempts);
        Assert.NotEqual(transactions[0], transactions[1]);
        Assert.False(uow.HasActiveTransaction);
        await using var verification = database.Context();
        Assert.Equal(2, await verification.Records.CountAsync(row => row.Id == efId || row.Id == sqlId));
        Assert.Equal(7, Assert.Single(await executor.QueryFunctionAsync<int>("fn_Zero")));
    }

    [ExecutorSqlFact]
    public async Task CommandTimeoutUsesEfContextConfiguration()
    {
        await using var db = database.Context();
        await using var uow = new UnitOfWork<SqlExecutorDatabase.ProbeContext>(db);
        db.Database.SetCommandTimeout(1);
        var error = await Assert.ThrowsAsync<SqlException>(() => uow.Sql.ExecuteProcedureAsync("probe.usp_Wait"));
        Assert.Equal(-2, error.Number);
        Assert.Equal(ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [ExecutorSqlFact]
    public async Task CancellationInterruptsAnExecutingCommand()
    {
        await using var db = database.Context();
        await using var uow = new UnitOfWork<SqlExecutorDatabase.ProbeContext>(db);
        using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uow.Sql.QueryProcedureAsync<int>("probe.usp_Wait", cancellationToken: source.Token));
        Assert.Equal(ConnectionState.Closed, db.Database.GetDbConnection().State);
        using var writeSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uow.Sql.ExecuteProcedureAsync("probe.usp_Wait", cancellationToken: writeSource.Token));
        Assert.Equal(ConnectionState.Closed, db.Database.GetDbConnection().State);
        Assert.Equal(7, Assert.Single(await uow.Sql.QueryFunctionAsync<int>("fn_Zero")));
    }

    public sealed class FunctionRow
    {
        public int Value { get; set; }
        public string? Label { get; set; }
    }
}

public sealed class SqlExecutorDatabase : IAsyncLifetime
{
    private const string Master = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=true";
    private readonly string _name = "HRK_SqlExecutorTests_" + Guid.NewGuid().ToString("N");
    private string Connection => new SqlConnectionStringBuilder(Master) { InitialCatalog = _name }.ConnectionString;

    public ProbeContext Context(bool retries = false) => new(new DbContextOptionsBuilder<ProbeContext>()
        .UseSqlServer(Connection, options =>
        {
            options.CommandTimeout(15);
            if (retries) options.EnableRetryOnFailure(1, TimeSpan.Zero, null);
        }).Options);

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("HRK_SQL_EXECUTOR_TESTS") != "1") return;
        await using var db = Context();
        await db.Database.EnsureCreatedAsync();
        string[] batches =
        [
            "CREATE SCHEMA probe",
            "CREATE VIEW probe.vw_Records AS SELECT Id, Label, Amount FROM dbo.Records",
            "CREATE PROCEDURE probe.usp_Insert @Id uniqueidentifier, @Label nvarchar(200), @Amount decimal(18,2) AS BEGIN SET NOCOUNT ON; INSERT dbo.Records(Id, Label, Amount) VALUES(@Id, @Label, @Amount); END",
            "CREATE PROCEDURE probe.usp_Echo @Value int, @Label nvarchar(200) = NULL AS BEGIN SET NOCOUNT ON; SELECT @Value AS Value, @Label AS Label; END",
            "CREATE FUNCTION probe.fn_Combine(@LeftValue int, @RightValue int, @Label nvarchar(200)) RETURNS TABLE AS RETURN SELECT @LeftValue * 100 + @RightValue AS Value, @Label AS Label",
            "CREATE FUNCTION dbo.fn_Zero() RETURNS TABLE AS RETURN SELECT 7 AS Value",
            "CREATE FUNCTION probe.fn_Multi(@Value int) RETURNS @Rows TABLE(Value int) AS BEGIN INSERT @Rows VALUES(@Value); RETURN; END",
            "CREATE PROCEDURE probe.usp_Wait AS BEGIN WAITFOR DELAY '00:00:05'; SELECT 1 AS Value; END"
        ];
        foreach (var batch in batches)
            await db.Database.ExecuteSqlRawAsync(batch);
    }

    public async Task DisposeAsync()
    {
        if (Environment.GetEnvironmentVariable("HRK_SQL_EXECUTOR_TESTS") != "1") return;
        using (var pooled = new SqlConnection(Connection)) SqlConnection.ClearPool(pooled);
        await using var master = new SqlConnection(Master);
        await master.OpenAsync();
        await using var command = master.CreateCommand();
        // Only this fixture's generated scratch database can be removed.
        command.CommandText = $"IF DB_ID('{_name}') IS NOT NULL BEGIN ALTER DATABASE [{_name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_name}]; END";
        await command.ExecuteNonQueryAsync();
    }

    public sealed class Probe
    {
        public Guid Id { get; set; }
        public string? Label { get; set; }
        public decimal Amount { get; set; }
    }

    public sealed class ProbeContext(DbContextOptions<ProbeContext> options) : DbContext(options)
    {
        public DbSet<Probe> Records => Set<Probe>();
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Probe>().ToTable("Records", "dbo");
            builder.Entity<Probe>().Property(row => row.Id).ValueGeneratedNever();
            builder.Entity<Probe>().Property(row => row.Label).HasMaxLength(200);
            builder.Entity<Probe>().Property(row => row.Amount).HasPrecision(18, 2);
        }
    }
}
