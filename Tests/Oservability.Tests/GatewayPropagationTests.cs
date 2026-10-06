using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Oservability;
using Xunit;

namespace Oservability.Tests;

public class GatewayPropagationTests
{
    [Fact]
    public async Task Ocelot_PropagatesCanonicalCorrelationAndW3cTraceToDownstream()
    {
        var backendBuilder = WebApplication.CreateBuilder();
        backendBuilder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, 0));
        backendBuilder.AddHrkObservability("DownstreamTest");
        await using var backend = backendBuilder.Build();
        backend.UseHrkCorrelation();
        backend.MapGet("/echo", (Microsoft.AspNetCore.Http.HttpContext http) => new
        {
            CorrelationId = http.Request.Headers["X-Correlation-Id"].ToString(),
            TraceId = System.Diagnostics.Activity.Current?.TraceId.ToHexString()
        });
        await backend.StartAsync();
        var port = new Uri(backend.Urls.Single()).Port;
        var gatewayBuilder = WebApplication.CreateBuilder();
        gatewayBuilder.WebHost.UseTestServer();
        gatewayBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Routes:0:DownstreamPathTemplate"] = "/echo",
            ["Routes:0:DownstreamScheme"] = "http",
            ["Routes:0:DownstreamHostAndPorts:0:Host"] = "127.0.0.1",
            ["Routes:0:DownstreamHostAndPorts:0:Port"] = port.ToString(),
            ["Routes:0:UpstreamPathTemplate"] = "/proxy",
            ["Routes:0:UpstreamHttpMethod:0"] = "Get"
        });
        gatewayBuilder.AddHrkObservability("GatewayTest");
        gatewayBuilder.Services.AddOcelot(gatewayBuilder.Configuration);
        await using var gateway = gatewayBuilder.Build();
        gateway.UseHrkCorrelation();
        await gateway.UseOcelot();
        await gateway.StartAsync();
        using var client = gateway.GetTestClient();
        var traceId = ActivityTraceId.CreateRandom().ToHexString();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/proxy");
        request.Headers.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom().ToHexString()}-01");
        request.Headers.Add("X-Correlation-Id", "business-operation");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("business-operation", body.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal(traceId, body.RootElement.GetProperty("traceId").GetString());
        using var generated = await client.GetAsync("/proxy");
        generated.EnsureSuccessStatusCode();
        using var generatedBody = JsonDocument.Parse(await generated.Content.ReadAsStringAsync());
        Assert.Equal(generated.Headers.GetValues("X-Correlation-Id").Single(),
            generatedBody.RootElement.GetProperty("correlationId").GetString());
    }
}
