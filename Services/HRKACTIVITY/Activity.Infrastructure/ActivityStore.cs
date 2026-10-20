using Activity.Application;
using Activity.Domain;
using Core.TransactionalMessaging.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Activity.Infrastructure;

public sealed class ActivityStore(ActivityDbContext db, IInboxExecutor inbox, ILogger<ActivityStore> logger) : IActivityRecorder, IActivityQueries
{
    public const string ConsumerName = "ActivityTimeline";

    public async Task RecordAsync(RecordPlayerActivityCommand command, CancellationToken ct)
    {
        var activity = ActivityMapping.Map(command);
        var execution = await inbox.ExecuteAsync(ConsumerName, command.MessageId, async token =>
        {
            // Also protects against replay after an Inbox retention change.
            if (!await db.PlayerActivities.AnyAsync(x => x.ActivityId == activity.ActivityId, token))
                db.PlayerActivities.Add(activity);
            return true;
        }, ct);
        if (execution.IsDuplicate) logger.LogDebug("Skipped duplicate activity message {MessageId}", command.MessageId);
    }

    public async Task<IReadOnlyList<PlayerActivity>> GetAsync(GetActivitiesQuery query, CancellationToken ct)
    {
        if (!query.IsValid) throw new ArgumentException("Invalid activity filters or pagination.");
        var records = db.PlayerActivities.AsNoTracking();
        if (query.PlayerId is { } playerId) records = records.Where(x => x.PlayerId == playerId);
        if (query.CorrelationId is { } correlationId) records = records.Where(x => x.CorrelationId == correlationId);
        if (query.EntityType is { } entityType) records = records.Where(x => x.EntityType == entityType);
        if (query.EntityId is { } entityId) records = records.Where(x => x.EntityId == entityId);
        if (query.GameEventId is { } eventId) records = records.Where(x => x.GameEventId == eventId);
        if (query.EventPeriodId is { } periodId) records = records.Where(x => x.EventPeriodId == periodId);
        if (query.PeriodKey is { } periodKey) records = records.Where(x => x.PeriodKey == periodKey);
        if (query.JobExecutionId is { } executionId) records = records.Where(x => x.JobExecutionId == executionId);
        if (query.FromUtc is { } from) records = records.Where(x => x.OccurredAt >= from);
        if (query.ToUtc is { } to) records = records.Where(x => x.OccurredAt < to);
        if (query.ActivityType is { } type) records = records.Where(x => x.ActivityType == type);
        return await records.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
    }

    public Task<PlayerActivity?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.PlayerActivities.AsNoTracking().SingleOrDefaultAsync(x => x.ActivityId == id, ct);
}
