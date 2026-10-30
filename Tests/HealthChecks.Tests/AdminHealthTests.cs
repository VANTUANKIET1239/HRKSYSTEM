using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Core.HealthChecks;
using Core.InternalApi;
using Admin.Application.DTOs.HealthMonitoring;
using Admin.Infrastructure.DependencyInjection;
using Admin.Application.Features.Queries.HealthMonitoring;
using MediatR;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Web.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace HealthChecks.Tests;

public sealed partial class AdminHealthTests
{
    private const string SigningKey = "health-test-signing-key-at-least-32-bytes-long";

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Player", HttpStatusCode.Forbidden)]
    [InlineData("Admin", HttpStatusCode.OK)]
    public async Task AdminApiRequiresAuthenticatedAdmin(string? role, HttpStatusCode expected)
    {
        using var server = Server(Config("game"), HealthyResponse);
        using var client = server.CreateClient();
        if (role is not null) client.DefaultRequestHeaders.Authorization = Token(role);
        var response = await client.GetAsync("/api/admin/health/services");
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
        {
            Assert.True(response.Headers.CacheControl!.NoStore);
            Assert.Equal(HealthStatuses.Healthy, (await ReadReportAsync(response))!.Status);
        }
        else
        {
            var error = await response.Content.ReadFromJsonAsync<BaseResponse<object>>();
            Assert.NotNull(error);
            Assert.False(error.Success);
            Assert.Equal((int)expected, error.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(error.TraceId));
        }
    }

    [Fact]
    public async Task ControllerDispatchesThroughTheCqrsPipeline()
    {
        var behavior = new QueryTrackingBehavior();
        using var server = Server(Config("game"), HealthyResponse, services =>
            services.AddSingleton<IPipelineBehavior<GetServicesHealthQuery, BaseResponse<SystemHealthReport>>>(behavior));
        using var client = Authorized(server);
        var report = await GetReportAsync(client);
        Assert.Equal(HealthStatuses.Healthy, report.Status);
        Assert.Equal(1, behavior.Calls);
    }

