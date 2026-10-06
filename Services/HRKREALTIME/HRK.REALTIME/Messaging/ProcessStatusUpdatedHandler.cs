using Core.Messaging.Contracts;
using Core.RabbitMQ.Interfaces;
using HRK.REALTIME.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HRK.REALTIME.Messaging;

public sealed class ProcessStatusUpdatedHandler(IHubContext<RealtimeHub> hubContext,
    ILogger<ProcessStatusUpdatedHandler> logger)
    : IMessageHandler<ProcessStatusUpdatedV1>
{
    public async Task HandleAsync(ProcessStatusUpdatedV1 message, CancellationToken ct = default)
    {
        await hubContext.Clients
            .User(message.UserId)
            .SendAsync("process-status-updated", message, ct);
        logger.LogDebug("RealtimeStatusSent: JobId={JobId}, Version={Version}, Status={Status}",
            message.JobId, message.Version, message.Status);
    }
}
