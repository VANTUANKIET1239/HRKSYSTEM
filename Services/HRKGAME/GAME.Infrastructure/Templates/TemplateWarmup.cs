using Core.Caching.Hybrid;
using GAME.Application.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GAME.Infrastructure.Templates;

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
