using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Infrastructure.Locking;
using Core.Application.DependencyInjection;
using GAME.Application.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace GAME.Domain.Tests;

internal static partial class RankingTestSupport
{
    public static IServiceCollection PipelineServices(IPlayerRankingWriter writer, IGameRequestContext actor)
    {
        var services = new ServiceCollection();
        services.AddCoreApplication(typeof(IPlayerRankingWriter).Assembly).AddPlayerPowerChangePipeline();
        services.AddSingleton(writer);
        services.AddSingleton(actor);
        return services;
    }

    public static IGameTransactionLock Locks(IUnitOfWork uow) => new SqlServerGameTransactionLock(uow);

    public static PlayerRankingWriter Writer(IUnitOfWork uow, int value = 0) => Writer(uow, new Power(value));

    public static PlayerRankingWriter Writer(IUnitOfWork uow, IFormationPowerQueryService power)
    {
        var locks = Locks(uow);
        return new(uow, power, locks, new PlayerRankingStatsStore(uow, locks, TimeProvider.System), TimeProvider.System);
    }

    public static DiamondSpendingService Spending(IUnitOfWork uow)
    {
        var locks = Locks(uow);
        return new(uow, locks, new PlayerRankingStatsStore(uow, locks, TimeProvider.System), TimeProvider.System);
    }
}
