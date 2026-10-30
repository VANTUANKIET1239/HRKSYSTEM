using Core.Messaging.Contracts;
using Core.RabbitMQ.Interfaces;
using GAME.Application.Common;
using GAME.Application.Features.Commands.QuickClimb;
using MediatR;

namespace GAME.Infrastructure.Messaging.Consumers;

public sealed class QuickClimbFloorRequestedHandler(IMediator mediator) : IMessageHandler<ProcessQuickClimbFloorRequestedV1>
{
    private readonly string _workerId = $"rabbit-{Environment.MachineName}-{Guid.NewGuid():N}";
    public async Task HandleAsync(ProcessQuickClimbFloorRequestedV1 message, CancellationToken ct = default)
    {
        try
        {
            await mediator.Send(new ProcessQuickClimbFloorCommand(message.EventId, message.JobId, message.UserId, message.ExpectedFloor, message.Version, _workerId), ct);
        }
        catch (QuickClimbTransientException exception)
        {
            throw new QuickClimbTransientMessageException(exception.Message, exception);
        }
    }
}
