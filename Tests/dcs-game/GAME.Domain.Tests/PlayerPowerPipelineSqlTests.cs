using Core.Application.CQRS;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using FluentValidation;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using HRK.GAME.Configuration;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class PlayerPowerPipelineSqlTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>
{
    [SummonSqlFact]
    public async Task ExplicitPlayerIdentityWorksWithoutHttpContextAndRecordsPowerOnce()
    {
        var player = await database.PlayerAsync(gold: 100);
        await using var db = database.Context();
        await using var uow = new UnitOfWork<GameDbContext>(db);
        var power = new RankingTestSupport.Power(700);
        var services = Services(uow, power, async (request, ct) =>
        {
            Assert.True(uow.HasActiveTransaction);
            var wallet = await uow.Repository<HrkPlayerWallet>().Query().SingleAsync(x => x.PlayerId == player.Id, ct);
            wallet.Gold += request.GoldDelta;
            return checked((int)wallet.Gold);
        });
        using var provider = services.BuildServiceProvider();
        Assert.Equal(101, await provider.GetRequiredService<ICqrsDispatcher>().CommandAsync(new PlayerPowerProbeCommand(player.UserId)));
        Assert.False(uow.HasActiveTransaction);
        Assert.Equal(1, power.Calls);
        Assert.Equal(101, (await db.PlayerWallets.AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).Gold);
        Assert.Equal(700, (await db.Set<HrkPlayerRankingStats>().AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).PeakFormationPower);
    }

    [SummonSqlFact]
    public async Task HandlerFailureRollsBackChangesSavedInsideThePipeline()
    {
        var player = await database.PlayerAsync(gold: 100);
        await using var db = database.Context();
        await using var uow = new UnitOfWork<GameDbContext>(db);
        var power = new RankingTestSupport.Power(700);
        var services = Services(uow, power, async (_, ct) =>
        {
            var wallet = await uow.Repository<HrkPlayerWallet>().Query().SingleAsync(x => x.PlayerId == player.Id, ct);
            wallet.Gold++;
            await uow.SaveChangesAsync(ct);
            throw new InvalidOperationException("Business operation failed.");
        });
        using var provider = services.BuildServiceProvider();
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetRequiredService<ICqrsDispatcher>()
            .CommandAsync(new PlayerPowerProbeCommand(player.UserId)));
        Assert.False(uow.HasActiveTransaction);
        Assert.Equal(0, power.Calls);
        Assert.Equal(100, (await db.PlayerWallets.AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).Gold);
        Assert.False(await db.Set<HrkPlayerRankingStats>().AnyAsync(x => x.PlayerId == player.Id));
    }

    [SummonSqlFact]
    public async Task PowerCalculationFailureRollsBackTheBusinessChange()
    {
        var player = await database.PlayerAsync(gold: 100);
        await using var db = database.Context();
        await using var uow = new UnitOfWork<GameDbContext>(db);
        var power = new RankingTestSupport.Power { Failure = new InvalidOperationException("Power calculation failed.") };
        var services = Services(uow, power, async (_, ct) =>
        {
            var wallet = await uow.Repository<HrkPlayerWallet>().Query().SingleAsync(x => x.PlayerId == player.Id, ct);
            wallet.Gold++;
            return 1;
        });
        using var provider = services.BuildServiceProvider();
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetRequiredService<ICqrsDispatcher>()
            .CommandAsync(new PlayerPowerProbeCommand(player.UserId)));
        Assert.False(uow.HasActiveTransaction);
        Assert.Equal(1, power.Calls);
        Assert.Equal(100, (await db.PlayerWallets.AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).Gold);
        Assert.False(await db.Set<HrkPlayerRankingStats>().AnyAsync(x => x.PlayerId == player.Id));
    }

    [SummonSqlFact]
    public async Task CancellationRollsBackAndDoesNotCalculatePower()
    {
        var player = await database.PlayerAsync(gold: 100);
        await using var db = database.Context();
        await using var uow = new UnitOfWork<GameDbContext>(db);
        using var cancellation = new CancellationTokenSource();
        var power = new RankingTestSupport.Power();
        var services = Services(uow, power, async (_, ct) =>
        {
            var wallet = await uow.Repository<HrkPlayerWallet>().Query().SingleAsync(x => x.PlayerId == player.Id, ct);
            wallet.Gold++;
            await uow.SaveChangesAsync(ct);
            cancellation.Cancel();
            ct.ThrowIfCancellationRequested();
            return 1;
        });
        using var provider = services.BuildServiceProvider();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetRequiredService<ICqrsDispatcher>()
            .CommandAsync(new PlayerPowerProbeCommand(player.UserId), cancellation.Token));
        Assert.Equal(0, power.Calls);
        Assert.False(uow.HasActiveTransaction);
        Assert.Equal(100, (await db.PlayerWallets.AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).Gold);
    }

    [SummonSqlFact]
    public async Task ValidationRunsBeforePlayerLookupAndBeforeTheHandler()
    {
        await using var db = database.Context();
        await using var uow = new UnitOfWork<GameDbContext>(db);
        var called = false;
        var services = Services(uow, new RankingTestSupport.Power(), (_, _) =>
        {
            called = true;
            return Task.FromResult(1);
        });
        var validator = new InlineValidator<PlayerPowerProbeCommand>();
        validator.RuleFor(x => x.GoldDelta).GreaterThan(0);
        services.AddSingleton<IValidator<PlayerPowerProbeCommand>>(validator);
        using var provider = services.BuildServiceProvider();
        await Assert.ThrowsAsync<RequestValidationException>(() => provider.GetRequiredService<ICqrsDispatcher>()
            .CommandAsync(new PlayerPowerProbeCommand("missing-player", 0)));
        Assert.False(called);
        Assert.False(uow.HasActiveTransaction);
    }

    [SummonSqlFact]
    public async Task ConcurrentCommandsLockBeforeReadingAndPreserveTheHighestPeak()
    {
        var player = await database.PlayerAsync(gold: 100);
        async Task Change(int value)
        {
            await using var db = database.Context();
            await using var uow = new UnitOfWork<GameDbContext>(db);
            var power = new RankingTestSupport.Power(value);
            var services = Services(uow, power, async (_, ct) =>
            {
                var wallet = await uow.Repository<HrkPlayerWallet>().Query().SingleAsync(x => x.PlayerId == player.Id, ct);
                wallet.Gold++;
                return 1;
            });
            using var provider = services.BuildServiceProvider();
            await provider.GetRequiredService<ICqrsDispatcher>().CommandAsync(new PlayerPowerProbeCommand(player.UserId));
            Assert.Equal(1, power.Calls);
        }
        await Task.WhenAll(Change(700), Change(900), Change(800));
        await using var read = database.Context();
        Assert.Equal(103, (await read.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
        Assert.Equal(900, (await read.Set<HrkPlayerRankingStats>().SingleAsync(x => x.PlayerId == player.Id)).PeakFormationPower);
    }

    private static IServiceCollection Services(IUnitOfWork uow, IFormationPowerQueryService power,
        Func<PlayerPowerProbeCommand, CancellationToken, Task<int>> handle)
    {
        // Empty HTTP context deliberately verifies worker commands use the identity in the request.
        var services = RankingTestSupport.PipelineServices(RankingTestSupport.Writer(uow, power),
            new HttpGameRequestContext(new HttpContextAccessor()));
        services.AddSingleton<IRequestHandler<PlayerPowerProbeCommand, int>>(new PipelineTestHandler<PlayerPowerProbeCommand, int>(handle));
        return services;
    }
}
