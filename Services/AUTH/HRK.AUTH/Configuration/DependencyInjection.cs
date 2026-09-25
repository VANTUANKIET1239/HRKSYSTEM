using AUTH.Application.Interfaces;
using AUTH.Infrastructure.Configuration;
using AUTH.Infrastructure.Data;
using AUTH.Infrastructure.Identity;
using AUTH.Infrastructure.Services;
using Core.Common.Common;
using Core.Common.Constants.Common;
using Core.Common.Database.Extensions;
using Core.Common.Database.Options;
using Core.Common.Entity;
using Core.Common.Extensions;
using Core.Common.JwtHandler.Entities;
using Core.RabbitMQ.DependencyInjection;
using Core.RabbitMQ.Interfaces;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection;
namespace HRK.AUTH.Configuration
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddCustomDependency(IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthInfrastructure(configuration);

            services.AddHttpContextAccessor();
            // add DbContexts
            AddDbContexts(services, configuration);


            AddOptions(services, configuration);

            AddServices(services, configuration);

       //     AddRabbitMq(services, configuration);

           

            services.AddValidatorsFromAssembly(typeof(IIdentityService).Assembly);
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(typeof(IIdentityService).Assembly);
            });



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
            services.AddCoreService(configuration);

            services.AddRepositoryUOW<ApplicationDbContext>();

            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IApplicationRouteConfigService, ApplicationRouteConfigService>();
        }

        public static void AddOptions(IServiceCollection services, IConfiguration configuration)
        {

            services.Configure<HRKCookieOptions>(configuration.GetSection(Constants.Cookie.COOKIE_OPTONS));
            services.AddOptions(configuration);

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

            services.AddScoped<IDbConnection>(sp =>
            new SqlConnection(configuration.GetConnectionString(Constants.CORE_CONSTANTS.DefaultConnection)));


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
