using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Application.Common;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GAME.Infrastructure.Ranking;

/// <summary>Backfills only missing power baselines. Existing records and spending totals are preserved.</summary>
public sealed class LeaderboardInitializationWorker(IServiceScopeFactory scopes, ILogger<LeaderboardInitializationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var batchScope = scopes.CreateScope();
                var store = batchScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var records = store.ReadOnlyRepository<HrkPlayerRankingStats>().Query();
                var ids = await store.ReadOnlyRepository<HrkPlayer>().Query()
                    .Where(p => p.IsActive && !records
                        .Any(r => r.PlayerId == p.Id && r.PowerInitialized))
                    .OrderBy(p => p.Id).Select(p => p.Id).Take(50).ToListAsync(stoppingToken);
                if (ids.Count == 0) return;
                foreach (var id in ids)
                {
                    using var scope = scopes.CreateScope();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var writer = scope.ServiceProvider.GetRequiredService<IPlayerRankingWriter>();
                    var locks = scope.ServiceProvider.GetRequiredService<IGameTransactionLock>();
                    await uow.ExecuteInTransactionAsync(async () =>
                    {
                        await locks.AcquireAsync(GameLockResources.PlayerState(id), stoppingToken);
                        if (!await uow.ReadOnlyRepository<HrkPlayerRankingStats>().Query()
                                .AnyAsync(x => x.PlayerId == id && x.PowerInitialized, stoppingToken))
                            await writer.RefreshPowerAsync(id, stoppingToken);
                    }, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not initialize leaderboard baselines; retrying in 30 seconds. Apply Migration_PlayerLeaderboards.sql first.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
