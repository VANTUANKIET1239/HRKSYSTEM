using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Oservability;
using Xunit;

namespace Oservability.Tests;

[Collection("Docker")]
public class SeqIntegrationTests
{
    [DockerFact]
    public async Task HostLogger_SendsStructuredTraceAndCorrelationToSeqWithoutQueryTokens()
    {
        // Authentication is disabled only for this disposable, randomly-bound test container.
        await using var seq = new ContainerBuilder().WithImage("datalust/seq:2025.2")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SEQ_FIRSTRUN_NOAUTHENTICATION", "true")
            .WithPortBinding(80, true).WithPortBinding(5341, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(80).ForPath("/health")))
            .Build();
        await seq.StartAsync();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Seq:ServerUrl"] = $"http://{seq.Hostname}:{seq.GetMappedPublicPort(5341)}";
        builder.AddHrkObservability("SeqSmoke");
        await using var app = builder.Build();
        app.UseHrkCorrelation();
        var marker = Guid.NewGuid().ToString("N");
        app.MapGet("/test", (ILogger<SeqIntegrationTests> logger) =>
        {
            logger.LogInformation("Seq verification {VerificationId}", marker);
            return "ok";
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test?access_token=do-not-record-this-token");
        request.Headers.Add("X-Correlation-Id", "seq-correlation-test");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        await app.StopAsync();
        using var seqClient = new HttpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        string events;
        do
        {
            timeout.Token.ThrowIfCancellationRequested();
            events = await seqClient.GetStringAsync($"http://{seq.Hostname}:{seq.GetMappedPublicPort(80)}/api/events?count=100", timeout.Token);
            if (!events.Contains(marker)) await Task.Delay(200, timeout.Token);
        } while (!events.Contains(marker));
        Assert.Contains("SeqSmoke", events);
        Assert.Contains("seq-correlation-test", events);
        Assert.Contains("TraceId", events);
        Assert.Contains("SpanId", events);
        Assert.DoesNotContain("do-not-record-this-token", events);
    }
}
