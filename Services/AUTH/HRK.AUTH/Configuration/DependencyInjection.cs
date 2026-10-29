using AUTH.Application.Interfaces;
using AUTH.Infrastructure.Configuration;
using AUTH.Infrastructure.Data;
using AUTH.Infrastructure.Identity;
using AUTH.Infrastructure.Services;
using Core.Persistence.Database.Extensions;
using Core.Persistence.Database.Options;
using Core.AUTH.DependencyInjection;
using Core.Web.DependencyInjection;
using Core.Persistence.DependencyInjection;
using Core.Application.DependencyInjection;
using Core.RabbitMQ.DependencyInjection;
using Core.RabbitMQ.Interfaces;
using Oservability;
using Core.Localization;
using Core.HealthChecks;
using HRK.AUTH.HealthChecks.Dependencies;
using Core.TransactionalMessaging.DependencyInjection;
using Core.Persistence.Repositories;
namespace HRK.AUTH.Configuration
{
    public static class DependencyInjection
    {
        public static WebApplicationBuilder AddCustomDependency(this WebApplicationBuilder builder)
        {
            AddObservability(builder);
            AddCustomDependency(builder.Services, builder.Configuration);
            return builder;
        }

        public static void AddObservability(WebApplicationBuilder builder) =>
            builder.AddHrkObservability("HRK.AUTH");

        public static IServiceCollection AddCustomDependency(IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthInfrastructure(configuration);

            services.AddHttpContextAccessor();
            // add DbContexts
            AddDbContexts(services, configuration);



            AddServices(services, configuration);

       //     AddRabbitMq(services, configuration);

           

            services.AddCoreApplication(typeof(IIdentityService).Assembly);



            // add services 



            return services;
        }


        public static void AddRabbitMq(IServiceCollection services, IConfiguration configuration)
        {
            //services
            //.AddRabbitMqMessaging(configuration)
            //.EnsureRabbitTopology();

            // 2) Consumer setup (message + handler)
            services.AddScoped<DemoMessageHandler>();
            services.AddRabbitConsumer<DemoMessage, DemoMessageHandler>(
                queue: configuration["RabbitMq:Consumer:Queue"]!);

           
        }   
        public static void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHrkInternalApi(configuration);
            services.AddHrkLocalization(configuration, "auth");
            AddServiceHealthChecks(services, configuration);
            services.AddCoreJwt(configuration);
            services.AddCoreCookies(configuration);

            services.AddRepositoryUOW<ApplicationDbContext>();
            services.AddScoped<IUnitOfWork<AuthDbContext>, UnitOfWork<AuthDbContext>>();
            AddMessagingServices(services, configuration);

            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IApplicationRouteConfigService, ApplicationRouteConfigService>();
        }

        public static void AddDbContexts(IServiceCollection services, IConfiguration configuration)
        {
            //services.AddDbContext<AuthDbContext>(options =>
            //options.UseSqlServer(configuration.GetConnectionString(Constants.CORE_CONSTANTS.DefaultConnection)));


            //services.AddDbContext<ApplicationDbContext>(options =>
            //  options.UseSqlServer(configuration.GetConnectionString(Constants.CORE_CONSTANTS.DefaultConnection)));

            services.Configure<DatabaseOptions>(
    configuration.GetSection("Database"));

            services.AddDatabase<AuthDbContext>(configuration);

            services.AddDatabase<ApplicationDbContext>(configuration);



        }

        private static void AddMessagingServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddRabbitMqMessaging(configuration).EnsureRabbitTopology();
            services.AddTransactionalMessaging<AuthDbContext>(configuration);
        }

        public static void AddServiceHealthChecks(IServiceCollection services, IConfiguration configuration)
        {
            var checks = services.AddHrkHealthChecks("HRK.AUTH", configuration)
                .AddDependency<AuthDatabaseHealthCheck>("database")
                .AddDependency<AuthRabbitMqHealthCheck>("rabbitmq");
            if (configuration.GetValue("Localization:Enabled", true))
                checks.AddDependency<AuthLocalizationRedisHealthCheck>("redis.localization");
        }


        //public static void IdentityConfiguration(IServiceCollection services)
        //{
        //    services.AddIdentity<ApplicationUser, IdentityRole>()
        //        .AddEntityFrameworkStores<AuthDbContext>()
        //        .AddDefaultTokenProviders();



        //}

        public sealed record DemoMessage(Guid Id, string Name, DateTime CreatedAtUtc);

        public sealed class DemoMessageHandler : IMessageHandler<DemoMessage>
        {
            private readonly ILogger<DemoMessageHandler> _log;
            public DemoMessageHandler(ILogger<DemoMessageHandler> log) => _log = log;

            public Task HandleAsync(DemoMessage message, CancellationToken ct)
            {
                _log.LogInformation("Handled DemoMessage {Id} - {Name} at {At}", message.Id, message.Name, message.CreatedAtUtc);
                // do work...
                return Task.CompletedTask;
            }
        }
    }
}
