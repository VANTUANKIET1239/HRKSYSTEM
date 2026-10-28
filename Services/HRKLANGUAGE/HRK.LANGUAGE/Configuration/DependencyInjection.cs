using Core.Localization;
using Core.Persistence.DependencyInjection;
using Core.TransactionalMessaging.Configuration;
using Core.TransactionalMessaging.Outbox;
using Core.Web.Compression;
using Language.Application;
using Language.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Oservability;
using Core.HealthChecks;
using HRK.LANGUAGE.HealthChecks.Dependencies;

namespace HRK.LANGUAGE.Configuration;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddCustomDependency(this WebApplicationBuilder builder)
    {
        builder.AddHrkObservability("HRK.LANGUAGE");
        AddPersistence(builder.Services, builder.Configuration);
        AddLanguageServices(builder.Services, builder.Configuration);
        AddHttpServices(builder.Services, builder.Configuration);
        AddServiceHealthChecks(builder.Services, builder.Configuration);
        return builder;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration["Database:ConnectionString"]
            ?? throw new InvalidOperationException("Database:ConnectionString is required.");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Database:ConnectionString is required.");
        services.AddDbContext<LanguageDbContext>(options => options.UseSqlServer(connection, sql => sql.EnableRetryOnFailure()));
        services.AddRepositoryUOW<LanguageDbContext>();
        services.Configure<TransactionalMessagingOptions>(configuration.GetSection("TransactionalMessaging"));
        services.AddScoped<IOutboxStore, SqlServerOutboxStore<LanguageDbContext>>();
        services.AddHostedService<LanguageCacheOutboxWorker>();
    }

    private static void AddServiceHealthChecks(IServiceCollection services, IConfiguration configuration) =>
        services.AddHrkHealthChecks("HRK.LANGUAGE", configuration)
            .AddDependency<LanguageDatabaseHealthCheck>("database")
            .AddDependency<LanguageLocalizationRedisHealthCheck>("redis");

    private static void AddLanguageServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHrkInternalApi(configuration);
        services.AddHrkLocalization(configuration);
        services.AddScoped<IPublishedLanguageReader, PublishedLanguageReader>();
    }

    private static void AddHttpServices(IServiceCollection services, IConfiguration configuration)
    {
        // Bundle endpoints deliberately return raw resources, outside API envelope filters.
        services.AddControllers();
        services.AddHrkResponseCompression(configuration);
        services.AddProblemDetails();
    }
}
