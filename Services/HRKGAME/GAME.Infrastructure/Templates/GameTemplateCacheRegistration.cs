using Core.Caching.Hybrid;
using GAME.Application.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GAME.Infrastructure.Templates;

public static class GameTemplateCacheRegistration
{
    public static IServiceCollection AddGameTemplateCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHrkHybridCache(configuration);
        var hybrid = configuration.GetSection(HrkHybridCacheOptions.SectionName).Get<HrkHybridCacheOptions>() ?? new();
        services.AddOptions<GameTemplateCacheOptions>().Bind(configuration.GetSection(GameTemplateCacheOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.DataVersion) && x.DataVersion.Length <= 80, "A short DataVersion is required.")
            .Validate(x => x.Tables is { Length: > 0 } && x.Tables.All(t => !string.IsNullOrWhiteSpace(t)), "GameTemplateCache:Tables is required.")
            .Validate(x => x.LocalExpirationMinutes > 0 && x.DistributedExpirationMinutes > 0 && x.OperationTimeoutSeconds > 0 && x.MaximumTableBytes > 0,
                "Template cache limits must be positive.")
            .Validate(x => x.MaximumTableBytes + 1024L <= hybrid.MaximumPayloadBytes, "Template payload limit must leave 1 KiB for HybridCache serialization metadata.")
            .ValidateOnStart();
        services.AddSingleton<TemplateTableRegistry>();
        services.AddSingleton<GameTemplateStore>();
        services.AddSingleton<IGameTemplateRefresh>(sp => sp.GetRequiredService<GameTemplateStore>());
        services.AddScoped<IGameTemplateCatalog, GameTemplateCatalog>();
        services.AddHostedService<TemplateWarmup>();
        return services;
    }
}

internal sealed class TemplateWarmup(GameTemplateStore store, IOptions<GameTemplateCacheOptions> settings,
    ILogger<TemplateWarmup> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Value.WarmupOnStartup) return;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(settings.Value.OperationTimeoutSeconds));
        try { await store.WarmupAsync(deadline.Token); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogWarning(ex, "Template warmup failed. The next request will retry."); }
    }
}
