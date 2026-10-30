namespace GAME.Application.Interfaces;

public interface IPlayerRankingWriter
{
    Task RefreshPowerAsync(long playerId, CancellationToken ct = default);
    Task<T> ExecutePowerChangeAsync<T>(string userId, Func<Task<T>> action, CancellationToken ct = default);
}
