using MediatR;

namespace GAME.Domain.Tests;

internal sealed class PipelineTestHandler<TRequest, TResponse>(Func<TRequest, CancellationToken, Task<TResponse>> handle)
    : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, CancellationToken ct) => handle(request, ct);
}
