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
        if (query.Page is < 1 or > 100000 || query.PageSize is < 1 or > 100)
            throw new ArgumentException("Page must be 1-100000 and PageSize 1-100.");
        var records = db.PlayerActivities.AsNoTracking();
        if (query.PlayerId is { } playerId) records = records.Where(x => x.PlayerId == playerId);
        if (query.CorrelationId is { } correlationId) records = records.Where(x => x.CorrelationId == correlationId);
        if (query.EntityType is { } entityType) records = records.Where(x => x.EntityType == entityType);
        if (query.EntityId is { } entityId) records = records.Where(x => x.EntityId == entityId);
        return await records.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
    }

    public Task<PlayerActivity?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.PlayerActivities.AsNoTracking().SingleOrDefaultAsync(x => x.ActivityId == id, ct);
}