    [Theory]
    [InlineData("/health/live", HealthKinds.Liveness)]
    [InlineData("/health/dependencies", HealthKinds.Dependency)]
    public async Task NativeAdminHealthEndpointsKeepTheirRawContractAndDoNotRunAggregation(string path, string kind)
    {
        var calls = 0;
        using var server = Server(Config("game"), (request, ct) =>
        {
            Interlocked.Increment(ref calls);
            return HealthyResponse(request, ct);
        });
        using var client = server.CreateClient();
        var report = (await client.GetFromJsonAsync<ServiceHealthReport>(path))!;
        Assert.True(report.IsValid("HRK.ADMIN", kind));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task DeadlineDuringDependencyProbeKeepsCompletedLivenessAndOtherService()
    {
        var config = Config("fast", "slow");
        config["HealthMonitoring:RequestTimeoutSeconds"] = "0.3";
        using var server = Server(config, async (request, ct) =>
        {
            if (request.RequestUri!.Host == "slow" && request.RequestUri.AbsolutePath.EndsWith("dependencies"))
                await Task.Delay(Timeout.Infinite, ct);
            return await HealthyResponse(request, ct);
        });
        using var client = Authorized(server);
        var result = await GetReportAsync(client);
        Assert.Equal(HealthStatuses.Healthy, result.Services[0].Status);
        var slow = result.Services[1];
        Assert.Equal(HealthStatuses.Healthy, slow.Liveness.Status);
        Assert.NotNull(slow.Liveness.Report);
        Assert.Equal("Timeout", slow.Dependencies!.ErrorCode);
        Assert.Equal(HealthStatuses.Unhealthy, slow.Status);
    }

    [Fact]
    public async Task IndividualProbeTimeoutReturnsReportInsteadOfFailingTheAdminApi()
    {
        var config = Config("slow");
        config["HealthMonitoring:ProbeTimeoutSeconds"] = "0.05";
        using var server = Server(config, async (request, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return await HealthyResponse(request, ct);
        });
        using var client = Authorized(server);
        var result = await GetReportAsync(client);
        Assert.Equal("Timeout", Assert.Single(result.Services).Liveness.ErrorCode);
        Assert.Null(result.Services[0].Dependencies);
    }

    [Fact]
    public async Task Returns503ComponentBodyAndKeepsOtherServicesWhenOneIsUnreachable()
    {
        var calls = new List<string>();
        using var server = Server(Config("game", "language", "down"), (request, ct) =>
        {
            lock (calls) calls.Add(request.RequestUri!.ToString());
            if (request.RequestUri!.Host == "down") throw new HttpRequestException("not reachable");
            if (request.RequestUri.Host == "game" && request.RequestUri.AbsolutePath.EndsWith("dependencies"))
                return Task.FromResult(Json(HttpStatusCode.ServiceUnavailable,
                    Report("GAME", HealthKinds.Dependency, false)));
            return HealthyResponse(request, ct);
        });
        using var client = Authorized(server);
        var response = await client.GetAsync("/api/admin/health/services");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await ReadReportAsync(response))!;
        Assert.Equal(HealthStatuses.Unhealthy, result.Status);
        var game = Assert.Single(result.Services, service => service.Service == "GAME");
        Assert.Equal(HealthStatuses.Healthy, game.Liveness.Status);
        Assert.Equal(503, game.Dependencies!.HttpStatusCode);
        Assert.Equal("database", Assert.Single(game.Dependencies.Report!.Checks).Name);
        Assert.Equal(HealthStatuses.Unhealthy, game.Dependencies.Report.Checks[0].Status);
        Assert.Equal(HealthStatuses.Healthy, Assert.Single(result.Services, service => service.Service == "LANGUAGE").Status);
        var down = Assert.Single(result.Services, service => service.Service == "DOWN");
        Assert.Equal("Unavailable", down.Liveness.ErrorCode);
        Assert.Null(down.Dependencies);
        // No retry and no hidden requests, including the 503 and unavailable outcomes.
        Assert.Equal(5, calls.Count);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("identity")]
    [InlineData("status")]
    [InlineData("version")]
    [InlineData("empty")]
    public async Task RejectsMalformedOrInconsistentReports(string mode)
    {
        using var server = Server(Config("game"), (request, ct) =>
        {
            if (mode == "json")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{bad json") });
            var report = Report("GAME", HealthKinds.Liveness, true);
            report = mode switch
            {
                "identity" => report with { Service = "SOMEONE-ELSE" },
                "status" => report with { Status = HealthStatuses.Unhealthy },
                "version" => report with { ContractVersion = 99 },
                "empty" => report with { Checks = [] },
                _ => report
            };
            return Task.FromResult(Json(HttpStatusCode.OK, report));
        });
        using var client = Authorized(server);
        var reportResult = (await GetReportAsync(client))!;
        var service = Assert.Single(reportResult.Services);
        Assert.Equal(HealthStatuses.Unhealthy, service.Status);
        Assert.Equal("InvalidResponse", service.Liveness.ErrorCode);
        Assert.Null(service.Dependencies);
    }

    [Fact]
    public async Task HttpStatusMustMatchReportedStatus()
    {
        using var server = Server(Config("game"), (request, ct) =>
            Task.FromResult(Json(HttpStatusCode.ServiceUnavailable, Report("GAME", HealthKinds.Liveness, true))));
        using var client = Authorized(server);
        var result = (await GetReportAsync(client))!;
        Assert.Equal("InvalidResponse", Assert.Single(result.Services).Liveness.ErrorCode);
    }

    [Fact]
    public async Task RepeatedUnhealthyReportsAreFreshAndDoNotOpenCircuit()
    {
        var calls = 0;
        using var server = Server(Config("game"), (request, ct) =>
        {
            Interlocked.Increment(ref calls);
            var kind = request.RequestUri!.AbsolutePath.EndsWith("dependencies") ? HealthKinds.Dependency : HealthKinds.Liveness;
            return Task.FromResult(Json(kind == HealthKinds.Dependency ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK,
                Report("GAME", kind, kind == HealthKinds.Liveness)));
        });
        using var client = Authorized(server);
        for (var i = 0; i < 12; i++)
        {
            var result = (await GetReportAsync(client))!;
            Assert.Equal(503, Assert.Single(result.Services).Dependencies!.HttpStatusCode);
        }
        Assert.Equal(24, calls);
    }

    [Fact]
    public async Task GlobalDeadlineRetainsCompletedResultsAndBoundsConcurrency()
    {
        var config = Config("fast", "slow1", "slow2", "slow3");
        config["HealthMonitoring:RequestTimeoutSeconds"] = "0.4";
        config["HealthMonitoring:MaxConcurrency"] = "2";
        var active = 0;
        var peak = 0;
        using var server = Server(config, async (request, ct) =>
        {
            var count = Interlocked.Increment(ref active);
            int previous;
            do { previous = Volatile.Read(ref peak); } while (count > previous && Interlocked.CompareExchange(ref peak, count, previous) != previous);
            try
            {
                if (request.RequestUri!.Host != "fast") await Task.Delay(Timeout.Infinite, ct);
                return await HealthyResponse(request, ct);
            }
            finally { Interlocked.Decrement(ref active); }
        });
        using var client = Authorized(server);
        var result = (await GetReportAsync(client))!;
        Assert.Equal(4, result.Services.Count);
        Assert.Equal(HealthStatuses.Healthy, result.Services[0].Status);
        Assert.All(result.Services.Skip(1), service => Assert.Equal("Timeout", service.Liveness.ErrorCode));
        Assert.InRange(peak, 1, 2);
    }

    [Fact]
    public async Task CallerCancellationStopsProbes()
    {
        using var server = Server(Config("game"), async (request, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return await HealthyResponse(request, ct);
        });
        using var client = Authorized(server);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync("/api/admin/health/services", cancellation.Token));
    }

