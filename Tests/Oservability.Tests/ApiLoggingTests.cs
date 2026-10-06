using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Oservability.Correlation;
using Oservability.Http;
using Oservability.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Oservability.Tests;

public class ApiLoggingTests
{
    [Fact]
    public void HostConfigs_EnableLifecycleAndDisableBodiesByDefault()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "HRK.sln"))) root = root.Parent;
        Assert.NotNull(root);
        foreach (var path in new[] { "ApiGateway/ApiGateway", "Services/AUTH/HRK.AUTH",
            "Services/HRKGAME/HRK.GAME", "Services/HRKREALTIME/HRK.REALTIME", "Services/HRKACTIVITY/HRK.ACTIVITY" })
        {
            var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(root.FullName, path, "appsettings.json")).Build();
            var options = config.GetSection(ApiLoggingOptions.SectionName).Get<ApiLoggingOptions>();
            Assert.NotNull(options);
            Assert.True(options.Enabled);
            Assert.False(options.LogRequestBody);
            Assert.False(options.LogResponseBody);
            Assert.Equal(4096, options.MaxBodyBytes);
        }
    }

    [Fact]
    public async Task ConcurrentRequests_HaveOnePairAndSharedTraceWithBusinessLogs()
    {
        var sink = new Sink();
        await using var app = await StartAsync(sink, new(), app =>
            app.MapGet("/test", async (ILogger<ApiLoggingTests> logger) =>
            {
                await Task.Yield();
                logger.LogInformation("Business step {ItemId} with {RequestId}", 7, "business-request");
                return Results.Ok();
            }));
        await Task.WhenAll(Enumerable.Range(0, 12).Select(i =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/test?access_token=must-not-log");
            request.Headers.Add(CorrelationContext.HeaderName, $"request-{i}");
            return app.GetTestClient().SendAsync(request);
        }));
        for (var i = 0; i < 12; i++)
        {
            var events = sink.Events.Where(e => Value(e, "CorrelationId") == $"request-{i}").ToArray();
            var start = Assert.Single(events, e => Value(e, "EventName") == "ApiStarted");
            var end = Assert.Single(events, e => Value(e, "EventName") == "ApiEnded");
            Assert.Equal("Success", Value(end, "Outcome"));
            Assert.NotNull(Value(start, "TraceId"));
            Assert.All(events, e => Assert.Equal(Value(start, "TraceId"), Value(e, "TraceId")));
            Assert.All(events, e => Assert.Equal(Value(start, "HttpRequestId"), Value(e, "HttpRequestId")));
            Assert.Equal(Value(start, "RequestId"), Value(end, "RequestId"));
            Assert.False(start.Properties.ContainsKey("ItemId"));
            Assert.False(end.Properties.ContainsKey("ItemId"));
        }
        Assert.DoesNotContain("must-not-log", string.Join("\n", sink.Events.Select(e => Render(e))));
    }

    [Theory]
    [InlineData(400, LogEventLevel.Warning, "ClientError")]
    [InlineData(401, LogEventLevel.Warning, "ClientError")]
    [InlineData(500, LogEventLevel.Error, "ServerError")]
    public async Task HttpFailures_LogEndWithCorrectLevel(int status, LogEventLevel level, string outcome)
    {
        var sink = new Sink();
        await using var app = await StartAsync(sink, new(), app =>
            app.MapGet("/test", () => Results.StatusCode(status)));
        await app.GetTestClient().GetAsync("/test");
        var end = Assert.Single(sink.Events, e => Value(e, "EventName") == "ApiEnded");
        Assert.Equal(level, end.Level);
        Assert.Equal(outcome, Value(end, "Outcome"));
    }

    [Fact]
    public async Task UnhandledException_Logs500AndStillPropagates()
    {
        var sink = new Sink();
        await using var app = await StartAsync(sink, new(), app =>
            app.Run(_ => throw new InvalidOperationException("test failure")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => app.GetTestClient().GetAsync("/test"));
        var end = Assert.Single(sink.Events, e => Value(e, "EventName") == "ApiEnded");
        Assert.Equal("500", Value(end, "StatusCode"));
        Assert.Equal(LogEventLevel.Error, end.Level);
        Assert.IsType<InvalidOperationException>(end.Exception);
    }

    [Fact]
    public async Task BodyCapture_RedactsNestedSecretsAndPreservesRequestAndResponse()
    {
        var sink = new Sink();
        var options = new ApiLoggingOptions
        {
            LogRequestBody = true, LogResponseBody = true,
            BodyPaths = ["/echo"], AllowedBodyFields = ["count", "password", "access_token"]
        };
        await using var app = await StartAsync(sink, options, app =>
            app.MapPost("/echo", async (HttpContext http) =>
            {
                var json = await new StreamReader(http.Request.Body).ReadToEndAsync();
                http.Response.ContentType = "application/json";
                await http.Response.WriteAsync(json);
            }));
        const string body = "{\"count\":2,\"password\":\"secret-one\",\"nested\":{\"access_token\":\"secret-two\",\"other\":\"secret-three\"}}";
        using var response = await app.GetTestClient().PostAsync("/echo", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(body, await response.Content.ReadAsStringAsync());
        var logged = string.Join("\n", sink.Events.Select(e => Render(e)));
        Assert.Contains("ApiInput", logged);
        Assert.Contains("ApiOutput", logged);
        Assert.Contains("REDACTED", logged);
        Assert.DoesNotContain("secret-one", logged);
        Assert.DoesNotContain("secret-two", logged);
        Assert.DoesNotContain("secret-three", logged);
    }

    [Fact]
    public async Task OversizedBody_IsOmittedAndDoesNotTruncateClientResponse()
    {
        var sink = new Sink();
        var options = new ApiLoggingOptions
        {
            MaxBodyBytes = 64, LogRequestBody = true, LogResponseBody = true,
            BodyPaths = ["/echo"], AllowedBodyFields = ["value"]
        };
        await using var app = await StartAsync(sink, options, app =>
            app.MapPost("/echo", async (HttpContext http) =>
            {
                http.Response.ContentType = "application/json";
                await http.Request.Body.CopyToAsync(http.Response.Body);
            }));
        var body = "{\"value\":\"" + new string('x', 2000) + "\"}";
        using var response = await app.GetTestClient().PostAsync("/echo", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(body, await response.Content.ReadAsStringAsync());
        var logged = string.Join("\n", sink.Events.Select(e => Render(e)));
        Assert.Contains("BodyTooLarge", logged);
        Assert.DoesNotContain(new string('x', 65), logged);
    }

    [Fact]
    public async Task DisabledLoggingAndExcludedPaths_DoNotLogLifecycle()
    {
        var sink = new Sink();
        await using (var app = await StartAsync(sink, new() { Enabled = false }, app => app.MapGet("/test", () => "ok")))
            await app.GetTestClient().GetAsync("/test");
        await using (var app = await StartAsync(sink, new(), app => app.MapGet("/health/live", () => "ok")))
            await app.GetTestClient().GetAsync("/health/live");
        Assert.DoesNotContain(sink.Events, e => e.Properties.ContainsKey("EventName"));
    }

    [Fact]
    public async Task BodiesAreNotLoggedOutsideAllowedPathsEvenWhenFlagsAreEnabled()
    {
        var sink = new Sink();
        await using var app = await StartAsync(sink, new()
        {
            LogRequestBody = true, LogResponseBody = true,
            BodyPaths = ["/allowed"], AllowedBodyFields = ["value"]
        }, app => app.MapPost("/other", () => Results.Json(new { value = "private-output" })));
        await app.GetTestClient().PostAsync("/other", new StringContent("{\"value\":\"private-input\"}", Encoding.UTF8, "application/json"));
        Assert.DoesNotContain(sink.Events, e => Value(e, "EventName") is "ApiInput" or "ApiOutput");
        Assert.DoesNotContain("private-input", string.Join("\n", sink.Events.Select(Render)));
        Assert.DoesNotContain("private-output", string.Join("\n", sink.Events.Select(Render)));
    }

    [Fact]
    public async Task RequestCancellation_LogsAbortedAndDoesNotHideCancellation()
    {
        var sink = new Sink();
        using var log = CreateLogger(sink);
        using var factory = new Serilog.Extensions.Logging.SerilogLoggerFactory(log, false);
        using var cts = new CancellationTokenSource();
        var context = new DefaultHttpContext { RequestAborted = cts.Token };
        var middleware = new ApiRequestLoggingMiddleware(_ =>
        {
            cts.Cancel();
            throw new OperationCanceledException(cts.Token);
        }, factory.CreateLogger<ApiRequestLoggingMiddleware>(), new Monitor(new()));
        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));
        var end = Assert.Single(sink.Events, e => Value(e, "EventName") == "ApiEnded");
        Assert.Equal("Aborted", Value(end, "Outcome"));
        Assert.Equal(LogEventLevel.Warning, end.Level);
        Assert.Null(end.Exception);
    }

    private static async Task<WebApplication> StartAsync(Sink sink, ApiLoggingOptions options, Action<WebApplication> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog(CreateLogger(sink), dispose: true);
        builder.Services.AddScoped<CorrelationContext>();
        builder.Services.AddSingleton<IOptionsMonitor<ApiLoggingOptions>>(new Monitor(options));
        var app = builder.Build();
        app.UseHrkCorrelation();
        configure(app);
        await app.StartAsync();
        return app;
    }

    private static Serilog.Core.Logger CreateLogger(Sink sink) => new LoggerConfiguration()
        .MinimumLevel.Debug().MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext().Enrich.With(new TelemetryLogEnricher())
        .WriteTo.Sink(sink).CreateLogger();
    private static string Render(LogEvent e)
    {
        using var writer = new StringWriter();
        new Serilog.Formatting.Json.JsonFormatter().Format(e, writer);
        return writer.ToString();
    }
    private static string? Value(LogEvent e, string key) =>
        e.Properties.TryGetValue(key, out var value) ? (value as ScalarValue)?.Value?.ToString() : null;
    private sealed class Sink : ILogEventSink
    {
        public ConcurrentQueue<LogEvent> Events { get; } = new();
        public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
    }
    private sealed class Monitor(ApiLoggingOptions value) : IOptionsMonitor<ApiLoggingOptions>
    {
        public ApiLoggingOptions CurrentValue => value;
        public ApiLoggingOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<ApiLoggingOptions, string?> listener) => null;
    }
}
