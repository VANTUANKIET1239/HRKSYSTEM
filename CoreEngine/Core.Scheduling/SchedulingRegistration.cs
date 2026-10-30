using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Core.Scheduling;

public static class SchedulingRegistration
{
    public static IServiceCollection AddManagedScheduling(this IServiceCollection services,
        Action<SchedulingOptions> configure)
    {
        var options = new SchedulingOptions();
        configure(options);
        if (string.IsNullOrWhiteSpace(options.SchedulerName) || options.SchedulerName.Length > 120 ||
            string.IsNullOrWhiteSpace(options.Group) || options.Group.Length > 100 ||
            options.MaxConcurrency is < 1 or > 100 || options.SynchronizationIntervalSeconds is < 5 or > 3600)
            throw new ArgumentException("Invalid scheduler options.");
        services.AddSingleton(options);
        services.AddScoped<ISchedulingStore, UnitOfWorkSchedulingStore>();
        if (!options.Enabled) return services;
        services.AddSingleton<JobExecutionRunner>();
        services.AddQuartz(q =>
        {
            q.SchedulerName = options.SchedulerName;
            q.SchedulerId = "AUTO";
            q.UseDefaultThreadPool(pool => pool.MaxConcurrency = options.MaxConcurrency);
            if (options.UsePersistentStore)
            {
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                    throw new ArgumentException("A SQL Server connection is required for persistent scheduling.");
                q.UsePersistentStore(store =>
                {
                    store.UseProperties = true;
                    store.UseSqlServer(sql => sql.ConnectionString = options.ConnectionString);
                    store.UseSystemTextJsonSerializer();
                    store.UseClustering();
                });
            }
            else q.UseInMemoryStore();
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        services.AddSingleton<IJobScheduleManager, QuartzScheduleManager>();
        services.AddHostedService<ScheduleSynchronizationWorker>();
        return services;
    }
}
