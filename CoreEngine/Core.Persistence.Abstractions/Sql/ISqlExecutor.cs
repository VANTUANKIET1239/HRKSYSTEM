namespace Core.Persistence.SqlExecutor;

/// <summary>
/// Executes buffered SQL on the owning UnitOfWork's connection and current transaction.
/// Does not save tracked EF changes, start transactions, retry, or own the connection.
/// Named-object APIs target SQL Server. Names must be developer-controlled identifiers.
/// </summary>
public interface ISqlExecutor
{
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null,
        CancellationToken cancellationToken = default);
    Task<int> ExecuteAsync(string sql, object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>Reads all rows from a view; does not imply ordering or pagination.</summary>
    Task<IReadOnlyList<T>> QueryViewAsync<T>(string viewName,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedureName, object? parameters = null,
        CancellationToken cancellationToken = default);
    /// <summary>Returns the provider's affected-row count, not the SP's RETURN value or a success flag.</summary>
    Task<int> ExecuteProcedureAsync(string procedureName, object? parameters = null,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Calls a TVF using parameter order discovered in the database. Supply every argument,
    /// including nulls, as a property object or a string/object dictionary. Defaults are not inferred.
    /// </summary>
    Task<IReadOnlyList<T>> QueryFunctionAsync<T>(string functionName, object? parameters = null,
        CancellationToken cancellationToken = default);
    /// <summary>Clears this executor's scoped TVF metadata cache after database signature changes.</summary>
    void ClearFunctionMetadataCache();

    // Compatibility with the original concrete executor API.
    Task<IEnumerable<T>> QueryStoredProcAsync<T>(string procName, object? parameters = null,
        CancellationToken cancellationToken = default);
}
