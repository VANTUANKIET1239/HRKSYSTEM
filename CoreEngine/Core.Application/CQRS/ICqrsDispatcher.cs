namespace Core.Application.CQRS;

/// <summary>Dispatches HRK requests through the registered CQRS and validation pipeline.</summary>
public interface ICqrsDispatcher
{
    Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        where TResponse : notnull;
    Task<TResponse> CommandAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
        where TResponse : notnull;
}
