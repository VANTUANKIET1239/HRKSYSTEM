using Core.TransactionalMessaging.Configuration;
using Core.TransactionalMessaging.Inbox;
using Core.TransactionalMessaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.TransactionalMessaging.DependencyInjection;

public static class TransactionalMessagingServiceCollectionExtensions
{
    public static IServiceCollection AddTransactionalMessaging<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TDbContext : DbContext
    {
        services.Configure<TransactionalMessagingOptions>(
            configuration.GetSection(TransactionalMessagingOptions.SectionName));

        services.AddScoped<IOutboxWriter, EfOutboxWriter<TDbContext>>();
        services.AddScoped<IOutboxStore, SqlServerOutboxStore<TDbContext>>();
        services.AddScoped<IInboxStore, SqlServerInboxStore<TDbContext>>();
        services.AddScoped<IInboxExecutor, InboxExecutor<TDbContext>>();
        services.AddHostedService<OutboxBackgroundService>();
        services.AddHostedService<InboxCleanupBackgroundService>();

        return services;
    }
}
