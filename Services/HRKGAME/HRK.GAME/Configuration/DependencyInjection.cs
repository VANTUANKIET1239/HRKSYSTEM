using Core.Common.Database.Extensions;
using Core.Common.Database.Options;
using Core.Common.Extensions;
using Core.Common.Caching;
using Core.RabbitMQ.DependencyInjection;
using Core.Messaging.Contracts;
using Core.TransactionalMessaging.DependencyInjection;
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
using GAME.Domain.Battle.Skills.ThanhThaiAura;
using GAME.Domain.Battle.Skills.NghiaPhucPrime;
using GAME.Domain.Battle.Skills.SibaThienThan;
using GAME.Domain.Battle.Skills.KietMaiXeo;
using GAME.Domain.Battle.Skills.TruongKietGraduation;
using GAME.Domain.Battle.Skills.QuocNhanGraduation;
using GAME.Domain.Battle.Skills.LongLeCat;
using GAME.Domain.Battle.Skills.QuocNhanRunNow;
using GAME.Domain.Battle.Reactions;
using GAME.Domain.Battle.Targets;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using GAME.Infrastructure.Messaging.Consumers;
using Oservability;
using Oservability.Tracing;
using HRK.GAME.Health;

namespace HRK.GAME.Configuration
{
    public static class DependencyInjection
    {
        public static WebApplicationBuilder AddCustomDependency(this WebApplicationBuilder builder)
        {
            AddObservability(builder);
            builder.Services.AddCustomDependency(builder.Configuration);
            return builder;
        }

        public static void AddObservability(WebApplicationBuilder builder) =>
            builder.AddHrkObservability("HRK.GAME");

        public static void AddRedisTelemetry(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHrkRedisTelemetry();
            services.AddOptions<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions>()
                .Configure<RedisTelemetry>((options, telemetry) =>
                    options.ConnectionMultiplexerFactory = () => telemetry.ConnectAsync(
                        configuration["Redis:ConnectionString"] ?? "localhost:6379,abortConnect=false"));
        }

        public static void AddServiceHealthChecks(IServiceCollection services) =>
            services.AddHealthChecks()
                .AddCheck<GameDatabaseHealthCheck>("database")
                .AddCheck<GameRabbitMqHealthCheck>("rabbitmq");

        public static IServiceCollection AddCustomDependency(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();

            // 1. Add DbContexts & Database
            AddDbContexts(services, configuration);

            // 2. Add Core Options
            AddOptions(services, configuration);

            // 3. Add Services & Repositories / UnitOfWork
            AddServices(services, configuration);
            AddRedisTelemetry(services, configuration);
            AddServiceHealthChecks(services);

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
            services.AddSingleton<global::GAME.Infrastructure.Messaging.PlayerActivityEventMapper>();
            services.AddScoped<IPlayerActivityEvents, global::GAME.Infrastructure.Messaging.PlayerActivityEvents>();

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
            services.AddScoped<IBattleLabService, BattleLabService>();
            services.AddScoped<BattleLabBuildResolver>();
            services.AddScoped<IDungeonStarService, DungeonStarService>();
            services.AddScoped<IDungeonRewardService, DungeonRewardService>();
            services.AddScoped<IDungeonEquipmentRewardPolicy, DungeonEquipmentRewardPolicy>();
            services.AddScoped<IDungeonChestService, DungeonChestService>();
            services.AddScoped<IDungeonService, DungeonService>();
            services.AddScoped<ILevelExperienceService, LevelExperienceService>();
            AddBattleEffectHandlers(services);
            AddBattleTargetSelectors(services);
            AddBattleSkillHandlers(services);
            AddBattleReactionHandlers(services);
            services.AddSingleton<IBattleSimulationEngine, BattleSimulationEngine>();
            services.AddScoped<IHeroStatCalculationService, HeroStatCalculationService>();
            services.AddScoped<IHeroEquipmentService, HeroEquipmentService>();
            services.AddScoped<IGameFeatureConfigService, GameFeatureConfigService>();
            services.AddScoped<ICombatPowerService, CombatPowerService>();
            services.AddScoped<IHeroUpgradeService, HeroUpgradeService>();
            services.AddScoped<IHeroProgressionStatService, HeroProgressionStatService>();
            services.AddScoped<IHeroStarUpgradeService, HeroStarUpgradeService>();

            services.AddRabbitMqMessaging(configuration)
                .EnsureRabbitTopology()
                .AddNamedRabbitConsumer<ProcessQuickClimbFloorRequestedV1, QuickClimbFloorRequestedHandler>(
                    "QuickClimb");
            services.AddTransactionalMessaging<GameDbContext>(configuration);

            services.AddScoped<TowerOperationRunner>();
            services.AddScoped<TowerBattleExecutor>();
            services.AddScoped<TowerRewardService>();
            // Event & Tower Climb Services
            services.AddScoped<IEventPeriodService, EventPeriodService>();
            services.AddScoped<ITowerClimbService, TowerClimbService>();
            services.AddScoped<ITowerQuickClimbService, TowerQuickClimbService>();

            // Domain Services & Infrastructure abstractions (DDD)
            services.AddSingleton<IEnhancementRoller, CryptoEnhancementRoller>();
            services.AddScoped<IEquipmentEnhancementDomainService, EquipmentEnhancementDomainService>();
        }

