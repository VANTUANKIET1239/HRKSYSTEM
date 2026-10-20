using System.Globalization;
using System.Text.Json;
using Core.Persistence.Repositories;
using Core.Scheduling;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Messaging.Contracts;
using Core.TransactionalMessaging.Outbox;
using Core.TransactionalMessaging.Entities;
using Microsoft.Extensions.Logging;

namespace GAME.Application.Services;

public sealed record EventResetParameters(int BatchSize = 100)
{
    public static EventResetParameters Parse(string json)
    {
        var value = JsonSerializer.Deserialize<EventResetParameters>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new ArgumentException("Invalid event reset parameters.");
        if (value.BatchSize is < 1 or > 1000) throw new ArgumentException("BatchSize must be between 1 and 1000.");
        return value;
    }
}

public sealed class EventResetJobHandler(IUnitOfWork unitOfWork, ITowerEventService towerEvents,
    TowerOperationRunner runner, IOutboxWriter outbox,
    ILogger<EventResetJobHandler> logger) : IScheduledJobHandler
{
    public const string Key = "EVENT_PERIOD_RESET";
    public string HandlerKey => Key;

    public async Task<ScheduledJobResult> ExecuteAsync(ScheduledJobContext context, CancellationToken ct)
    {
        var eventId = int.Parse(context.Definition.ScopeKey, CultureInfo.InvariantCulture);
        var parameters = EventResetParameters.Parse(context.Definition.ParametersJson);
        var gameEvent = await unitOfWork.ReadOnlyRepository<HrkGameEvent>().Query().SingleAsync(x => x.Id == eventId, ct);
        if (gameEvent.EventType != "TOWER") throw new InvalidOperationException("No reset settlement handler is registered for this event type.");
        // Period creation uses the same definition as request-time period resolution.
        await runner.RunAsync($"reset:event:{eventId}", () => towerEvents.GetOrCreateCurrentPeriodAsync(gameEvent, ct), ct);
        var cutoff = DateTime.UtcNow;
        int closed = 0;
        long lastPlayerId = 0;
        var failedPlayers = new List<long>();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var players = await unitOfWork.ReadOnlyRepository<HrkPlayerEventPeriodProgress>().Query()
                .Where(x => x.EventId == eventId && x.PlayerId > lastPlayerId && x.ClosedAtUtc == null && x.EventPeriod.EndAtUtc <= cutoff)
                .Select(x => new { x.PlayerId, x.Player.UserId }).Distinct().OrderBy(x => x.PlayerId)
                .Take(parameters.BatchSize).ToListAsync(ct);
            if (players.Count == 0) break;
            foreach (var player in players)
            {
                // Uses exactly the lock used by battle/claim/quick-climb operations.
                try
                {
                    closed += await runner.RunAsync(player.UserId,
                        () => towerEvents.CloseExpiredPeriodsAsync(player.PlayerId, gameEvent, cutoff, context.ExecutionId, ct), ct);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception exception)
                {
                    failedPlayers.Add(player.PlayerId);
                    logger.LogWarning(exception, "Event reset failed for PlayerId={PlayerId}, GameEventId={GameEventId}, ExecutionId={ExecutionId}",
                        player.PlayerId, eventId, context.ExecutionId);
                }
                lastPlayerId = player.PlayerId;
            }
        }
        if (failedPlayers.Count > 0)
            throw new InvalidOperationException($"Reset requires retry for {failedPlayers.Count} players. First IDs: {string.Join(',', failedPlayers.Take(20))}");
        // Include work committed by earlier attempts of this same execution.
        closed = await unitOfWork.ReadOnlyRepository<HrkPlayerEventPeriodProgress>().Query()
            .CountAsync(x => x.ResetExecutionId == context.ExecutionId, ct);
        if (closed > 0)
        {
            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (await unitOfWork.ReadOnlyRepository<OutboxMessage>().Query().AnyAsync(x => x.Id == context.ExecutionId, ct)) return;
                var message = new PlayerActivityRecordedV1(context.ExecutionId, "EventResetCompleted", null, null,
                    "JobExecution", context.ExecutionId.ToString(), "HRK.GAME", DateTimeOffset.UtcNow,
                    JsonSerializer.SerializeToElement(new { GameEventId = eventId, ClosedPeriods = closed }),
                    GameEventId: eventId, JobExecutionId: context.ExecutionId);
                outbox.Add(message, ActivityContractNames.PlayerActivityRecordedV1, MessagingPublisherNames.GameEvents,
                    ActivityContractNames.PlayerActivityRecordedV1);
                await unitOfWork.SaveChangesAsync(ct);
            }, ct);
        }
        return new(closed, JsonSerializer.Serialize(new { GameEventId = eventId, ClosedPeriods = closed, CutoffUtc = cutoff }));
    }
}
