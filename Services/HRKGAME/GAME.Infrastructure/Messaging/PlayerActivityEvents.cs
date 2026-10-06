using Core.Messaging.Contracts;
using Core.TransactionalMessaging.Outbox;
using Core.TransactionalMessaging.Entities;
using GAME.Application.Events;
using GAME.Application.Interfaces;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Messaging;

public sealed class PlayerActivityEvents(IOutboxWriter outbox, PlayerActivityEventMapper mapper,
    GameDbContext db) : IPlayerActivityEvents
{
    private readonly Dictionary<Guid, (OutboxMessage Message, Guid? Transaction)> _staged = new();

    public void Raise(PlayerActivityEvent activity)
    {
        var mapped = mapper.Map(activity);
        var transaction = db.Database.CurrentTransaction?.TransactionId;
        if (_staged.TryGetValue(mapped.Message.EventId, out var previous))
        {
            var entry = db.Entry(previous.Message);
            if (previous.Transaction == transaction && entry.State != EntityState.Detached)
                return; // Multiple SaveChanges/event raises within the same attempt.
            // A rollback may leave an accepted/Added row in the tracker. Re-enlist it
            // in the new transaction instead of attaching two instances of the same ID.
            entry.State = EntityState.Detached;
        }
        var row = outbox.Add(mapped.Message, ActivityContractNames.PlayerActivityRecordedV1,
            MessagingPublisherNames.GameEvents, ActivityContractNames.PlayerActivityRecordedV1,
            partitionKey: mapped.PartitionKey, sequence: mapped.Sequence);
        _staged[mapped.Message.EventId] = (row, transaction);
    }
}
