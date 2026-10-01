using GAME.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services;

public class TowerQuickClimbWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TowerQuickClimbWorker> _logger;
    private readonly string _workerId;
    private readonly TowerWorkerOptions _options;

    public TowerQuickClimbWorker(IServiceScopeFactory scopeFactory, ILogger<TowerQuickClimbWorker> logger, IOptions<TowerWorkerOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        _workerId = $"worker-{Environment.MachineName}-{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TowerQuickClimbWorker {WorkerId} is starting.", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ITowerQuickClimbService>();

                bool didWork = await service.ProcessNextJobFloorAsync(_workerId, stoppingToken);

                if (didWork)
                {
                    // Small breathing room between floor simulations
                    await Task.Delay(Math.Max(1, _options.BetweenFloorsMs), stoppingToken);
                }
                else
                {
                    // Idle poll interval
                    await Task.Delay(Math.Max(1, _options.IdlePollMs), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in TowerQuickClimbWorker loop.");
                await Task.Delay(Math.Max(1, _options.ErrorBackoffMs), stoppingToken);
            }
        }

        _logger.LogInformation("TowerQuickClimbWorker {WorkerId} stopped.", _workerId);
    }
}
