using StackExchange.Redis;

namespace Core.Caching.Redis;

internal sealed class RedisConnectionProvider : IRedisConnectionProvider, IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Task<IConnectionMultiplexer>> _connections = new(StringComparer.Ordinal);
    private readonly Func<string, Task<IConnectionMultiplexer>> _connect;
    private bool _disposed;

    public RedisConnectionProvider() : this(ConnectAsync) { }

    internal RedisConnectionProvider(Func<string, Task<IConnectionMultiplexer>> connect) => _connect = connect;

    public async Task<IDatabase> GetDatabaseAsync(string configuration, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration);
        cancellationToken.ThrowIfCancellationRequested();
        Task<IConnectionMultiplexer> pending;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_connections.TryGetValue(configuration, out pending!) || pending.IsFaulted || pending.IsCanceled)
            {
                // Observe a failed connection attempt before replacing it on the next request.
                if (pending?.IsFaulted == true) _ = pending.Exception;
                pending = _connections[configuration] = _connect(configuration);
            }
        }

        // A caller cancelling its wait must not cancel the connection shared with other callers.
        var connection = await pending.WaitAsync(cancellationToken);
        return connection.GetDatabase();
    }

    private static async Task<IConnectionMultiplexer> ConnectAsync(string configuration) =>
        await ConnectionMultiplexer.ConnectAsync(configuration);

    public void Dispose()
    {
        Task<IConnectionMultiplexer>[] pending;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            pending = _connections.Values.ToArray();
            _connections.Clear();
        }

        foreach (var connection in pending)
            _ = connection.ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully) task.Result.Dispose();
                else _ = task.Exception;
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
