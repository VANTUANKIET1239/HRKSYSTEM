using Core.Messaging.Contracts;
using Core.RabbitMQ.Interfaces;
using GAME.Application.Interfaces;

namespace GAME.Infrastructure.Messaging.Consumers;

public sealed class QuickClimbFloorRequestedHandler(ITowerQuickClimbService service)
    : IMessageHandler<ProcessQuickClimbFloorRequestedV1>
{
    private readonly string _workerId = $"rabbit-{Environment.MachineName}-{Guid.NewGuid():N}";

    public Task HandleAsync(ProcessQuickClimbFloorRequestedV1 message, CancellationToken ct = default) =>
        service.ProcessJobFloorMessageAsync(
            message.EventId,
            message.JobId,
            message.UserId,
            message.ExpectedFloor,
            message.Version,
            _workerId,
            ct);
}

public sealed class QuickClimbTransientMessageException(string message, Exception innerException)
    : Exception(message, innerException), ITransientMessageException;
