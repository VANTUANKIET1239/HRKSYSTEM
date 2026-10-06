using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Activity.Application;
using Core.Messaging.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Oservability;
using Oservability.Correlation;
using Oservability.Logging;
using Oservability.Tracing;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Oservability.Tests;

public class ObservabilityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid id")]
    [InlineData("bad\r\nheader")]
    public void Correlation_ReplacesMissingOrInvalidInput(string? value)
    {
        var context = new CorrelationContext();
        context.Initialize(value);
        Assert.True(Guid.TryParse(context.CorrelationId, out _));
    }

    [Fact]
    public async Task Middleware_IsolatesConcurrentRequestsAndReturnsHeader()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddHrkObservability("TestService");
        await using var app = builder.Build();
        app.UseHrkCorrelation();
        app.MapGet("/test", async (ICorrelationContext context) =>
        {
            await Task.Yield();
            return context.CorrelationId;
        });
        await app.StartAsync();
        var client = app.GetTestClient();
        await Task.WhenAll(Enumerable.Range(0, 20).Select(async i =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/test");
            request.Headers.Add(CorrelationContext.HeaderName, $"operation-{i}");
            using var response = await client.SendAsync(request);
            Assert.Equal($"operation-{i}", await response.Content.ReadAsStringAsync());
            Assert.Equal($"operation-{i}", response.Headers.GetValues(CorrelationContext.HeaderName).Single());
        }));
        using var generated = await client.GetAsync("/test");
        Assert.True(Guid.TryParse(generated.Headers.GetValues(CorrelationContext.HeaderName).Single(), out _));
    }

    [Fact]
    public void LoggerOnly_EnrichesExistingActivityWithoutCreatingNewSpan()
    {
        var sink = new CapturingSink();
        using var serilog = new LoggerConfiguration().Enrich.With(new TelemetryLogEnricher()).WriteTo.Sink(sink).CreateLogger();
        using var factory = new Serilog.Extensions.Logging.SerilogLoggerFactory(serilog, false);
        using var activity = new System.Diagnostics.Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();
        factory.CreateLogger("Example").LogInformation("Processed player {PlayerId}", 123);
        var log = Assert.Single(sink.Events);
        Assert.Equal(activity.TraceId.ToHexString(), ((ScalarValue)log.Properties["TraceId"]).Value);
        Assert.Equal(activity.SpanId.ToHexString(), ((ScalarValue)log.Properties["SpanId"]).Value);
        Assert.Same(activity, System.Diagnostics.Activity.Current);
        activity.Stop();
        factory.CreateLogger("Example").LogInformation("No activity");
        Assert.False(sink.Events.Last().Properties.ContainsKey("TraceId"));
    }

    [Fact]
    public void Messaging_W3cPropagationPreservesParentAndRejectsMalformedContext()
    {
        using var parent = new System.Diagnostics.Activity("producer").SetIdFormat(ActivityIdFormat.W3C).Start();
        var headers = new Dictionary<string, object?>();
        HrkTelemetry.Inject(headers);
        headers["traceparent"] = Encoding.UTF8.GetBytes((string)headers["traceparent"]!);
        var extracted = HrkTelemetry.Extract(headers);
        Assert.True(extracted.IsRemote);
        Assert.Equal(parent.TraceId, extracted.TraceId);
        Assert.Equal(parent.SpanId, extracted.SpanId);
        Assert.Equal(default, HrkTelemetry.Extract("bad", null));
    }

    [Fact]
    public void ActivityMapping_ValidatesIdentityAndStoresSummaryInUtc()
    {
        var id = Guid.NewGuid();
        var message = new PlayerActivityRecordedV1(id, "ItemEnhanced", 42, "user-7", "Item", "123",
            "HRK.GAME", DateTimeOffset.Now, JsonSerializer.SerializeToElement(new { OldLevel = 1, NewLevel = 2 }));
        var mapped = ActivityMapping.Map(new(message, id.ToString("N"), "enhance-1", "0123456789abcdef0123456789abcdef"));
        Assert.Equal(42, mapped.PlayerId);
        Assert.Equal("user-7", mapped.UserId);
        Assert.Equal(TimeSpan.Zero, mapped.OccurredAt.Offset);
        Assert.Equal("enhance-1", mapped.CorrelationId);
        Assert.Throws<ArgumentException>(() => ActivityMapping.Map(new(message, Guid.NewGuid().ToString("N"), null, null)));
    }

    private sealed class CapturingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
