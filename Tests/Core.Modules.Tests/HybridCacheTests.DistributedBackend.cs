using Core.Caching.Hybrid;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Core.Modules.Tests;
public sealed partial class HybridCacheTests
{
    // HybridCache deliberately bypasses the exact MemoryDistributedCache type.
    // A distinct backend type lets this test exercise its real L2 serialization path.
    private sealed class DistributedBackend() : MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
