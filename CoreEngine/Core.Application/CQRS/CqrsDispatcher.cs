using MediatR;

namespace Core.Application.CQRS;

internal sealed class CqrsDispatcher(ISender sender) : ICqrsDispatcher
{
    public Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        return sender.Send(query, cancellationToken);
    }

    public Task<TResponse> CommandAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        return sender.Send(command, cancellationToken);
    }
}
