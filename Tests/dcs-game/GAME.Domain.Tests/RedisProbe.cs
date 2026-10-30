using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Features.Commands.GameConfig;
using GAME.Application.Features.Queries.Catalog;
using GAME.Application.Templates;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace GAME.Domain.Tests;

internal sealed class RedisProbe : IDistributedCache
{
    private readonly MemoryDistributedCache _inner = new(Options.Create(new MemoryDistributedCacheOptions()));
    public bool FailRevisionWrites, FailTableWrites, Offline;
    public System.Collections.Concurrent.ConcurrentDictionary<string, byte> Keys { get; } = new();
    public int RevisionWrites;
    private void Check(CancellationToken ct) { ct.ThrowIfCancellationRequested(); if (Offline) throw new IOException("Redis offline"); }
    public byte[]? Get(string key) { Check(default); return _inner.Get(key); }
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) { Check(token); return _inner.GetAsync(key, token); }
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        Check(default);
        if (key.EndsWith(":revision")) { if (FailRevisionWrites) throw new IOException("Publish failed"); Interlocked.Increment(ref RevisionWrites); }
        else if (FailTableWrites) throw new IOException("Table write failed");
        _inner.Set(key, value, options);
        Keys[key] = 0;
    }
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    { Check(token); Set(key, value, options); return Task.CompletedTask; }
    public void Refresh(string key) => _inner.Refresh(key);
    public Task RefreshAsync(string key, CancellationToken token = default) => _inner.RefreshAsync(key, token);
    public void Remove(string key) => _inner.Remove(key);
    public Task RemoveAsync(string key, CancellationToken token = default) => _inner.RemoveAsync(key, token);
}
