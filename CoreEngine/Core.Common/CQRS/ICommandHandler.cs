namespace CoreEngine.CQRS
{
    public interface ICommandHandler : IRequestHandler<ICommand, Unit>
    {
    }


    public interface ICommandHandler<in TRequest,TResponse> : IRequestHandler<TRequest, TResponse>
        where TRequest : ICommand<TResponse>
        where TResponse : notnull
    {
    }
}
