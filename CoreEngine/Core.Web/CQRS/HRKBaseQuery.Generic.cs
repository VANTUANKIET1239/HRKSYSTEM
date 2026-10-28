using Core.Application.CQRS;
using Microsoft.AspNetCore.Http;

namespace Core.Web.CQRS;

/// <summary>Typed query handler using the shared HRK HTTP context and response helpers.</summary>
public abstract class HRKBaseQuery<TQuery, TResponse>(IHttpContextAccessor httpContextAccessor)
    : HRKBaseQuery(httpContextAccessor), IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : notnull
{
    public abstract Task<TResponse> Handle(TQuery request, CancellationToken cancellationToken);
}
