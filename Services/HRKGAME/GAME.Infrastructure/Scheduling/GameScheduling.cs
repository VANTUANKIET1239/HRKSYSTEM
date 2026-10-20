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

public static class DailyEventSchedule
{
    public static string ToCron(TimeSpan time)
    {
        if (time < TimeSpan.Zero || time >= TimeSpan.FromDays(1) || time.Ticks % TimeSpan.TicksPerSecond != 0)
            throw new ArgumentException("Reset time must be a whole second within one day.");
        return FormattableString.Invariant($"{time.Seconds} {time.Minutes} {time.Hours} * * ?");
    }

    public static TimeSpan Parse(string cron)
    {
        var parts = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 6 || parts[3] != "*" || parts[4] != "*" || parts[5] != "?" ||
            !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var second) || second is < 0 or > 59 ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minute) || minute is < 0 or > 59 ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var hour) || hour is < 0 or > 23)
            throw new ArgumentException("Daily event reset requires 'second minute hour * * ?'.");
        return new(hour, minute, second);
    }
}

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

public static class GameSchedulingRegistration
{
    public static IServiceCollection AddGameScheduling(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IEventResetSchedule, EventResetSchedule>();
        AddJobHandlers(services);
        services.AddManagedScheduling(options =>
        {
            configuration.GetSection("Scheduling").Bind(options);
            options.SchedulerName = "HRK.GAME";
            options.Group = "game";
            options.ConnectionString ??= configuration["Database:ConnectionString"];
        });
        return services;
    }

    private static void AddJobHandlers(IServiceCollection services)
    {
        services.AddScoped<IScheduledJobHandler, EventResetJobHandler>();
    }
}
