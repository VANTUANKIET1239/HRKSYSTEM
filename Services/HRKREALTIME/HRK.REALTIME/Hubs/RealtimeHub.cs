using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HRK.REALTIME.Hubs;

[Authorize]
public sealed class RealtimeHub(ILogger<RealtimeHub> logger) : Hub
{
    public override Task OnConnectedAsync()
    {
        logger.LogInformation(
            "SignalR client connected. ConnectionId={ConnectionId}, UserId={UserId}",
            Context.ConnectionId,
            Context.UserIdentifier);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation(
            "SignalR client disconnected. ConnectionId={ConnectionId}, UserId={UserId}",
            Context.ConnectionId,
            Context.UserIdentifier);
        return base.OnDisconnectedAsync(exception);
    }
}
