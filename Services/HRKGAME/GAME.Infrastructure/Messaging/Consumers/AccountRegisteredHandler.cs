using Core.Application.CQRS;
using Core.Messaging.Contracts;
using Core.RabbitMQ.Interfaces;
using GAME.Application.Features.Commands.Player;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Messaging.Consumers;

public sealed class AccountRegisteredHandler(ICqrsDispatcher dispatcher) : IMessageHandler<AccountRegisteredV1>
{
    public async Task HandleAsync(AccountRegisteredV1 message, CancellationToken ct)
    {
        try { await dispatcher.CommandAsync(new InitializePlayerCommand(message), ct); }
        catch (Exception exception) when (exception is SqlException or DbUpdateException or TimeoutException)
        {
            throw new PlayerInitializationTransientException(exception);
        }
    }
}
