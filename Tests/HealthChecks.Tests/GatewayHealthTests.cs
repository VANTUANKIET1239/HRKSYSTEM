using System.Net;
using System.Net.Http.Json;
using ApiGateway.Configuration;
using Core.HealthChecks;
using Core.InternalApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.Middleware;
using Xunit;

namespace HealthChecks.Tests;

public sealed partial class GatewayHealthTests
{
    [Fact]
    public async Task ArbitraryTargetUsesConfiguredAddressPathNameAndIdentityWithoutOcelotRoutes()
    {
        Uri? seen = null;
        await using var app = await ConfiguredGatewayAsync(new(), (request, ct) =>
        {
            seen = request.RequestUri;
            return Task.FromResult(HealthyWorkerResponse());
        });
        using var client = app.GetTestClient();
        var response = await client.GetAsync("/health/dependencies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = (await response.Content.ReadFromJsonAsync<ServiceHealthReport>())!;
        Assert.Equal("service.worker", Assert.Single(report.Checks).Name);
        Assert.Equal("http://probe-worker:4321/status/alive", seen!.ToString());
    }

    [Fact]
    public async Task ConfiguredExpectedIdentityIsValidated()
    {
        await using var app = await ConfiguredGatewayAsync(new()
        {
            ["GatewayHealthChecks:Dependencies:service.worker:ExpectedService"] = "SOMEONE.ELSE"
        }, (request, ct) => Task.FromResult(HealthyWorkerResponse()));
        using var client = app.GetTestClient();
        var response = await client.GetAsync("/health/dependencies");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task DisabledConfiguredTargetMakesNoHttpCall()
    {
        var calls = 0;
        await using var app = await ConfiguredGatewayAsync(new()
        {
            ["HealthChecks:Dependencies:service.worker:Enabled"] = "false"
        }, (request, ct) =>
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(HealthyWorkerResponse());
        });
        using var client = app.GetTestClient();
        var report = (await client.GetFromJsonAsync<ServiceHealthReport>("/health/dependencies"))!;
        Assert.Empty(report.Checks);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task HttpTimeoutComesFromInternalApiConfiguration()
    {
        var calls = 0;
        await using var app = await ConfiguredGatewayAsync(new()
        {
            ["InternalApi:Services:custom-worker:TimeoutSeconds"] = "0.05"
        }, async (request, ct) =>
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(Timeout.Infinite, ct);
            return HealthyWorkerResponse();
        });
        using var client = app.GetTestClient();
        var response = await client.GetAsync("/health/dependencies");
        var report = (await response.Content.ReadFromJsonAsync<ServiceHealthReport>())!;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("DownstreamUnavailable", Assert.Single(report.Checks).ErrorCode);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ConfiguredHttpRetryPolicyIsNotOverwrittenByGatewayRegistration()
    {
        var calls = 0;
        await using var app = await ConfiguredGatewayAsync(new()
        {
            ["InternalApi:Services:custom-worker:Retry:Enabled"] = "true",
            ["InternalApi:Services:custom-worker:Retry:MaxRetries"] = "1",
            ["InternalApi:Services:custom-worker:Retry:RetryDelayMilliseconds"] = "1"
        }, (request, ct) => Task.FromResult(Interlocked.Increment(ref calls) == 1
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : HealthyWorkerResponse()));
        using var client = app.GetTestClient();
        var response = await client.GetAsync("/health/dependencies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData("ServiceName", "missing-profile")]
    [InlineData("ExpectedService", "")]
    [InlineData("Path", "https://somewhere/health/live")]
    [InlineData("Path", "//somewhere/health/live")]
    public void InvalidTargetConfigurationFailsAtStartup(string property, string value)
    {
        var builder = ConfiguredBuilder(new()
        {
            [$"GatewayHealthChecks:Dependencies:service.worker:{property}"] = value
        });
        Assert.Throws<ArgumentException>(() => builder.AddGatewayHealthChecks());
    }

    [Fact]
    public async Task LocalHealthRoutesRunBeforeOcelotAndOnlyProbeDownstreamLiveness()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HRK.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = Path.Combine(directory!.FullName, "ApiGateway", "ApiGateway"),
            EnvironmentName = "Testing"
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "gateway-health-test-signing-key-at-least-32-bytes"
        });
        DependencyInjection.AddServices(builder);
        var requests = new List<string>();
        foreach (var target in new[] { "auth", "game", "language", "admin" })
            builder.Services.AddHttpClient(InternalApiClientNames.Rest("health-" + target))
                .ConfigurePrimaryHttpMessageHandler(() => new UnreachableHandler(requests));
        await using var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapHrkHealthChecks());
        await app.UseOcelot();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var live = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Empty(requests);
        var dependencies = await client.GetAsync("/health/dependencies");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, dependencies.StatusCode);
        var report = (await dependencies.Content.ReadFromJsonAsync<ServiceHealthReport>())!;
        Assert.True(report.IsValid("ApiGateway", HealthKinds.Dependency));
        Assert.Equal(4, report.Checks.Count);
        Assert.Equal(4, requests.Count);
        Assert.All(requests, path => Assert.Equal("/health/live", path));
    }

    private static WebApplicationBuilder ConfiguredBuilder(Dictionary<string, string?> overrides)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        var values = new Dictionary<string, string?>
        {
            ["GatewayHealthChecks:Dependencies:service.worker:ServiceName"] = "custom-worker",
            ["GatewayHealthChecks:Dependencies:service.worker:ExpectedService"] = "CUSTOM.WORKER",
            ["GatewayHealthChecks:Dependencies:service.worker:Path"] = "/status/alive",
            ["InternalApi:Services:custom-worker:BaseAddress"] = "http://probe-worker:4321",
            ["InternalApi:Services:custom-worker:TimeoutSeconds"] = "1",
            ["InternalApi:Services:custom-worker:Retry:Enabled"] = "false",
            ["InternalApi:Services:custom-worker:Retry:MaxRetries"] = "0",
            ["InternalApi:Services:custom-worker:CircuitBreaker:Enabled"] = "false"
        };
        foreach (var (key, value) in overrides) values[key] = value;
        builder.Configuration.AddInMemoryCollection(values);
        return builder;
    }

    private static async Task<WebApplication> ConfiguredGatewayAsync(Dictionary<string, string?> overrides,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var builder = ConfiguredBuilder(overrides);
        builder.AddGatewayHealthChecks();
        builder.Services.AddHttpClient(InternalApiClientNames.Rest("custom-worker"))
            .ConfigurePrimaryHttpMessageHandler(() => new DelegateHandler(handler));
        var app = builder.Build();
        app.MapHrkHealthChecks();
        await app.StartAsync();
        return app;
    }

    private static HttpResponseMessage HealthyWorkerResponse() => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new ServiceHealthReport
        {
            Service = "CUSTOM.WORKER", InstanceId = "worker-1", Kind = HealthKinds.Liveness,
            Status = HealthStatuses.Healthy, CheckedAtUtc = DateTimeOffset.UtcNow,
            Checks = [new() { Name = "self", Status = HealthStatuses.Healthy }]
        })
    };
}
