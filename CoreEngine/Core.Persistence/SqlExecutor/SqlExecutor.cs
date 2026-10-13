using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;

namespace Core.Persistence.SqlExecutor;

public sealed class SqlExecutor(DbContext context) : ISqlExecutor
{
    // Scoped with the UOW, never shared between contexts, databases or tenants.
    private readonly Dictionary<string, string[]> _functionParameters = new(StringComparer.Ordinal);

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null,
        CancellationToken cancellationToken = default) =>
        await QueryBufferedAsync<T>(sql, parameters, CommandType.Text, cancellationToken);

    public Task<int> ExecuteAsync(string sql, object? parameters = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(sql, parameters, CommandType.Text, cancellationToken);

    public Task<IReadOnlyList<T>> QueryViewAsync<T>(string viewName,
        CancellationToken cancellationToken = default)
    {
        var name = NamedObject(viewName, cancellationToken);
        return QueryBufferedAsync<T>($"SELECT * FROM {name.Quoted}", null, CommandType.Text, cancellationToken);
    }

    public Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedureName, object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var name = NamedObject(procedureName, cancellationToken);
        return QueryBufferedAsync<T>(name.Quoted, parameters, CommandType.StoredProcedure, cancellationToken);
    }

    public Task<int> ExecuteProcedureAsync(string procedureName, object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var name = NamedObject(procedureName, cancellationToken);
        return ExecuteCommandAsync(name.Quoted, parameters, CommandType.StoredProcedure, cancellationToken);
    }

    public async Task<IEnumerable<T>> QueryStoredProcAsync<T>(string procName, object? parameters = null,
        CancellationToken cancellationToken = default) =>
        await QueryProcedureAsync<T>(procName, parameters, cancellationToken);

    public async Task<IReadOnlyList<T>> QueryFunctionAsync<T>(string functionName, object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var name = NamedObject(functionName, cancellationToken);
        var values = ReadArguments(parameters);
        var parameterNames = await GetFunctionParametersAsync(name, cancellationToken);
        var missing = parameterNames.Where(parameter => !values.ContainsKey(parameter)).ToArray();
        var extra = values.Keys.Except(parameterNames, StringComparer.OrdinalIgnoreCase).ToArray();
        if (missing.Length != 0 || extra.Length != 0)
            throw new ArgumentException(
                $"Arguments for '{name.Quoted}' do not match its signature. " +
                $"Missing: [{string.Join(", ", missing)}]. Unexpected: [{string.Join(", ", extra)}].", nameof(parameters));

        // Generated placeholders avoid trusting metadata or caller keys as SQL fragments.
        var args = new DynamicParameters();
        var placeholders = new string[parameterNames.Length];
        for (var i = 0; i < parameterNames.Length; i++)
        {
            placeholders[i] = $"@arg{i}";
            args.Add(placeholders[i], values[parameterNames[i]]);
        }
        return await QueryBufferedAsync<T>($"SELECT * FROM {name.Quoted}({string.Join(", ", placeholders)})",
            args, CommandType.Text, cancellationToken);
    }

    public void ClearFunctionMetadataCache() => _functionParameters.Clear();

    private SqlServerObjectName NamedObject(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parsed = SqlServerObjectName.Parse(name);
        if (!context.Database.IsSqlServer())
            throw new NotSupportedException("Named view, procedure and TVF APIs require the SQL Server EF Core provider.");
        return parsed;
    }

    private async Task<string[]> GetFunctionParametersAsync(SqlServerObjectName name, CancellationToken cancellationToken)
    {
        if (_functionParameters.TryGetValue(name.Quoted, out var cached))
            return cached;

        const string sql = """
            SELECT p.name AS ParameterName
            FROM sys.objects AS o
            INNER JOIN sys.schemas AS s ON s.schema_id = o.schema_id
            LEFT JOIN sys.parameters AS p ON p.object_id = o.object_id AND p.parameter_id > 0
            WHERE s.name = @Schema AND o.name = @Name AND o.type IN ('IF', 'TF', 'FT')
            ORDER BY p.parameter_id
            """;
        var rows = await QueryBufferedAsync<FunctionParameter>(sql,
            new { name.Schema, name.Name }, CommandType.Text, cancellationToken);
        if (rows.Count == 0)
            throw new InvalidOperationException(
                $"Table-valued function '{name.Quoted}' was not found or its metadata is not visible to the database user.");

        var names = rows.Where(row => row.ParameterName != null)
            .Select(row => row.ParameterName!.TrimStart('@')).ToArray();
        _functionParameters.Add(name.Quoted, names);
        return names;
    }

    private async Task<IReadOnlyList<T>> QueryBufferedAsync<T>(string sql, object? parameters,
        CommandType commandType, CancellationToken cancellationToken)
    {
        var command = Command(sql, parameters, commandType, cancellationToken);
        // Dapper buffers results and restores a previously closed connection. The context owns it.
        try
        {
            var rows = await context.Database.GetDbConnection().QueryAsync<T>(command);
            return rows.AsList();
        }
        catch (DbException exception) when (cancellationToken.IsCancellationRequested)
        {
            // SqlClient may report server-side cancellation as a provider exception.
            throw new OperationCanceledException("SQL execution was cancelled.", exception, cancellationToken);
        }
    }

    private async Task<int> ExecuteCommandAsync(string sql, object? parameters,
        CommandType commandType, CancellationToken cancellationToken)
    {
        var command = Command(sql, parameters, commandType, cancellationToken);
        try
        {
            return await context.Database.GetDbConnection().ExecuteAsync(command);
        }
        catch (DbException exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("SQL execution was cancelled.", exception, cancellationToken);
        }
    }

    private CommandDefinition Command(string sql, object? parameters, CommandType commandType,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        cancellationToken.ThrowIfCancellationRequested();
        // Never capture a transaction across retries; EF may have replaced/disposed it.
        return new CommandDefinition(sql, parameters,
            transaction: context.Database.CurrentTransaction?.GetDbTransaction(),
            commandTimeout: context.Database.GetCommandTimeout() ?? 30,
            commandType: commandType, cancellationToken: cancellationToken);
    }

    private static Dictionary<string, object?> ReadArguments(object? parameters)
    {
        var arguments = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (parameters == null)
            return arguments;

        if (parameters is IEnumerable<KeyValuePair<string, object?>> dictionary)
        {
            foreach (var pair in dictionary)
                Add(pair.Key, pair.Value);
        }
        else
        {
            if (parameters is SqlMapper.IDynamicParameters || parameters.GetType().IsValueType || parameters is string)
                throw new ArgumentException("TVF arguments must be a property object or a string/object dictionary.", nameof(parameters));
            foreach (var property in parameters.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(property => property.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0))
                Add(property.Name, property.GetValue(parameters));
        }
        return arguments;

        void Add(string key, object? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (!arguments.TryAdd(key.TrimStart('@'), value))
                throw new ArgumentException($"Duplicate TVF argument '{key}'.", nameof(parameters));
        }
    }

    private sealed class FunctionParameter
    {
        public string? ParameterName { get; set; }
    }
}
