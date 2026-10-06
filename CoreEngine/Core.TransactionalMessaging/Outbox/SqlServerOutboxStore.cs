using System.Data;
using System.Data.Common;
using Core.TransactionalMessaging.Configuration;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Core.TransactionalMessaging.Outbox;

public sealed class SqlServerOutboxStore<TDbContext> : IOutboxStore
    where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly string _qualifiedTable;

    public SqlServerOutboxStore(TDbContext db)
    {
        _db = db;
        _qualifiedTable = SqlIdentifier.FromEntity<OutboxMessage>(db);
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimAsync(
        int batchSize,
        string lockToken,
        int leaseSeconds,
        CancellationToken ct)
    {
        var result = new List<OutboxMessage>();
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            result.Clear();
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            var connection = _db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(ct);
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = $"""
                ;WITH Claimable AS
                (
                    SELECT TOP (@BatchSize) *
                    FROM {_qualifiedTable} WITH (UPDLOCK, READPAST, ROWLOCK, READCOMMITTEDLOCK)
                    WHERE
                        (
                            Status = @Pending
                            OR
                            (
                                Status = @Processing
                                AND
                                (
                                    LockedUntil < SYSUTCDATETIME()
                                    OR LockToken = @LockToken
                                )
                            )
                        )
                        AND (NextAttemptAt IS NULL OR NextAttemptAt <= SYSUTCDATETIME())
                    ORDER BY CreatedAt, Id
                )
                UPDATE Claimable
                SET
                    Status = @Processing,
                    LockToken = @LockToken,
                    LockedUntil = DATEADD(SECOND, @LeaseSeconds, SYSUTCDATETIME())
                OUTPUT
                    inserted.Id, inserted.EventName, inserted.EventVersion,
                    inserted.PublisherName, inserted.RoutingKey, inserted.Payload,
                    inserted.Status, inserted.RetryCount, inserted.CreatedAt,
                    inserted.NextAttemptAt, inserted.LastAttemptAt, inserted.PublishedAt,
                    inserted.LockToken, inserted.LockedUntil, inserted.LastError,
                    inserted.PartitionKey, inserted.Sequence,
                    inserted.CorrelationId, inserted.TraceParent, inserted.TraceState;
                """;
            AddParameter(command, "@BatchSize", Math.Max(1, batchSize));
            AddParameter(command, "@Pending", (byte)OutboxStatus.Pending);
            AddParameter(command, "@Processing", (byte)OutboxStatus.Processing);
            AddParameter(command, "@LockToken", lockToken);
            AddParameter(command, "@LeaseSeconds", Math.Max(1, leaseSeconds));

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(Map(reader));
            }

            await reader.CloseAsync();
            await transaction.CommitAsync(ct);
        });

        return result;
    }

    public Task<int> MarkPublishedAsync(Guid id, string lockToken, CancellationToken ct)
    {
        var sql = $"""
            UPDATE {_qualifiedTable}
            SET Status = @Status,
                PublishedAt = SYSUTCDATETIME(), LastAttemptAt = SYSUTCDATETIME(),
                LockToken = NULL, LockedUntil = NULL, LastError = NULL
            WHERE Id = @Id
              AND Status = @Processing
              AND LockToken = @LockToken;
            """;

        return ExecuteNonQueryAsync(
            sql,
            ct,
            ("@Status", (byte)OutboxStatus.Published),
            ("@Id", id),
            ("@Processing", (byte)OutboxStatus.Processing),
            ("@LockToken", lockToken));
    }

    public Task<int> MarkFailedAttemptAsync(
        OutboxMessage message,
        string lockToken,
        OutboxOptions options,
        Exception exception,
        bool permanent,
        CancellationToken ct)
    {
        var nextRetryCount = message.RetryCount + 1;
        var exhausted = permanent || nextRetryCount >= Math.Max(1, options.MaxRetryCount);
        var status = exhausted ? OutboxStatus.Failed : OutboxStatus.Pending;
        DateTime? nextAttemptAt = exhausted
            ? null
            : DateTime.UtcNow.AddMilliseconds(ResolveDelay(options.RetryDelaysMs, nextRetryCount - 1));
        // Error messages can contain tokens, SQL parameters or connection strings.
        var error = Truncate(exception.GetType().Name, options.ErrorMaxLength);
        var sql = $"""
            UPDATE {_qualifiedTable}
            SET Status = @Status, RetryCount = @RetryCount,
                NextAttemptAt = @NextAttemptAt, LastAttemptAt = SYSUTCDATETIME(),
                LockToken = NULL, LockedUntil = NULL, LastError = @LastError
            WHERE Id = @Id
              AND Status = @Processing
              AND LockToken = @LockToken;
            """;

        return ExecuteNonQueryAsync(
            sql,
            ct,
            ("@Status", (byte)status),
            ("@RetryCount", nextRetryCount),
            ("@NextAttemptAt", (object?)nextAttemptAt ?? DBNull.Value),
            ("@LastError", error),
            ("@Id", message.Id),
            ("@Processing", (byte)OutboxStatus.Processing),
            ("@LockToken", lockToken));
    }

    private static int ResolveDelay(IReadOnlyList<int> delays, int attemptIndex)
    {
        if (delays.Count == 0)
        {
            return 1000;
        }

        return Math.Max(1, delays[Math.Min(Math.Max(0, attemptIndex), delays.Count - 1)]);
    }

    private static string Truncate(string value, int maxLength)
    {
        var length = Math.Max(1, maxLength);
        return value.Length <= length ? value : value[..length];
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private async Task<int> ExecuteNonQueryAsync(
        string sql,
        CancellationToken ct,
        params (string Name, object Value)[] parameters)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            AddParameter(command, name, value);
        }

        return await command.ExecuteNonQueryAsync(ct);
    }

    private static OutboxMessage Map(DbDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        EventName = reader.GetString(1),
        EventVersion = reader.GetInt32(2),
        PublisherName = reader.GetString(3),
        RoutingKey = reader.GetString(4),
        Payload = reader.GetString(5),
        Status = (OutboxStatus)reader.GetByte(6),
        RetryCount = reader.GetInt32(7),
        CreatedAt = reader.GetDateTime(8),
        NextAttemptAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
        LastAttemptAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
        PublishedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
        LockToken = reader.IsDBNull(12) ? null : reader.GetString(12),
        LockedUntil = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
        LastError = reader.IsDBNull(14) ? null : reader.GetString(14),
        PartitionKey = reader.IsDBNull(15) ? null : reader.GetString(15),
        Sequence = reader.IsDBNull(16) ? null : reader.GetInt64(16),
        CorrelationId = reader.IsDBNull(17) ? null : reader.GetString(17),
        TraceParent = reader.IsDBNull(18) ? null : reader.GetString(18),
        TraceState = reader.IsDBNull(19) ? null : reader.GetString(19)
    };
}
