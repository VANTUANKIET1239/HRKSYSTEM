using System.Data;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Core.TransactionalMessaging.Inbox;

public sealed class SqlServerInboxStore<TDbContext> : IInboxStore
    where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly string _qualifiedTable;

    public SqlServerInboxStore(TDbContext db)
    {
        _db = db;
        _qualifiedTable = SqlIdentifier.FromEntity<InboxMessage>(db);
    }

    public async Task<bool> TryReserveAsync(
        string consumerName,
        string messageId,
        CancellationToken ct = default)
    {
        if (_db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Inbox reservation must run inside the same transaction as the business operation.");
        }

        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = _db.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText = $"""
            INSERT INTO {_qualifiedTable} (ConsumerName, MessageId, ProcessedAt)
            SELECT @ConsumerName, @MessageId, SYSUTCDATETIME()
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM {_qualifiedTable} WITH (UPDLOCK, HOLDLOCK)
                WHERE ConsumerName = @ConsumerName
                  AND MessageId = @MessageId
            );
            """;
        AddParameter(command, "@ConsumerName", consumerName);
        AddParameter(command, "@MessageId", messageId);

        return await command.ExecuteNonQueryAsync(ct) == 1;
    }

    public async Task<int> DeleteExpiredAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            DELETE TOP (@BatchSize)
            FROM {_qualifiedTable} WITH (ROWLOCK, READPAST)
            WHERE ProcessedAt < @OlderThanUtc;
            """;
        AddParameter(command, "@BatchSize", Math.Max(1, batchSize));
        AddParameter(command, "@OlderThanUtc", olderThanUtc);

        return await command.ExecuteNonQueryAsync(ct);
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
