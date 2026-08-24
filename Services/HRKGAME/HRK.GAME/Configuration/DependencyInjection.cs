using Core.Common.Database.Extensions;
using Core.Common.Database.Options;
using Core.Common.Extensions;
using GAME.Application.Interfaces;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HRK.GAME.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCustomDependency(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();

            // 1. Add DbContexts & Database
            AddDbContexts(services, configuration);

            // 2. Add Core Options
            AddOptions(services, configuration);

            // 3. Add Services & Repositories / UnitOfWork
            AddServices(services, configuration);

            // 4. Register MediatR for GAME.Application assembly
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(IMetadataService).Assembly);
            });

            return services;
        }

        public static void AddDbContexts(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
            services.AddDatabase<GameDbContext>(configuration);

            services.AddScoped<IDbConnection>(sp =>
                new SqlConnection(configuration.GetConnectionString(Core.Common.Constants.Common.Constants.CORE_CONSTANTS.DefaultConnection) 
                                 ?? configuration["Database:ConnectionString"]));
        }

        public static void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddCoreService(configuration);

            // Register UnitOfWork for GameDbContext
            services.AddRepositoryUOW<GameDbContext>();

            // Register Application Services
            services.AddScoped<IMetadataService, MetadataService>();
            services.AddScoped<ICatalogService, CatalogService>();
            services.AddScoped<IGamePlayerService, GamePlayerService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IFormationService, FormationService>();
            services.AddScoped<IBattleService, BattleService>();
        }

        public static void AddOptions(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions(configuration);
        }
    }
}
