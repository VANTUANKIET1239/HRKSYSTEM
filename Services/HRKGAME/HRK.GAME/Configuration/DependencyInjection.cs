using Core.Persistence.Database.Extensions;
using Core.Persistence.DependencyInjection;
using Core.Application.DependencyInjection;
using Core.Caching;
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
using GAME.Application.Services;
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
using GAME.Infrastructure.Messaging.Consumers;
using Oservability;
using Oservability.Tracing;
using Core.HealthChecks;
using HRK.GAME.HealthChecks.Dependencies;
using HRK.GAME.Filters;
using Core.Web.Responses;
using Core.Localization;
using Core.Web.Compression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text;
using GAME.Application.Templates;
using GAME.Infrastructure.Messaging;
using GAME.Infrastructure.Templates;
using GAME.Infrastructure.Scheduling;
using HRK.GAME.Common;

namespace HRK.GAME.Configuration
{
    public static class DependencyInjection
    {
        public static WebApplicationBuilder AddCustomDependency(this WebApplicationBuilder builder)
        {
            AddObservability(builder);
            builder.Services.AddCustomDependency(builder.Configuration, builder.Environment);
            return builder;
        }

        public static void AddObservability(WebApplicationBuilder builder) => builder.AddHrkObservability("HRK.GAME");
        public static void AddRedisTelemetry(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHrkRedisTelemetry();
            services.AddOptions<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions>()
                .Configure<RedisTelemetry>((options, telemetry) =>
                    options.ConnectionMultiplexerFactory = () => telemetry.ConnectAsync(
                        configuration["Redis:ConnectionString"] ?? "localhost:6379,abortConnect=false"));
        }

        public static void AddServiceHealthChecks(IServiceCollection services, IConfiguration configuration)
        {
            var checks = services.AddHrkHealthChecks("HRK.GAME", configuration)
                .AddDependency<GameDatabaseHealthCheck>("database")
                .AddDependency<GameRabbitMqHealthCheck>("rabbitmq")
                .AddDependency<GameRedisHealthCheck>("redis");
            if (configuration.GetValue("Localization:Enabled", true))
                checks.AddDependency<GameLocalizationRedisHealthCheck>("redis.localization");
        }
        public static IServiceCollection AddCustomDependency(this IServiceCollection services, IConfiguration configuration,
            IHostEnvironment? environment = null)
        {
            AddServices(services, configuration, environment);
            return services;
        }

        public static void AddDbContexts(IServiceCollection services, IConfiguration configuration) =>
            services.AddDatabase<GameDbContext>(configuration);

        public static void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
        {
            AddOptions(services, configuration);
            AddPersistenceServices(services, configuration);
            AddApiServices(services);
            AddResponseCompression(services, configuration);
            AddSecurityServices(services, configuration, environment?.IsDevelopment() == true);
            AddCachingServices(services, configuration);
            AddMessagingServices(services, configuration);
            AddInternalApiServices(services, configuration);
            AddPlayerServices(services);
            AddFormationServices(services);
            AddProgressionServices(services);
            AddInventoryServices(services);
            AddDungeonServices(services);
            AddBattleServices(services);
            AddSummonServices(services);
            AddEventServices(services);
            AddSchedulingServices(services, configuration);
            AddServiceHealthChecks(services, configuration);
            services.AddCoreApplication(typeof(IGameRequestContext).Assembly);
        }

        private static void AddPersistenceServices(IServiceCollection services, IConfiguration configuration)
        {
            AddDbContexts(services, configuration);
            services.AddRepositoryUOW<GameDbContext>();
        }

        private static void AddApiServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSwaggerGen();
            services.AddControllers(options => options.AddGameApiFilters());
            services.AddHrkApiResponses();
        }

        private static void AddResponseCompression(IServiceCollection services, IConfiguration configuration) =>
            services.AddHrkResponseCompression(configuration);

