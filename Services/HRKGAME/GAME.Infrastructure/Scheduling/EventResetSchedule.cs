using System.Globalization;
using Core.Persistence.Repositories;
using Core.Scheduling;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GAME.Infrastructure.Scheduling;

public sealed class EventResetSchedule(IUnitOfWork unitOfWork) : IEventResetSchedule
{
    public async Task<(string TimeZoneId, TimeSpan ResetTime)> GetAsync(HrkGameEvent gameEvent, CancellationToken ct)
    {
        var scope = gameEvent.Id.ToString(CultureInfo.InvariantCulture);
        var definition = await unitOfWork.ReadOnlyRepository<JobDefinition>().Query()
            .SingleOrDefaultAsync(x => x.HandlerKey == EventResetJobHandler.Key && x.ScopeKey == scope, ct);
        // Disabling a job stops background processing, not the logical progression of time.
        if (definition is null) return (gameEvent.TimeZoneId, gameEvent.ResetTime);
        return (definition.TimeZoneId, DailyEventSchedule.Parse(definition.CronExpression));
    }
}