    [Fact]
    public void MonitoringRejectsRetryOrCircuitProfiles()
    {
        var config = Config("game");
        config["InternalApi:Services:game:CircuitBreaker:Enabled"] = "true";
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddAdminInfrastructure(config));
        config["InternalApi:Services:game:CircuitBreaker:Enabled"] = "false";
        config["InternalApi:Services:game:Retry:MaxRetries"] = "2";
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddAdminInfrastructure(config));
    }

    private static HttpClient Authorized(TestServer server)
    {
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = Token("Admin");
        return client;
    }

    private static async Task<SystemHealthReport> GetReportAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/admin/health/services");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadReportAsync(response);
    }

    private static async Task<SystemHealthReport> ReadReportAsync(HttpResponseMessage response)
    {
        var envelope = await response.Content.ReadFromJsonAsync<BaseResponse<SystemHealthReport>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(200, envelope.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(envelope.TraceId));
        return Assert.IsType<SystemHealthReport>(envelope.Data);
    }

    private static AuthenticationHeaderValue Token(string role) => new("Bearer",
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("hrkapp", "game-api",
            [new Claim("sub", "health-admin-test"), new Claim(ClaimTypes.Role, role)],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256))));

    private static IConfigurationRoot Config(params string[] names)
    {
        var values = new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "hrkapp", ["JwtSettings:Audience"] = "game-api", ["JwtSettings:SecretKey"] = SigningKey
        };
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            values[$"HealthMonitoring:Services:{i}:ServiceName"] = name;
            values[$"HealthMonitoring:Services:{i}:ExpectedService"] = name.ToUpperInvariant();
            values[$"InternalApi:Services:{name}:BaseAddress"] = $"http://{name}";
            values[$"InternalApi:Services:{name}:Retry:MaxRetries"] = "0";
            values[$"InternalApi:Services:{name}:CircuitBreaker:Enabled"] = "false";
        }
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static TestServer Server(IConfiguration configuration,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler,
        Action<IServiceCollection>? configureServices = null) => new(new WebHostBuilder()
        .ConfigureServices(services =>
        {
            HRK.ADMIN.Configuration.DependencyInjection.AddServices(services, configuration);
            configureServices?.Invoke(services);
            var targets = configuration.GetSection("InternalApi:Services").GetChildren();
            foreach (var target in targets)
                services.AddHttpClient(InternalApiClientNames.Rest(target.Key))
                    .ConfigurePrimaryHttpMessageHandler(() => new DelegateHandler(handler));
        }).Configure(app =>
        {
            app.UseRouting();
            app.UseHrkApiResponses();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); endpoints.MapHrkHealthChecks(); });
        }));

    private static Task<HttpResponseMessage> HealthyResponse(HttpRequestMessage request, CancellationToken ct) =>
        Task.FromResult(Json(HttpStatusCode.OK, Report(request.RequestUri!.Host.ToUpperInvariant(),
            request.RequestUri.AbsolutePath.EndsWith("dependencies") ? HealthKinds.Dependency : HealthKinds.Liveness, true)));

    private static HttpResponseMessage Json(HttpStatusCode status, ServiceHealthReport report) =>
        new(status) { Content = JsonContent.Create(report) };

    private static ServiceHealthReport Report(string service, string kind, bool healthy) => new()
    {
        Service = service, InstanceId = "test-instance", Kind = kind, CheckedAtUtc = DateTimeOffset.UtcNow,
        Status = healthy ? HealthStatuses.Healthy : HealthStatuses.Unhealthy,
        Checks = [new() { Name = kind == HealthKinds.Liveness ? "self" : "database",
            Status = healthy ? HealthStatuses.Healthy : HealthStatuses.Unhealthy }]
    };
}
