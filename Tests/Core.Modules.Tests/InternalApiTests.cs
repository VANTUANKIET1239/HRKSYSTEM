using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Core.InternalApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Core.Modules.Tests;

public sealed class InternalApiTests
{
    [Fact]
    public async Task ExplicitlyAccepted503ReturnsTypedBodyWithoutChangingNormalGetBehavior()
    {
        using var server = Server(async context =>
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsJsonAsync(new Reply(7));
        });
        var services = NewServices();
        services.AddInternalApi(Options());
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IInternalApiClient>();
        var response = await client.GetResponseAsync<Reply>("TEST", "/health/dependencies",
            [HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable]);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(7, response.Body.Value);
        var error = await Assert.ThrowsAsync<InternalApiException>(() => client.GetAsync<Reply>("TEST", "/health/dependencies"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, error.HttpStatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ValidBearerTokenCanCallProtectedApi_FromCallOptionsOrServiceConfig(bool perCall)
    {
        var signingKey = RandomNumberGenerator.GetBytes(32);
        using var server = ProtectedServer(signingKey);
        var authorization = BearerToken(signingKey);
        var options = Options();
        if (!perCall) options.Services["TEST"].Authorization = authorization;
        var services = NewServices();
        services.AddInternalApi(options);
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.IsType<ConfiguredInternalApiCredentialsProvider>(scope.ServiceProvider.GetRequiredService<IInternalApiCredentialsProvider>());
        var result = await scope.ServiceProvider.GetRequiredService<IInternalApiClient>().GetAsync<Reply>("test", "/private",
            perCall ? new() { Authorization = authorization } : null);
        Assert.Equal(42, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Bearer invalid-token")]
    public async Task ProtectedApiStillRejectsMissingOrInvalidCredentials(string? authorization)
    {
        using var server = ProtectedServer(RandomNumberGenerator.GetBytes(32));
        var services = NewServices();
        services.AddInternalApi(Options(retry: true));
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var error = await Assert.ThrowsAsync<InternalApiException>(() => scope.ServiceProvider.GetRequiredService<IInternalApiClient>()
            .GetAsync<Reply>("TEST", "/private", new() { Authorization = authorization }));
        Assert.Equal(HttpStatusCode.Unauthorized, error.HttpStatusCode);
    }

    [Fact]
    public async Task PerCallAuthorizationOverridesConfigAndDoesNotLeakToFollowingCalls()
    {
        var seen = new List<string>();
        using var server = Server(async context =>
        {
            seen.Add(context.Request.Headers.Authorization.ToString());
            await context.Response.WriteAsJsonAsync(new Reply(1));
        });
        var options = Options();
        options.Services["TEST"].Authorization = "Bearer configured-token";
        var services = NewServices();
        services.AddInternalApi(options);
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IInternalApiClient>();
        await client.GetAsync<Reply>("TEST", "/private", new() { Authorization = "Bearer call-token" });
        await client.GetAsync<Reply>("TEST", "/private");
        Assert.Equal(new[] { "Bearer call-token", "Bearer configured-token" }, seen);
    }

    [Fact]
    public async Task ServiceCredentialsRemainIsolatedByTargetService()
    {
        var seen = new List<string>();
        using var server = Server(async context =>
        {
            seen.Add(context.Request.Headers.Authorization.ToString());
            await context.Response.WriteAsJsonAsync(new Reply(1));
        });
        var options = Options();
        options.Services["TEST"].Authorization = "Bearer first-service-token";
        options.Services["OTHER"] = new() { BaseAddress = "https://other-service", Authorization = "Bearer second-service-token" };
        var services = NewServices();
        services.AddInternalApi(options);
        UseServer(services, server);
        services.AddHttpClient(InternalApiClientNames.Rest("OTHER")).ConfigurePrimaryHttpMessageHandler(server.CreateHandler);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IInternalApiClient>();
        await client.GetAsync<Reply>("TEST", "/private");
        await client.GetAsync<Reply>("OTHER", "/private");
        Assert.Equal(new[] { "Bearer first-service-token", "Bearer second-service-token" }, seen);
    }

    [Fact]
    public async Task ConfiguredAuthorizationBindsFromConfiguration()
    {
        using var server = Server(async context =>
        {
            Assert.Equal("Bearer configured-test-token", context.Request.Headers.Authorization.ToString());
            await context.Response.WriteAsJsonAsync(new Reply(1));
        });
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InternalApi:Services:TEST:BaseAddress"] = "https://localhost",
            ["InternalApi:Services:TEST:Authorization"] = "Bearer configured-test-token"
        }).Build();
        var services = NewServices();
        services.AddInternalApi(config);
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Equal(1, (await Call(scope.ServiceProvider)).Value);
    }

    [Theory]
    [InlineData("Bearer token\r\nInjected: bad")]
    [InlineData(" ")]
    [InlineData("\u0000")]
    public async Task InvalidAuthorizationIsRejectedBeforeSending(string authorization)
    {
        var services = NewServices();
        services.AddInternalApi(Options());
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<ArgumentException>(() => scope.ServiceProvider.GetRequiredService<IInternalApiClient>()
            .GetAsync<Reply>("TEST", "/private", new() { Authorization = authorization }));
    }

    [Fact]
    public async Task DirectUrls_CallNewEndpointsWithoutOperationRegistration()
    {
        var seen = new List<string>();
        using var server = Server(async context =>
        {
            seen.Add(context.Request.Path + context.Request.QueryString);
            await context.Response.WriteAsJsonAsync(new Reply(42));
        });
        var services = NewServices();
        var options = Options();
        options.Services["TEST"].BaseAddress = "https://localhost/internal/";
        services.AddInternalApi(options);
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IInternalApiClient>();
        foreach (var api in new[] { "players/42?name=a%20b", "/health", "https://LOCALHOST:443/another-api?value=42" })
            Assert.Equal(42, (await client.GetAsync<Reply>("test", api)).Value);
        Assert.Equal(new[] { "/internal/players/42?name=a%20b", "/health", "/another-api?value=42" }, seen);
    }

    [Fact]
    public async Task WriteRetry_PreservesBodyMetadataAndCredentials()
    {
        var seen = new List<(int Value, string Correlation, string Key, string Authorization, string Tenant)>();
        using var server = Server(async context =>
        {
            var payload = await context.Request.ReadFromJsonAsync<Request>();
            seen.Add((payload!.Value, context.Request.Headers["x-correlation-id"].ToString(),
                context.Request.Headers["idempotency-key"].ToString(), context.Request.Headers.Authorization.ToString(),
                context.Request.Headers["x-tenant"].ToString()));
            context.Response.StatusCode = seen.Count == 1 ? 503 : 200;
            await context.Response.WriteAsJsonAsync(new Reply(payload.Value));
        });
        var services = NewServices();
        services.AddScoped<IInternalApiCredentialsProvider, Credentials>();
        services.AddScoped<IInternalApiMetadataProvider, MetadataProvider>();
        services.AddInternalApi(Options());
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var reply = await scope.ServiceProvider.GetRequiredService<IInternalApiClient>().PostAsync<Request, Reply>(
            "test", "/rewards", new(42), new()
            {
                RetryEnabled = true, CorrelationId = "request-42", IdempotencyKey = "reward-42",
                Metadata = new Dictionary<string, string> { ["X-Tenant"] = "caller" }
            });
        Assert.Equal(42, reply.Value);
        Assert.Equal(2, seen.Count);
        Assert.All(seen, x => Assert.Equal((42, "request-42", "reward-42", "Bearer service-test", "caller"), x));
    }

    [Fact]
    public async Task HttpVerbs_SendBodiesAndSupportEmptyResponses()
    {
        var seen = new List<(string Method, string Path, int? Value)>();
        using var server = Server(async context =>
        {
            var value = context.Request.Method == "DELETE" ? (int?)null : (await context.Request.ReadFromJsonAsync<Request>())!.Value;
            seen.Add((context.Request.Method, context.Request.Path.ToString(), value));
            context.Response.StatusCode = 204;
        });
        var services = NewServices();
        services.AddInternalApi(Options());
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IInternalApiClient>();
        await client.PostAsync<Request, InternalApiEmpty>("TEST", "/create", new(1));
        await client.PutAsync<Request, InternalApiEmpty>("TEST", "/replace", new(2));
        await client.PatchAsync<Request, InternalApiEmpty>("TEST", "/update", new(3));
        await client.DeleteAsync<InternalApiEmpty>("TEST", "/delete");
        Assert.Equal(new (string, string, int?)[] { ("POST", "/create", 1), ("PUT", "/replace", 2), ("PATCH", "/update", 3), ("DELETE", "/delete", null) }, seen);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(501)]
    public async Task DoesNotRetryBusinessOrPermanentErrors(int status)
    {
        var calls = 0;
        using var server = Server(context => { calls++; context.Response.StatusCode = status; return Task.CompletedTask; });
        var services = NewServices();
        services.AddInternalApi(Options(retry: true));
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var error = await Assert.ThrowsAsync<InternalApiException>(() => Call(scope.ServiceProvider));
        Assert.Equal(InternalApiFailure.HttpError, error.Failure);
        Assert.Equal((HttpStatusCode)status, error.HttpStatusCode);
        Assert.Equal("/echo", error.Api);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task WriteDefaultsToSingleAttempt_EvenWithServiceRetryAndGlobalResilience(string method)
    {
        var calls = 0;
        using var server = Server(context => { calls++; context.Response.StatusCode = 503; return Task.CompletedTask; });
        var services = NewServices();
        services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
        services.AddInternalApi(Options(retry: true));
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<InternalApiException>(() => scope.ServiceProvider.GetRequiredService<IInternalApiClient>()
            .SendAsync<Reply>("TEST", new HttpMethod(method), "/write", new Request(1), new() { IdempotencyKey = "provided-but-no-retry-opt-in" }));
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(408)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public async Task ReadRetry_ExhaustsExactlyConfiguredAttempts(int status)
    {
        var calls = 0;
        using var server = Server(context => { calls++; context.Response.StatusCode = status; return Task.CompletedTask; });
        var services = NewServices();
        services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
        services.AddInternalApi(Options(retry: true));
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<InternalApiException>(() => Call(scope.ServiceProvider));
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task RetryCanBeSelectedPerCall_WithoutLeakingToNextRequest()
    {
        var calls = 0;
        using var server = Server(context => { calls++; context.Response.StatusCode = 503; return Task.CompletedTask; });
        var services = NewServices();
        services.AddInternalApi(Options());
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IInternalApiClient>();
        await Assert.ThrowsAsync<InternalApiException>(() => client.GetAsync<Reply>("TEST", "/retry", new() { RetryEnabled = true }));
        Assert.Equal(3, calls);
        await Assert.ThrowsAsync<InternalApiException>(() => client.GetAsync<Reply>("TEST", "/single"));
        Assert.Equal(4, calls);
    }

    [Fact]
    public async Task CallCanDisableServiceRetry()
    {
        var calls = 0;
        using var server = Server(context => { calls++; context.Response.StatusCode = 503; return Task.CompletedTask; });
        var services = NewServices();
        services.AddInternalApi(Options(retry: true));
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<InternalApiException>(() => scope.ServiceProvider.GetRequiredService<IInternalApiClient>()
            .GetAsync<Reply>("TEST", "/echo", new() { RetryEnabled = false }));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task NetworkExceptionIsRetriedUsingTheSameRequestPolicy()
    {
        var handler = new NetworkFailureHandler();
        var services = NewServices();
        services.AddInternalApi(Options(retry: true));
        services.AddHttpClient(InternalApiClientNames.Rest("TEST")).ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Equal(9, (await Call(scope.ServiceProvider)).Value);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task CallTimeoutIncludesRetryAfterDelay_AndOverridesServiceTimeout()
    {
        var calls = 0;
        using var server = Server(context =>
        {
            calls++;
            context.Response.StatusCode = 503;
            context.Response.Headers.RetryAfter = "60";
            return Task.CompletedTask;
        });
        var services = NewServices();
        services.AddInternalApi(Options(retry: true));
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var error = await Assert.ThrowsAsync<InternalApiException>(() => scope.ServiceProvider.GetRequiredService<IInternalApiClient>()
            .GetAsync<Reply>("TEST", "/echo", new() { TimeoutSeconds = 0.3 }));
        Assert.Equal(InternalApiFailure.Timeout, error.Failure);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CallerCancellationIsPreserved()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = Server(async context =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.Infinite, context.RequestAborted);
        });
        var services = NewServices();
        services.AddInternalApi(Options(retry: true));
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        using var cancellation = new CancellationTokenSource();
        var call = Call(scope.ServiceProvider, cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => call);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("<html>bad gateway</html>")]
    public async Task InvalidSuccessfulResponseIsNotRetried(string content)
    {
        var calls = 0;
        using var server = Server(async context => { calls++; await context.Response.WriteAsync(content); });
        var services = NewServices();
        services.AddInternalApi(Options(retry: true));
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var error = await Assert.ThrowsAsync<InternalApiException>(() => Call(scope.ServiceProvider));
        Assert.Equal(InternalApiFailure.InvalidResponse, error.Failure);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("https://other-service/steal")]
    [InlineData("https://localhost:444/steal")]
    [InlineData("http://localhost/steal")]
    [InlineData("https://user:password@localhost/steal")]
    [InlineData("//other-service/steal")]
    [InlineData("\\\\other-service\\steal")]
    [InlineData("ftp://localhost/steal")]
    [InlineData("/echo#fragment")]
    [InlineData("/echo\r\ninjected")]
    public async Task RejectsMismatchedOrInvalidUrls(string api)
    {
        var services = NewServices();
        services.AddInternalApi(Options());
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<ArgumentException>(() => scope.ServiceProvider.GetRequiredService<IInternalApiClient>().GetAsync<Reply>("TEST", api));
    }

    [Fact]
    public async Task CircuitIsSharedAcrossUrlsAndScopesOfTheSameService()
    {
        var calls = 0;
        using var server = Server(context => { calls++; context.Response.StatusCode = 503; return Task.CompletedTask; });
        var options = Options();
        options.Services["TEST"].CircuitBreaker.MinimumThroughput = 2;
        var services = NewServices();
        services.AddInternalApi(options);
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        for (var i = 0; i < 2; i++)
        {
            using var scope = provider.CreateScope();
            await Assert.ThrowsAsync<InternalApiException>(() => scope.ServiceProvider.GetRequiredService<IInternalApiClient>().GetAsync<Reply>("TEST", $"/api/{i}"));
        }
        using var finalScope = provider.CreateScope();
        var error = await Assert.ThrowsAsync<InternalApiException>(() => Call(finalScope.ServiceProvider));
        Assert.Equal(InternalApiFailure.CircuitOpen, error.Failure);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task EmptyConfiguration_AllowsInjectionAndReportsMissingService()
    {
        var services = new ServiceCollection();
        services.AddInternalApi(new InternalApiOptions());
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Call(scope.ServiceProvider));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task RetryingWriteRequiresIdempotencyKeyBeforeSending(string method)
    {
        var services = NewServices();
        services.AddInternalApi(Options(retry: true));
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<ArgumentException>(() => scope.ServiceProvider.GetRequiredService<IInternalApiClient>()
            .SendAsync<Reply>("TEST", new HttpMethod(method), "/echo", options: new() { RetryEnabled = true }));
    }

    [Theory]
    [InlineData("Authorization", "Bearer user-token")]
    [InlineData("X-Test", "value\r\nInjected: bad")]
    [InlineData("X-Test", "\u0000")]
    [InlineData("X-Forwarded-For", "1.2.3.4")]
    public async Task MetadataRejectsTransportHeadersAndInvalidValues(string name, string value)
    {
        var services = NewServices();
        services.AddInternalApi(Options());
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<ArgumentException>(() => scope.ServiceProvider.GetRequiredService<IInternalApiClient>()
            .GetAsync<Reply>("TEST", "/echo", new() { Metadata = new Dictionary<string, string> { [name] = value } }));
    }

    [Fact]
    public async Task ConfigBindsOnlyServiceAddress_WithoutAnyApiRegistration()
    {
        using var server = Server(context => context.Response.WriteAsJsonAsync(new Reply(5)));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InternalApi:Services:TEST:BaseAddress"] = "https://localhost",
            ["InternalApi:Services:TEST:TimeoutSeconds"] = "3"
        }).Build();
        var services = NewServices();
        services.AddInternalApi(config);
        UseServer(services, server);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Equal(5, (await Call(scope.ServiceProvider)).Value);
    }

    [Fact]
    public void InvalidServiceConfigFailsAtRegistration()
    {
        var options = Options();
        options.Services["TEST"].BaseAddress = "ftp://localhost";
        Assert.Throws<ArgumentException>(() => NewServices().AddInternalApi(options));
        options = Options();
        options.Services["TEST"].TimeoutSeconds = 0;
        Assert.Throws<ArgumentException>(() => NewServices().AddInternalApi(options));
        options = Options(retry: true);
        options.Services["TEST"].Retry.MaxRetries = 0;
        Assert.Throws<ArgumentException>(() => NewServices().AddInternalApi(options));
    }

    private static ServiceCollection NewServices() => (ServiceCollection)new ServiceCollection().AddLogging();

    private static InternalApiOptions Options(bool retry = false) => new()
    {
        Services = new()
        {
            ["TEST"] = new()
            {
                BaseAddress = "https://localhost", TimeoutSeconds = 5,
                Retry = new() { Enabled = retry, MaxRetries = 2, RetryDelayMilliseconds = 1 }
            }
        }
    };

    private static Task<Reply> Call(IServiceProvider provider, CancellationToken ct = default) =>
        provider.GetRequiredService<IInternalApiClient>().GetAsync<Reply>("TEST", "/echo", cancellationToken: ct);

    private static TestServer Server(RequestDelegate handler) => new(new WebHostBuilder().Configure(app => app.Run(handler)));

    private static TestServer ProtectedServer(byte[] signingKey) => new(new WebHostBuilder()
        .ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = "internal-api-test",
                    ValidateAudience = true, ValidAudience = "game-test",
                    ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(signingKey),
                    ValidateLifetime = true, ClockSkew = TimeSpan.Zero
                });
            services.AddAuthorization();
        })
        .Configure(app =>
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapGet("/private", context => context.Response.WriteAsJsonAsync(new Reply(42)))
                .RequireAuthorization());
        }));

    private static string BearerToken(byte[] signingKey) => "Bearer " + new JwtSecurityTokenHandler().WriteToken(
        new JwtSecurityToken("internal-api-test", "game-test", [new Claim("sub", "calling-service")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(signingKey), SecurityAlgorithms.HmacSha256)));

    private static void UseServer(IServiceCollection services, TestServer server) =>
        services.AddHttpClient(InternalApiClientNames.Rest("TEST")).ConfigurePrimaryHttpMessageHandler(server.CreateHandler);

    public sealed record Request(int Value);
    public sealed record Reply(int Value);

    private sealed class Credentials : IInternalApiCredentialsProvider
    {
        public ValueTask<string?> GetAuthorizationAsync(string serviceName, CancellationToken cancellationToken) => new("Bearer service-test");
    }

    private sealed class MetadataProvider : IInternalApiMetadataProvider
    {
        public ValueTask<IReadOnlyDictionary<string, string>> GetMetadataAsync(CancellationToken cancellationToken) =>
            new(new Dictionary<string, string> { ["x-tenant"] = "provider", ["x-correlation-id"] = "provider-id" });
    }

    private sealed class NetworkFailureHandler : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (++Calls == 1) throw new HttpRequestException("transient test error");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new Reply(9)) });
        }
    }
}
