using GAME.Domain.Entities;

namespace GAME.Application.Interfaces;

public interface IEventResetSchedule
{
    Task<(string TimeZoneId, TimeSpan ResetTime)> GetAsync(HrkGameEvent gameEvent, CancellationToken ct);
}
