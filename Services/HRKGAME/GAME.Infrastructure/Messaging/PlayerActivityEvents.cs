using Core.Messaging.Contracts;
using Core.TransactionalMessaging.Outbox;
using Core.TransactionalMessaging.Entities;
using GAME.Application.Events;
using GAME.Application.Interfaces;
using Core.Persistence.Repositories;

namespace GAME.Infrastructure.Messaging;

public sealed class PlayerActivityEvents(IOutboxWriter outbox, PlayerActivityEventMapper mapper,
    IUnitOfWork unitOfWork) : IPlayerActivityEvents
{
    private readonly Dictionary<Guid, (OutboxMessage Message, Guid? Transaction)> _staged = new();

    public void Raise(PlayerActivityEvent activity)
    {
        var mapped = mapper.Map(activity);
        var transaction = unitOfWork.CurrentTransactionId;
        if (_staged.TryGetValue(mapped.Message.EventId, out var previous))
        {
            if (previous.Transaction == transaction && unitOfWork.IsTracked(previous.Message))
                return; // Multiple SaveChanges/event raises within the same attempt.
            // A rollback may leave an accepted/Added row in the tracker. Re-enlist it
            // in the new transaction instead of attaching two instances of the same ID.
            unitOfWork.Detach(previous.Message);
        }
        var row = outbox.Add(mapped.Message, ActivityContractNames.PlayerActivityRecordedV1,
            MessagingPublisherNames.GameEvents, ActivityContractNames.PlayerActivityRecordedV1,
            partitionKey: mapped.PartitionKey, sequence: mapped.Sequence);
        _staged[mapped.Message.EventId] = (row, transaction);
    }
}
