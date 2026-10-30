using System.Security.Claims;
using Core.Common.Errors;
using GAME.Application.Features.Commands.Battle;
using GAME.Application.Features.Commands.PlayerHeroes;
using GAME.Application.Features.Queries.Dungeon;
using GAME.Application.Features.Queries.Formation;
using GAME.Application.Features.Queries.Tower;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using HRK.GAME.Configuration;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Xunit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Core.Web.Compression;
using GAME.Infrastructure.Scheduling;

namespace GAME.Domain.Tests;

public sealed partial class GameCqrsTests
{
    [Fact]
    public void Production_registration_resolves_every_game_handler_in_a_scope()
    {
        var configuration = new ConfigurationBuilder().AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json")).AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Provider"] = "SqlServer", ["Database:ConnectionString"] = "Server=localhost;Database=CqrsRegistrationProbe;Trusted_Connection=True", ["Redis:ConnectionString"] = "localhost:6379,abortConnect=false" }).Build();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(HRK.GAME.Controllers.EventsController).Assembly.GetName().Name,
            EnvironmentName = Environments.Development
        });
        builder.Configuration.AddConfiguration(configuration);
        var services = builder.Services;
        services.AddSingleton<IDiagnosticContext>(new Serilog.Extensions.Hosting.DiagnosticContext(new LoggerConfiguration().CreateLogger()));
        services.AddCustomDependency(builder.Configuration, builder.Environment);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        Assert.Equal("Bearer", provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value.DefaultScheme);
        Assert.False(provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Bearer").RequireHttpsMetadata);
        Assert.NotEmpty(provider.GetRequiredService<IOptions<HrkResponseCompressionOptions>>().Value.Policies);
        var handlers = services.Where(s => s.ServiceType.IsGenericType && s.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)).ToArray();
        Assert.NotEmpty(handlers);
        foreach (var handler in handlers)
            Assert.NotNull(scope.ServiceProvider.GetRequiredService(handler.ServiceType));
    }

    [Fact]
    public async Task Hero_flags_update_both_fields_once_and_reject_another_players_hero()
    {
        var store = new TowerTestStore();
        store.Data<HrkPlayer>().Add(new() { Id = 1, UserId = "owner", IsActive = true });
        var own = new HrkPlayerHero
        {
            Id = 10,
            PlayerId = 1,
            IsActive = true
        };
        var other = new HrkPlayerHero
        {
            Id = 20,
            PlayerId = 2,
            IsActive = true
        };
        store.Data<HrkPlayerHero>().AddRange([own, other]);
        var handler = new UpdatePlayerHeroFlagsCommandHandler(store, TestGamePlayers.Create(store), new Actor("owner"));
        var response = await handler.Handle(new(10, new(true, true)), default);
        Assert.True(response.Data!.IsLocked);
        Assert.True(response.Data.IsFavorite);
        Assert.Equal(1, store.SaveCount);
        var error = await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(new(20, new(true, true)), default));
        Assert.Equal("HERO_NOT_FOUND", error.Code);
        Assert.False(other.IsLocked);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Stamina_query_projects_recovery_without_saving()
    {
        var store = new TowerTestStore();
        store.Data<HrkPlayer>().Add(new() { Id = 1, UserId = "owner", IsActive = true, Stamina = 1, MaxStamina = 100, LastStaminaRegeneratedOn = DateTime.UtcNow.AddMinutes(-13), StaminaPurchaseDate = DateTime.UtcNow.AddDays(-1), DailyStaminaPurchaseCount = 4 });
        var result = await new GetStaminaQueryHandler(store, TestGamePlayers.Create(store)).Handle(new("owner"), default);
        Assert.Equal(3, result.Data!.Current);
        Assert.Equal(0, result.Data.PurchaseCount);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task Tower_query_reads_defaults_without_creating_period_or_progress()
    {
        var store = new TowerTestStore();
        store.Data<HrkPlayer>().Add(new() { Id = 1, UserId = "owner", IsActive = true });
        store.Data<HrkGameEvent>().Add(new() { Id = 1, Code = "TOWER", Name = "Tower", ResetType = "DAILY", TimeZoneId = "UTC", InitialLives = 3, RulesJson = "{}" });
        store.Data<HrkTowerFloor>().Add(new() { Id = 1, EventId = 1, FloorNumber = 1, IsActive = true });
        var handler = new GetEventsQueryHandler(new TowerBattleExecutor(store, null!, null!, null!), store, TestGamePlayers.Create(store), null!, null!, new TowerEventService(store, null!, null!, new EventResetSchedule(store)));
        var response = await handler.Handle(new("owner"), default);
        Assert.Single(response.Data!);
        Assert.Empty(store.Data<HrkEventPeriod>());
        Assert.Empty(store.Data<HrkPlayerEventPeriodProgress>());
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task Formation_detail_query_preserves_missing_resource_error_without_initializing_data()
    {
        var store = new TowerTestStore();
        store.Data<HrkPlayer>().Add(new() { Id = 1, UserId = "owner", IsActive = true });
        var handler = new GetFormationDetailQueryHandler(store, TestGamePlayers.Create(store), null!, null!, null!);
        var error = await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(new("owner", "MISSING"), default));
        Assert.Equal(BusinessErrorKind.NotFound, error.Kind);
        Assert.Empty(store.Data<HrkPlayerFormation>());
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task Battle_command_rejects_campaign_before_running_simulation()
    {
        var handler = new StartBattleCommandHandler(null!, new Actor("owner"));
        var error = await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(new(new() { BattleType = "CAMPAIGN" }), default));
        Assert.Equal(BusinessErrorKind.Validation, error.Kind);
    }

    [Theory]
    [InlineData(ClaimTypes.NameIdentifier)]
    [InlineData("sub")]
    [InlineData("UserId")]
    [InlineData("id")]
    public void Http_actor_reads_supported_identity_claims(string claim)
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(claim, "owner")], "test"));
        Assert.Equal("owner", new HttpGameRequestContext(accessor).UserId);
    }

    [Fact]
    public void Http_actor_rejects_missing_or_empty_identity()
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        Assert.Throws<UnauthorizedAccessException>(() => new HttpGameRequestContext(accessor).UserId);
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", " ")], "test"));
        Assert.Throws<UnauthorizedAccessException>(() => new HttpGameRequestContext(accessor).UserId);
    }
}
