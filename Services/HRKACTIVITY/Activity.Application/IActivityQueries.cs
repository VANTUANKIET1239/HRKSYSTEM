using Activity.Domain;
using Core.Messaging.Contracts;
using System.Text.Json;

namespace Activity.Application;

public interface IActivityQueries
{
    Task<IReadOnlyList<PlayerActivity>> GetAsync(GetActivitiesQuery query, CancellationToken ct);
    Task<PlayerActivity?> GetByIdAsync(Guid activityId, CancellationToken ct);
}
