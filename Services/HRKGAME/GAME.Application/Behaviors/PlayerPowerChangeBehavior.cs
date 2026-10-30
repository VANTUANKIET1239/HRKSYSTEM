using GAME.Application.Interfaces;
using MediatR;

namespace GAME.Application.Behaviors;

/// <summary>
/// Runs marked commands inside the existing player lock/transaction and records power
/// before commit. Commands owning their own transactions must not use this marker.
/// </summary>
public sealed class PlayerPowerChangeBehavior<TRequest, TResponse>(
    IPlayerRankingWriter rankings, IGameRequestContext actor) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IPlayerPowerChangeCommand
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var userId = request.UserId ?? actor.UserId;
        if (string.IsNullOrWhiteSpace(userId)) throw new UnauthorizedAccessException("A player identity is required.");
        return rankings.ExecutePowerChangeAsync(userId, () => next(), ct);
    }
}
