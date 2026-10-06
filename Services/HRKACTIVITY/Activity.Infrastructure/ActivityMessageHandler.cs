using Activity.Application;
using Core.Messaging.Contracts;
using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Microsoft.Data.SqlClient;

namespace Activity.Infrastructure;

public sealed class ActivityMessageHandler(IActivityRecorder recorder, MessageContext context)
    : IMessageHandler<PlayerActivityRecordedV1>
{
    public async Task HandleAsync(PlayerActivityRecordedV1 message, CancellationToken ct = default)
    {
        try
        {
            await recorder.RecordAsync(new(message, context.MessageId ?? "", context.CorrelationId, context.TraceId), ct);
        }
        catch (SqlException exception)
        {
            // SQL transport/deadlock/timeout is retryable; validation/constraint errors are not.
            if (exception.Number is -2 or 64 or 233 or 1205 or 4060 or 40197 or 40501 or 40613 or 10053 or 10054 or 10060)
                throw new ActivityPersistenceException(exception);
            throw;
        }
    }

    private sealed class ActivityPersistenceException(Exception inner)
        : Exception("Activity persistence is temporarily unavailable.", inner), ITransientMessageException;
}