        private static void AddInternalApiServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHrkInternalApi(configuration);
            services.AddHrkLocalization(configuration, "game");
        }

        private static void AddSecurityServices(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
        {
            services
                .AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", opts =>
                {
                    opts.Audience = configuration["JwtSettings:Audience"];
                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = configuration["JwtSettings:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = configuration["JwtSettings:Audience"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"] ?? "Kite@1239kK10Kite@1239kK10Kite@1239kK10")
                        ),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(60)
                    };

                    if (isDevelopment)
                        opts.RequireHttpsMetadata = false;
                });

            services.AddAuthorization();
        }

        private static void AddCachingServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHrkRedisCache(configuration);
            services.AddGameTemplateCache(configuration);
            services.AddScoped<IGameTemplateRefreshAccess, TemplateRefreshAccess>();
            AddRedisTelemetry(services, configuration);
        }

        private static void AddMessagingServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<PlayerActivityEventMapper>();
            services.AddScoped<IPlayerActivityEvents, PlayerActivityEvents>();
            services.AddRabbitMqMessaging(configuration).EnsureRabbitTopology()
                .AddNamedRabbitConsumer<ProcessQuickClimbFloorRequestedV1, QuickClimbFloorRequestedHandler>("QuickClimb")
                .AddNamedRabbitConsumer<AccountRegisteredV1, AccountRegisteredHandler>("AccountRegistered");
            services.AddTransactionalMessaging<GameDbContext>(configuration);
        }

        private static void AddPlayerServices(IServiceCollection services)
        {
            services.AddScoped<IGamePlayerService, GamePlayerService>();
            services.AddScoped<NewPlayerHeroFactory>();
            services.AddScoped<IPlayerReadModels, PlayerReadModels>();
            services.AddScoped<IGameRequestContext, HttpGameRequestContext>();
        }

        private static void AddFormationServices(IServiceCollection services)
        {
            services.AddScoped<IFormationPowerQueryService, FormationPowerQueryService>();
            services.AddScoped<IFormationSnapshotService, FormationSnapshotService>();
            services.AddScoped<IFormationStatService, FormationStatService>();
        }

        private static void AddProgressionServices(IServiceCollection services)
        {
            services.AddScoped<ILevelExperienceService, LevelExperienceService>();
            services.AddScoped<IHeroStatCalculationService, HeroStatCalculationService>();
            services.AddScoped<ICombatPowerService, CombatPowerService>();
            services.AddScoped<IHeroProgressionStatService, HeroProgressionStatService>();
        }

        private static void AddInventoryServices(IServiceCollection services)
        {
            services.AddScoped<IEquipmentInstanceFactory, EquipmentInstanceFactory>();
            services.AddScoped<IItemStatCalculationService, ItemStatCalculationService>();
            services.AddSingleton<IEnhancementRoller, CryptoEnhancementRoller>();
            services.AddScoped<IEquipmentEnhancementDomainService, EquipmentEnhancementDomainService>();
        }

        private static void AddDungeonServices(IServiceCollection services)
        {
            services.AddScoped<IDungeonChestReader, DungeonChestReader>();
            services.AddScoped<IDungeonStarService, DungeonStarService>();
            services.AddScoped<IDungeonRewardService, DungeonRewardService>();
            services.AddScoped<IDungeonEquipmentRewardPolicy, DungeonEquipmentRewardPolicy>();
        }

        private static void AddBattleServices(IServiceCollection services)
        {
            services.AddSingleton<IRandomService, DefaultRandomService>();
            services.AddScoped<IBattleExecutor, BattleExecutor>();
            services.AddScoped<BattleLabBuildResolver>();
            AddBattleEffectHandlers(services);
            AddBattleTargetSelectors(services);
            AddBattleSkillHandlers(services);
            AddBattleReactionHandlers(services);
            services.AddSingleton<IBattleSimulationEngine, BattleSimulationEngine>();
        }

        private static void AddSummonServices(IServiceCollection services)
        {
            services.AddSingleton<ISummonRandom, CryptoSummonRandom>();
            services.AddScoped<SummonRewardGranter>();
        }

        private static void AddEventServices(IServiceCollection services)
        {
            services.TryAddSingleton<TimeProvider>(TimeProvider.System);
            services.AddScoped<GameRewardGranter>();
            services.AddScoped<TowerOperationRunner>();
            services.AddScoped<TowerBattleExecutor>();
            services.AddScoped<TowerRewardService>();
            services.AddScoped<ITowerEventService, TowerEventService>();
        }

        private static void AddSchedulingServices(IServiceCollection services, IConfiguration configuration) =>
            services.AddGameScheduling(configuration);

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
            services.Configure<InventorySettings>(configuration.GetSection(InventorySettings.SectionName));
        }
    }
}
