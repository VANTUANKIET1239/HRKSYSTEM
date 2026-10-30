using System.Net;
using System.Net.Http.Json;
using Core.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace HealthChecks.Tests;
public sealed partial class HealthEndpointTests
{
    public sealed class ScopedResource : IDisposable
    {
        public static int Created, Disposed;
        public ScopedResource() => Interlocked.Increment(ref Created);
        public void Dispose() => Interlocked.Increment(ref Disposed);
    }
}
