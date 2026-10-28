using Core.Application.CQRS;
using Microsoft.AspNetCore.Http;

namespace Core.Web.CQRS;

/// <summary>Typed command handler using the shared HRK HTTP context and response helpers.</summary>
public abstract class HRKBaseCommand<TCommand, TResponse>(IHttpContextAccessor httpContextAccessor)
    : HRKBaseCommand(httpContextAccessor), ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : notnull
{
    public abstract Task<TResponse> Handle(TCommand request, CancellationToken cancellationToken);
}
