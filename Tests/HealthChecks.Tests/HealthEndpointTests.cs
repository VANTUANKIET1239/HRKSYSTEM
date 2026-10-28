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

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task LivenessNeverActivatesDependencies()
    {
        ThrowingCheck.Calls = 0;
        using var server = Server(checks => checks.AddDependency<ThrowingCheck>("database"));
        var response = await server.CreateClient().GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = (await response.Content.ReadFromJsonAsync<ServiceHealthReport>())!;
        Assert.True(report.IsValid("TEST", HealthKinds.Liveness));
        Assert.Equal("self", Assert.Single(report.Checks).Name);
        Assert.Equal(0, ThrowingCheck.Calls);
    }

    [Theory]
    [InlineData("/health/dependencies")]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task DependenciesAndAliasesReturnSafe503Json(string path)
    {
        using var server = Server(checks => checks.AddDependency<ThrowingCheck>("database"));
        var response = await server.CreateClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("no-store, no-cache", response.Headers.CacheControl!.ToString());
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("sensitive-password", raw);
        var report = (await response.Content.ReadFromJsonAsync<ServiceHealthReport>())!;
        Assert.True(report.IsValid("TEST", HealthKinds.Dependency));
        Assert.Equal(HealthStatuses.Unhealthy, report.Status);
        Assert.Equal("DatabaseUnavailable", Assert.Single(report.Checks).ErrorCode);
    }

    [Fact]
    public async Task DisabledCheckIsNotExecuted()
    {
        var configuration = Config(new() { ["HealthChecks:Dependencies:database:Enabled"] = "false" });
        using var server = Server(checks => checks.AddDependency<ThrowingCheck>("database"), configuration);
        var response = await server.CreateClient().GetAsync("/health/dependencies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<ServiceHealthReport>())!.Checks);
    }

    [Fact]
    public async Task SlowDependencyTimesOutAndLivenessRemainsAvailable()
    {
        using var server = Server(checks => checks.AddDependency<SlowCheck>("slow"),
            Config(new() { ["HealthChecks:Dependencies:slow:TimeoutSeconds"] = "0.05" }));
        var response = await server.CreateClient().GetAsync("/health/dependencies");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var report = (await response.Content.ReadFromJsonAsync<ServiceHealthReport>())!;
        Assert.Equal("Timeout", Assert.Single(report.Checks).ErrorCode);
        Assert.Equal(HttpStatusCode.OK, (await server.CreateClient().GetAsync("/health/live")).StatusCode);
    }

    [Fact]
    public async Task EachConcurrentCheckGetsItsOwnDisposableScope()
    {
        ScopedResource.Created = ScopedResource.Disposed = 0;
        using var server = Server(checks => checks.AddDependency<ScopedCheck>("first").AddDependency<ScopedCheck>("second"),
            extra: services => services.AddScoped<ScopedResource>());
        var response = await server.CreateClient().GetAsync("/health/dependencies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, ScopedResource.Created);
        Assert.Equal(2, ScopedResource.Disposed);
    }

    [Fact]
    public async Task FactoryRegistrationsUseIndependentScopesAndHonorDisabledChecks()
    {
        ScopedResource.Created = ScopedResource.Disposed = 0;
        using var server = Server(checks =>
        {
            foreach (var name in new[] { "first", "second", "disabled" })
                checks.AddDependency(name, provider => new ScopedCheck(provider.GetRequiredService<ScopedResource>()));
        }, Config(new() { ["HealthChecks:Dependencies:disabled:Enabled"] = "false" }),
            services => services.AddScoped<ScopedResource>());
        var response = await server.CreateClient().GetAsync("/health/dependencies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, (await response.Content.ReadFromJsonAsync<ServiceHealthReport>())!.Checks.Count);
        Assert.Equal(2, ScopedResource.Created);
        Assert.Equal(2, ScopedResource.Disposed);
    }

    [Fact]
    public void DuplicateNamesAndInvalidTimeoutFailAtRegistration()
    {
        var services = new ServiceCollection();
        var checks = services.AddHrkHealthChecks("TEST");
        checks.AddDependency<SlowCheck>("db");
        Assert.Throws<ArgumentException>(() => checks.AddDependency<SlowCheck>("DB"));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddHrkHealthChecks("TEST",
            Config(new() { ["HealthChecks:DefaultTimeoutSeconds"] = "0" })));
        Assert.Throws<InvalidOperationException>(() => services.AddHrkHealthChecks("SECOND"));
    }

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static TestServer Server(Action<HrkHealthChecksBuilder> configure, IConfiguration? configuration = null,
        Action<IServiceCollection>? extra = null) => new(new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddRouting();
            extra?.Invoke(services);
            configure(services.AddHrkHealthChecks("TEST", configuration));
        }).Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapHrkHealthChecks());
        }));

    public sealed class ThrowingCheck() : DependencyHealthCheck("DatabaseUnavailable")
    {
        public static int Calls;
        protected override Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            throw new InvalidOperationException("sensitive-password");
        }
    }

    public sealed class SlowCheck() : DependencyHealthCheck("Slow")
    {
        protected override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return true;
        }
    }

    public sealed class ScopedResource : IDisposable
    {
        public static int Created, Disposed;
        public ScopedResource() => Interlocked.Increment(ref Created);
        public void Dispose() => Interlocked.Increment(ref Disposed);
    }

    public sealed class ScopedCheck(ScopedResource resource) : DependencyHealthCheck("Scoped")
    {
        protected override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            GC.KeepAlive(resource);
            return true;
        }
    }
}
