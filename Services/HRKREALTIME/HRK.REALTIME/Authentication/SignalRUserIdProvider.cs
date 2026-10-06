using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace HRK.REALTIME.Authentication;

public sealed class SignalRUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
