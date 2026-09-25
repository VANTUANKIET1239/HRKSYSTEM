using Core.Common.Database.Extensions;
using Core.Common.Database.Options;
using Core.Common.Extensions;
using Core.Common.Caching;
using GAME.Application.Configuration;
using GAME.Application.Interfaces;
using GAME.Domain.Interfaces;
using GAME.Domain.Services;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Random;
using GAME.Infrastructure.Services;
using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Skills.QaKyTinh;
using GAME.Domain.Battle.Skills.HaiLastSmile;
using GAME.Domain.Battle.Skills.ChuanMen;
using GAME.Domain.Battle.Reactions;
using GAME.Domain.Battle.Targets;
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
            services.AddHrkRedisCache(configuration);

            // Register UnitOfWork for GameDbContext
            services.AddRepositoryUOW<GameDbContext>();

            // Register Application Services
            services.AddSingleton<IRandomService, DefaultRandomService>();
            services.AddScoped<IEquipmentInstanceFactory, EquipmentInstanceFactory>();
            services.AddScoped<IFormationPowerQueryService, FormationPowerQueryService>();
            services.AddScoped<IFormationSnapshotService, FormationSnapshotService>();
            services.AddScoped<IMetadataService, MetadataService>();
            services.AddScoped<ICatalogService, CatalogService>();
            services.AddScoped<IItemStatCalculationService, ItemStatCalculationService>();
            services.AddScoped<IGamePlayerService, GamePlayerService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IFormationStatService, FormationStatService>();
            services.AddScoped<IFormationService, FormationService>();
            services.AddScoped<IBattleService, BattleService>();
            services.AddScoped<IDungeonStarService, DungeonStarService>();
            services.AddScoped<IDungeonRewardService, DungeonRewardService>();
            services.AddScoped<IDungeonEquipmentRewardPolicy, DungeonEquipmentRewardPolicy>();
            services.AddScoped<IDungeonChestService, DungeonChestService>();
            services.AddScoped<IDungeonService, DungeonService>();
            services.AddSingleton<IBattleEffectHandler, DamageEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, HealEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, StatBuffEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, StatDebuffEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, StunEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, ShieldEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, MarkEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, PositionSwapEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, SilenceEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, DamageReductionEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, TauntEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, DamageReflectionEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, BleedEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, PanicEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, BleedDetonateEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, EnergyChangeEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, RicardoEffectHandler>();
            services.AddSingleton<IBattleStatusReactionHandler, RicardoStatusReactionHandler>();
            services.AddSingleton<BattleStatusReactionHandlerRegistry>();
            services.AddSingleton<BattleEffectHandlerRegistry>();
            services.AddSingleton<IBattleTargetSelector, SelfTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, AllyAllTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, AllyRandomTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, AllyRandom2TargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemySingleTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyAllTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyRandomTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyRandom4TargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyFrontRowTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyBackRowTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemySameLaneBackRowTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, LowestHpPercentTargetSelector>();
            services.AddSingleton<BattleTargetSelectorRegistry>();
            services.AddSingleton<DefaultSkillHandler>();
            services.AddSingleton<ISkillHandler, FatalAllInSkillHandler>();
            services.AddSingleton<ISkillHandler, HaiLastSmileSkillHandler>();
            services.AddSingleton<ISkillHandler, ChuanMenSkillHandler>();
            services.AddSingleton<SkillHandlerRegistry>();
            services.AddSingleton<IBattleSimulationEngine, BattleSimulationEngine>();
            services.AddScoped<IHeroStatCalculationService, HeroStatCalculationService>();
            services.AddScoped<IHeroEquipmentService, HeroEquipmentService>();
            services.AddScoped<IGameFeatureConfigService, GameFeatureConfigService>();
            services.AddScoped<ICombatPowerService, CombatPowerService>();
            services.AddScoped<IHeroUpgradeService, HeroUpgradeService>();

            // Domain Services & Infrastructure abstractions (DDD)
            services.AddSingleton<IEnhancementRoller, CryptoEnhancementRoller>();
            services.AddScoped<IEquipmentEnhancementDomainService, EquipmentEnhancementDomainService>();
        }

        public static void AddOptions(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions(configuration);
            services.Configure<InventorySettings>(configuration.GetSection(InventorySettings.SectionName));
        }
    }
}
