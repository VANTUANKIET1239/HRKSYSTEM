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
