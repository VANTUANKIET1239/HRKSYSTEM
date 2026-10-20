using Core.InternalApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oservability;
using Oservability.Correlation;
using Xunit;

namespace Oservability.Tests;

public sealed class InternalApiContextTests
{
    [Fact]
    public async Task HrkClient_PropagatesScopedCorrelationWithoutLeakingBetweenCalls()
    {
        var seen = new List<string>();
        using var server = new TestServer(new WebHostBuilder().Configure(app => app.Run(async context =>
        {
            seen.Add(context.Request.Headers["x-correlation-id"].ToString());
            Assert.False(context.Request.Headers.ContainsKey("Authorization"));
            await context.Response.WriteAsJsonAsync(new Reply(1));
        })));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InternalApi:Services:TEST:BaseAddress"] = "https://localhost"
        }).Build();
        var services = new ServiceCollection();
        services.AddScoped<CorrelationContext>();
        services.AddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
        services.AddHrkInternalApi(config);
        services.AddHttpClient(InternalApiClientNames.Rest("TEST")).ConfigurePrimaryHttpMessageHandler(server.CreateHandler);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        foreach (var id in new[] { "request-1", "message-2" })
        {
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<CorrelationContext>().Initialize(id);
            await scope.ServiceProvider.GetRequiredService<IInternalApiClient>().GetAsync<Reply>("TEST", "/echo");
        }
        Assert.Equal(new[] { "request-1", "message-2" }, seen);
    }

    public sealed record Reply(int Value);
}
