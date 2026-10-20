using Core.Messaging.Contracts;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.Interfaces;
using GAME.Application.Events;
using GAME.Domain.Entities;

namespace GAME.Application.Features.Shared.QuickClimb;

public abstract class QuickClimbHandlerSupport
{
    protected readonly IOutboxWriter _outbox;
    protected readonly IPlayerActivityEvents _activityEvents;
    protected QuickClimbHandlerSupport(IOutboxWriter outbox, IPlayerActivityEvents activityEvents)
    {
        this._outbox = outbox;
        this._activityEvents = activityEvents;
    }

    protected void EnqueueFloorCommand(HrkTowerQuickClimbJob job, string userId)
    {
        var command = new ProcessQuickClimbFloorRequestedV1(Guid.NewGuid(), job.JobId, userId, job.CurrentFloor, job.Version);
        _outbox.Add(command, MessagingContractNames.QuickClimbFloorRequestedV1, MessagingPublisherNames.GameEvents, MessagingContractNames.QuickClimbFloorRequestedV1, partitionKey: job.JobId, sequence: job.Version);
    }

    protected void EnqueueStatus(HrkTowerQuickClimbJob job, string userId, string? errorCode = null, string? errorMessage = null)
    {
        var total = Math.Max(1, job.TargetFloor - job.StartFloor + 1);
        var percentage = Math.Clamp((int)Math.Round(job.ClearedFloorsCount * 100d / total), 0, 100);
        if (job.Status == "COMPLETED")
        {
            percentage = 100;
        }

        var statusEvent = new ProcessStatusUpdatedV1(Guid.NewGuid(), job.JobId, userId, "TOWER_QUICK_CLIMB", job.Version, job.CurrentFloor, job.TargetFloor, percentage, job.Status, DateTime.UtcNow, errorCode, errorMessage is null ? null : errorMessage[..Math.Min(errorMessage.Length, 500)]);
        _outbox.Add(statusEvent, MessagingContractNames.ProcessStatusUpdatedV1, MessagingPublisherNames.GameEvents, MessagingContractNames.ProcessStatusUpdatedV1, partitionKey: job.JobId, sequence: job.Version);
        if (job.Status is not ("QUEUED" or "PROCESSING"))
            _activityEvents.Raise(new QuickClimbEndedEvent(job.PlayerId, userId, job.JobId, job.Status, job.StartFloor, job.CurrentFloor, job.TargetFloor, job.ClearedFloorsCount, job.StopReason, job.Version)
            {
                GameEventId = job.EventPeriod?.EventId, EventPeriodId = job.EventPeriodId,
                PeriodKey = job.EventPeriod?.PeriodKey, OccurredAt = job.CompletedOnUtc is { } completed ?
                    new DateTimeOffset(DateTime.SpecifyKind(completed, DateTimeKind.Utc)) : null
            });
    }
}
