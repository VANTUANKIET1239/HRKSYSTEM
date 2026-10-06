using System.Text.Json;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Oservability;
using Oservability.Tracing;
using Xunit;

namespace Oservability.Tests;

[Collection("Docker")]
public class TelemetryIntegrationTests
{
    [DockerFact]
    public async Task OtlpCollector_ExportsTraceToJaegerAndMetricsForPrometheus()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "docker", "otel-collector-config.yaml")))
            folder = folder.Parent;
        Assert.NotNull(folder);
        await using var network = new NetworkBuilder().Build();
        await network.CreateAsync();
        await using var jaeger = new ContainerBuilder().WithImage("jaegertracing/all-in-one:1.68.0")
            .WithNetwork(network).WithNetworkAliases("jaeger")
            .WithEnvironment("COLLECTOR_OTLP_ENABLED", "true").WithPortBinding(16686, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(16686).ForPath("/")))
            .Build();
        await using var collector = new ContainerBuilder().WithImage("otel/opentelemetry-collector-contrib:0.123.0")
            .WithNetwork(network).WithPortBinding(4318, true).WithPortBinding(8889, true)
            .WithResourceMapping(Path.Combine(folder!.FullName, "docker", "otel-collector-config.yaml"), "/etc/otelcol/")
            .WithCommand("--config=/etc/otelcol/otel-collector-config.yaml")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Everything is ready"))
            .Build();
        await jaeger.StartAsync();
        try { await collector.StartAsync(); }
        catch (Exception exception)
        {
            var logs = await collector.GetLogsAsync();
            throw new InvalidOperationException($"Collector startup failed: {logs.Stdout} {logs.Stderr}", exception);
        }
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Telemetry:Otlp:Enabled"] = "true";
        builder.Configuration["Telemetry:Otlp:Endpoint"] = $"http://{collector.Hostname}:{collector.GetMappedPublicPort(4318)}";
        builder.Configuration["Telemetry:Otlp:Protocol"] = "HttpProtobuf";
        builder.AddHrkObservability("TelemetrySmoke");
        await using var app = builder.Build();
        await app.StartAsync();
        string? traceId;
        using (var operation = new GameOperation("battle"))
        {
            traceId = System.Diagnostics.Activity.Current?.TraceId.ToHexString();
            operation.Complete();
        }
        Assert.NotNull(traceId);
        Assert.True(app.Services.GetRequiredService<TracerProvider>().ForceFlush());
        Assert.True(app.Services.GetRequiredService<MeterProvider>().ForceFlush());
        await app.StopAsync();
        using var http = new HttpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        while (true)
        {
            timeout.Token.ThrowIfCancellationRequested();
            var response = await http.GetStringAsync($"http://{jaeger.Hostname}:{jaeger.GetMappedPublicPort(16686)}/api/traces?service=TelemetrySmoke", timeout.Token);
            using var document = JsonDocument.Parse(response);
            if (document.RootElement.GetProperty("data").EnumerateArray()
                .Any(trace => trace.GetProperty("traceID").GetString() == traceId)) break;
            await Task.Delay(200, timeout.Token);
        }
        string metrics;
        do
        {
            timeout.Token.ThrowIfCancellationRequested();
            metrics = await http.GetStringAsync($"http://{collector.Hostname}:{collector.GetMappedPublicPort(8889)}/metrics", timeout.Token);
            if (!metrics.Contains("game_operation_count")) await Task.Delay(200, timeout.Token);
        } while (!metrics.Contains("game_operation_count"));
        Assert.Contains("game_operation_count", metrics);
        Assert.Contains("TelemetrySmoke", metrics);
    }
}
