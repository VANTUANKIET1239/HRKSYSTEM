using System.Reflection;
using System.Text.Json;
using Core.Caching.Redis;
using Core.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace Language.Tests;
public sealed partial class RedisTranslationCacheTests
{
    private sealed class Connection(IDatabase database) : IRedisConnectionProvider
    {
        public Task<IDatabase> GetDatabaseAsync(string configuration, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(database);
        }
    }
}
