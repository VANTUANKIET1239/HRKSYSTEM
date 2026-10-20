using GAME.Application.Interfaces;
using GAME.Domain.Entities;

namespace GAME.Application.Events;

public static class EventActivityExtensions
{
    public static void RaiseEvent(this IPlayerActivityEvents activities, HrkPlayer player, HrkEventPeriod period,
        string type, string sourceId, object metadata, DateTime? occurredAtUtc = null)
    {
        activities.Raise(new GameEventActivityEvent(player.Id, player.UserId, type, sourceId, metadata)
        {
            GameEventId = period.EventId, EventPeriodId = period.Id, PeriodKey = period.PeriodKey,
            OccurredAt = occurredAtUtc.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(occurredAtUtc.Value, DateTimeKind.Utc)) : null
        });
    }
}
