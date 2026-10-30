using System.Data;
using Core.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Core.Modules.Tests;

public sealed partial class SqlExecutorDatabase : IAsyncLifetime
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
}