        private static void AddBattleEffectHandlers(IServiceCollection services)
        {
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
            services.AddSingleton<IBattleEffectHandler, ActionBarChangeEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, DispelDebuffEffectHandler>();
            services.AddSingleton<IBattleEffectHandler, RicardoEffectHandler>();
            services.AddSingleton<BattleEffectHandlerRegistry>();
        }

        private static void AddBattleTargetSelectors(IServiceCollection services)
        {
            services.AddSingleton<IBattleTargetSelector, SelfTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, AllyAllTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, AllyRandomTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, AllyRandom2TargetSelector>();
            services.AddSingleton<IBattleTargetSelector, AllyLowestEnergyTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemySingleTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyAllTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyRandomTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyRandom4TargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyRandomDistinctNTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyRandom3TargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyFrontStraightRowTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyFrontRowTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyBackRowTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemySameLaneBackRowTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, LowestHpPercentTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, AllyLowestHpPreferWithoutStatusTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemySameVerticalLaneTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, EnemyFrontRowFallbackTargetSelector>();
            services.AddSingleton<IBattleTargetSelector, RandomEligibleAlliesNTargetSelector>();
            services.AddSingleton<BattleTargetSelectorRegistry>();
        }

        private static void AddBattleSkillHandlers(IServiceCollection services)
        {
            services.AddSingleton<DefaultSkillHandler>();
            services.AddSingleton<ISkillHandler, FatalAllInSkillHandler>();
            services.AddSingleton<ISkillHandler, HaiLastSmileSkillHandler>();
            services.AddSingleton<ISkillHandler, ChuanMenSkillHandler>();
            services.AddSingleton<ISkillHandler, ThanhThaiAuraSkillHandler>();
            services.AddSingleton<ISkillHandler, NghiaPhucPrimeSkillHandler>();
            services.AddSingleton<ISkillHandler, SibaThienThanSkillHandler>();
            services.AddSingleton<ISkillHandler, KietMaiXeoSkillHandler>();
            services.AddSingleton<ISkillHandler, TruongKietGraduationSkillHandler>();
            services.AddSingleton<ISkillHandler, QuocNhanGraduationSkillHandler>();
            services.AddSingleton<ISkillHandler, LongLeCatSkillHandler>();
            services.AddSingleton<ISkillHandler, QuocNhanRunNowSkillHandler>();
            services.AddSingleton<SkillHandlerRegistry>();
        }

        private static void AddBattleReactionHandlers(IServiceCollection services)
        {
            services.AddSingleton<IBattleStatusReactionHandler, RicardoStatusReactionHandler>();
            services.AddSingleton<IBattleStatusReactionHandler, TinChiDanhDuReactionHandler>();
            services.AddSingleton<IBattleStatusReactionHandler>(_ => new CatScratchReactionHandler(BattleCodes.CatScratch));
            services.AddSingleton<IBattleStatusReactionHandler>(_ => new CatScratchReactionHandler(BattleCodes.DeepCatScratch));
            services.AddSingleton<BattleStatusReactionHandlerRegistry>();
            services.AddSingleton<IBattleCombatantReactionHandler, ThanhThaiAuraReactionHandler>();
            services.AddSingleton<IBattleCombatantReactionHandler, CatCombatantReactionHandler>();
            services.AddSingleton<BattleCombatantReactionRegistry>();
            services.AddSingleton<IBattleSkillSelectionStrategy, ThanhThaiAuraSkillSelectionStrategy>();
            services.AddSingleton<BattleSkillSelectionStrategyRegistry>();
            services.AddSingleton<IDamageRedirectHandler, NghiaPhucPrimeReactionHandler>();
            services.AddSingleton<DamageRedirectHandlerRegistry>();
        }

        public static void AddOptions(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions(configuration);
            services.Configure<InventorySettings>(configuration.GetSection(InventorySettings.SectionName));
        }
    }
}
