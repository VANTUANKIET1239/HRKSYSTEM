using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.Caching.Hybrid;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Templates;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GAME.Infrastructure.Templates;

/// <summary>One HybridCache entry per table; a small revision marker coordinates nodes.</summary>
public sealed partial class GameTemplateStore(IServiceScopeFactory scopes, IDistributedCache redis, IHybridCacheService hybrid,
    TemplateTableRegistry registry, IOptions<GameTemplateCacheOptions> settings, IHostEnvironment environment,
    ILogger<GameTemplateStore> logger) : IGameTemplateRefresh
{
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly ConcurrentDictionary<Type, (string Revision, JsonElement Rows)> _lastValid = new();
    private string? _lastRevision;
    private string? _prefix;
    private string Prefix => _prefix ??= $"templates:{environment.EnvironmentName}:tables-v1:d{settings.Value.DataVersion}:" +
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', registry.Types.Select(t => t.FullName).Order()))))[..12];
    private string RevisionKey => $"{Prefix}:revision";
    private string TableKey(Type type, string revision) => $"{Prefix}:{revision}:{registry.Name(type)}";
    private HybridCacheEntryOptions EntryOptions(HybridCacheEntryFlags? flags = null) => new()
    {
        Expiration = TimeSpan.FromMinutes(settings.Value.DistributedExpirationMinutes),
        LocalCacheExpiration = TimeSpan.FromMinutes(settings.Value.LocalExpirationMinutes),
        Flags = flags
    };

    internal async Task<string> GetRevisionAsync(CancellationToken ct)
    {
        if (!settings.Value.Enabled) return "uncached";
        try
        {
            var revision = await redis.GetStringAsync(RevisionKey, ct) ?? "initial";
            Volatile.Write(ref _lastRevision, revision);
            return revision;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cannot read template revision; using the last local revision.");
            var revision = Volatile.Read(ref _lastRevision) ?? "initial";
            Interlocked.CompareExchange(ref _lastRevision, revision, null);
            return revision;
        }
    }

    internal async Task<JsonElement> GetTableAsync(Type type, string revision, CancellationToken ct, bool allowLocalFallback = true)
    {
        registry.Require(type);
        if (!settings.Value.Enabled) return await LoadAsync(type, ct);
        try
        {
            var rows = await hybrid.GetOrCreateAsync(TableKey(type, revision), async token =>
            {
                // Never reconstruct a published revision from subsequently edited DB rows.
                if (revision != "initial") throw new TemplateTableExpiredException(type.Name);
                if (await redis.GetStringAsync(RevisionKey, token) is not null)
                    throw new TemplateTableExpiredException(type.Name);
                return await LoadAsync(type, token);
            }, ct, EntryOptions());
            if (revision == Volatile.Read(ref _lastRevision)) _lastValid[type] = (revision, rows);
            return rows;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            if (_lastValid.TryGetValue(type, out var last) && last.Revision == revision) return last.Rows;
            if (!allowLocalFallback || ex is TemplateTableExpiredException) throw;
            logger.LogWarning(ex, "Template table {Table} cache read failed.", type.Name);
            if (revision != "initial" || revision != Volatile.Read(ref _lastRevision)) throw;
            await _refreshGate.WaitAsync(ct);
            try
            {
                if (_lastValid.TryGetValue(type, out last) && last.Revision == revision) return last.Rows;
                var rows = await LoadAsync(type, ct);
                _lastValid[type] = (revision, rows);
                return rows;
            }
            finally { _refreshGate.Release(); }
        }
    }

    private async Task<JsonElement> LoadAsync(Type type, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var rows = await registry.LoadAsync(type, scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), ct);
        CheckSize(type, rows);
        return rows;
    }

    public Task<TemplateRefreshResult> RefreshAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        registry.Require(typeof(T));
        return RefreshCoreAsync(registry.Relations(typeof(T)).ToHashSet(), null, cancellationToken);
    }

    public Task<TemplateRefreshResult> RefreshAsync(CancellationToken cancellationToken = default) =>
        RefreshCoreAsync(null, null, cancellationToken);

    internal Task<TemplateRefreshResult> RenewAsync(string expiredRevision, CancellationToken ct) =>
        RefreshCoreAsync(null, expiredRevision, ct);

    private async Task<TemplateRefreshResult> RefreshCoreAsync(HashSet<Type>? reload, string? expiredRevision, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(settings.Value.OperationTimeoutSeconds));
        if (expiredRevision != null) await _refreshGate.WaitAsync(deadline.Token);
        else if (!await _refreshGate.WaitAsync(0, deadline.Token))
            throw new BusinessException("TEMPLATE_REFRESH_BUSY", "Đang có một lượt đồng bộ template. Vui lòng thử lại sau.");
        try
        {
            if (!settings.Value.Enabled) throw new BusinessException("TEMPLATE_CACHE_DISABLED", "Cache template đang tắt.");
            var ct = deadline.Token;
            var previousRevision = await GetRevisionAsync(ct);
            if (expiredRevision != null && previousRevision != expiredRevision)
                return new(previousRevision, DateTimeOffset.UtcNow, registry.Types.Count, 0);
            var revision = Guid.NewGuid().ToString("N");
            var loadedAt = DateTimeOffset.UtcNow;
            var tables = new Dictionary<Type, JsonElement>();
            using (var scope = scopes.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                foreach (var type in registry.Types)
                {
                    var rows = reload == null || reload.Contains(type)
                        ? await registry.LoadAsync(type, unitOfWork, ct)
                        : await GetTableAsync(type, previousRevision, ct, allowLocalFallback: false);
                    CheckSize(type, rows);
                    tables.Add(type, rows);
                }
            }
            registry.Link(tables.ToDictionary(p => p.Key, p => registry.Materialize(p.Key, p.Value)));
            foreach (var (type, rows) in tables)
                await hybrid.SetAsync(TableKey(type, revision), rows, ct, EntryOptions());
            // HybridCache tolerates backend failures; verify L2 before reporting publication success.
            foreach (var type in tables.Keys)
                _ = await hybrid.GetOrCreateAsync<JsonElement>(TableKey(type, revision),
                    _ => throw new IOException($"Template {type.Name} was not persisted to Redis."), ct,
                    EntryOptions(HybridCacheEntryFlags.DisableLocalCacheRead | HybridCacheEntryFlags.DisableDistributedCacheWrite));
            await redis.SetStringAsync(RevisionKey, revision, ct);
            foreach (var (type, rows) in tables) _lastValid[type] = (revision, rows);
            Volatile.Write(ref _lastRevision, revision);
            logger.LogInformation("Refreshed {Tables} template tables, revision {Revision}.", tables.Count, revision);
            return new(revision, loadedAt, tables.Count, tables.Values.Sum(t => t.GetArrayLength()));
        }
        finally { _refreshGate.Release(); }
    }

    internal async Task WarmupAsync(CancellationToken ct)
    {
        if (!settings.Value.Enabled) return;
        var revision = await GetRevisionAsync(ct);
        try
        {
            foreach (var type in registry.Types) _ = await GetTableAsync(type, revision, ct);
        }
        catch (TemplateTableExpiredException) { await RenewAsync(revision, ct); }
    }

    private void CheckSize(Type type, JsonElement rows)
    {
        if (Encoding.UTF8.GetByteCount(rows.GetRawText()) > settings.Value.MaximumTableBytes)
            throw new InvalidOperationException($"Template table {type.Name} exceeds MaximumTableBytes.");
    }
}
