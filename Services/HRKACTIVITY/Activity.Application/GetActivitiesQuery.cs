using Activity.Domain;
using Core.Messaging.Contracts;
using System.Text.Json;

namespace Activity.Application;

public sealed record GetActivitiesQuery(long? PlayerId = null, string? CorrelationId = null,
    string? EntityType = null, string? EntityId = null, int Page = 1, int PageSize = 50,
    int? GameEventId = null, long? EventPeriodId = null, string? PeriodKey = null,
    Guid? JobExecutionId = null, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null,
    string? ActivityType = null)
{
    public bool IsValid => Page is >= 1 and <= 100000 && PageSize is >= 1 and <= 100 &&
        GameEventId is not <= 0 && EventPeriodId is not <= 0 && PlayerId is not <= 0 &&
        JobExecutionId != Guid.Empty && (PeriodKey is null || PeriodKey.Length <= 50) &&
        (ActivityType is null || ActivityType.Length <= 100) &&
        (!FromUtc.HasValue || !ToUtc.HasValue || FromUtc < ToUtc);
}
