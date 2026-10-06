using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using Oservability.Correlation;
using Oservability.Logging;
using Oservability.Http;
using Oservability.Tracing;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Oservability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddHrkObservability(this WebApplicationBuilder builder, string serviceName)
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;
        var version = builder.Configuration["ApplicationVersion"] ??
            Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddOptions<ApiLoggingOptions>()
            .Bind(builder.Configuration.GetSection(ApiLoggingOptions.SectionName))
            .Validate(options => options.MaxBodyBytes is >= 1 and <= 65536,
                "Observability:ApiLogging:MaxBodyBytes must be between 1 and 65536.")
            .ValidateOnStart();
        builder.Services.AddScoped<CorrelationContext>();
        builder.Services.AddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
        builder.Services.AddTransient<CorrelationHandler>();
        builder.Services.ConfigureHttpClientDefaults(client => client.AddHttpMessageHandler<CorrelationHandler>());
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog((services, log) =>
        {
            log.MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .MinimumLevel.Override("Ocelot", LogEventLevel.Warning)
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext().Enrich.With(new TelemetryLogEnricher(services.GetRequiredService<IHttpContextAccessor>()))
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .Enrich.WithProperty("ApplicationVersion", version)
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                .WriteTo.Console(new RenderedCompactJsonFormatter());
            var url = builder.Configuration["Seq:ServerUrl"];
            if (!string.IsNullOrWhiteSpace(url))
                log.WriteTo.Seq(url, apiKey: builder.Configuration["Seq:ApiKey"], eventBodyLimitBytes: 262144);
        }, preserveStaticLogger: true);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion: version,
                serviceInstanceId: Environment.MachineName))
            .WithTracing(traces =>
            {
                traces.AddSource(HrkTelemetry.SourceName, "HRK.Game")
                    .AddAspNetCoreInstrumentation(options => options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health"))
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation();
                if (builder.Configuration.GetValue<bool>("Telemetry:Otlp:Enabled"))
                    traces.AddOtlpExporter(options => ConfigureExporter(options, builder.Configuration, "traces"));
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(HrkTelemetry.MeterName, "HRK.Game")
                    .AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
                if (builder.Configuration.GetValue<bool>("Telemetry:Otlp:Enabled"))
                    metrics.AddOtlpExporter(options => ConfigureExporter(options, builder.Configuration, "metrics"));
            });
        builder.Services.AddSingleton(new ServiceMetadata(serviceName, version));
        builder.Services.AddHostedService<StartupLoggingService>();
        return builder;
    }

    public static IApplicationBuilder UseHrkCorrelation(this IApplicationBuilder app)
    {
        app.Use(async (http, next) =>
        {
            var context = http.RequestServices.GetRequiredService<CorrelationContext>();
            var supplied = http.Request.Headers[CorrelationContext.HeaderName];
            context.Initialize(supplied.Count == 1 ? supplied[0] : null);
            http.Request.Headers[CorrelationContext.HeaderName] = context.CorrelationId;
            http.Items[CorrelationContext.HeaderName] = context.CorrelationId;
            Activity.Current?.SetTag("correlation.id", context.CorrelationId);
            http.Response.OnStarting(() =>
            {
                http.Response.Headers[CorrelationContext.HeaderName] = context.CorrelationId;
                return Task.CompletedTask;
            });
            using (LogContext.PushProperty("CorrelationId", context.CorrelationId))
            using (LogContext.PushProperty("RequestPath", http.Request.Path.Value))
                await next(http);
        });
        app.UseMiddleware<ApiRequestLoggingMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseHrkIdentityLogging(this IApplicationBuilder app) =>
        app.Use(async (http, next) =>
        {
            using (LogContext.PushProperty("UserId", http.User.FindFirstValue(ClaimTypes.NameIdentifier)))
                await next(http);
        });

    private sealed record ServiceMetadata(string Name, string Version);
    private static void ConfigureExporter(OtlpExporterOptions options, IConfiguration configuration, string signal)
    {
        if (Enum.TryParse<OtlpExportProtocol>(configuration["Telemetry:Otlp:Protocol"], true, out var protocol))
            options.Protocol = protocol;
        if (configuration["Telemetry:Otlp:Endpoint"] is { Length: > 0 } endpoint)
            // Setting Endpoint directly is signal-specific; HTTP exporter doesn't append /v1/* itself.
            options.Endpoint = new Uri(options.Protocol == OtlpExportProtocol.HttpProtobuf
                ? $"{endpoint.TrimEnd('/')}/v1/{signal}" : endpoint);
    }
    private sealed class StartupLoggingService(ILogger<StartupLoggingService> logger, ServiceMetadata metadata,
        IHostEnvironment environment) : IHostedService
    {
        public Task StartAsync(CancellationToken ct)
        {
            logger.LogInformation("Starting {ServiceName} version {ApplicationVersion} in {Environment}",
                metadata.Name, metadata.Version, environment.EnvironmentName);
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken ct)
        {
            logger.LogInformation("Stopping {ServiceName}", metadata.Name);
            return Task.CompletedTask;
        }
    }
}
