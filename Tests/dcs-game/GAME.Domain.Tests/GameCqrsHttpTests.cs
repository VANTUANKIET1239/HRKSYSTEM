using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Core.Application.DependencyInjection;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using Core.Messaging.Contracts;
using Core.RabbitMQ.Interfaces;
using Core.Web.Responses;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Features.Commands.Formation;
using GAME.Application.Features.Commands.CheckIn;
using GAME.Application.Features.Queries.CheckIn;
using GAME.Application.Features.Commands.QuickClimb;
using GAME.Application.Features.Commands.Tower;
using GAME.Application.Features.Queries.Formation;
using GAME.Application.Features.Queries.Player;
using GAME.Application.Features.Queries.Tower;
using GAME.Infrastructure.Messaging.Consumers;
using HRK.GAME.Controllers;
using HRK.GAME.Filters;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class GameCqrsHttpTests
{
    [Fact]
    public async Task CheckInRoutesUseAuthenticatedPlayerAndDoNotRunTowerLifecycle()
    {
        var probe = new CqrsTransportProbe();
        using var server = Server(probe);
        using var client = server.CreateClient();
        using var get = await client.GetAsync("/api/check-in");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("authenticated-owner", Assert.IsType<GetCheckInCalendarQuery>(Assert.Single(probe.Requests)).UserId);
        probe.Requests.Clear();
        var requestId = Guid.NewGuid();
        using var claim = await client.PostAsJsonAsync("/api/check-in/claim", new { RequestId = requestId, PlayerId = 999, CheckInDate = "2000-01-01" });
        Assert.Equal(HttpStatusCode.OK, claim.StatusCode);
        var command = Assert.IsType<ClaimCheckInCommand>(Assert.Single(probe.Requests));
        Assert.Equal("authenticated-owner", command.UserId);
        Assert.Equal(requestId, command.RequestId);
    }

    [Theory]
    [InlineData("/api/events", null)]
    [InlineData("/api/events/tower/progress", "TOWER_CLIMB")]
    [InlineData("/api/events/tower/pending-rewards", "TOWER_CLIMB")]
    public async Task Tower_routes_refresh_the_player_period_before_reading(string route, string? expectedScope)
    {
        var probe = new CqrsTransportProbe();
        using var server = Server(probe);
        using var response = await server.CreateClient().GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, probe.Requests.Count);
        var lifecycle = Assert.IsType<RefreshTowerPeriodCommand>(probe.Requests[0]);
        Assert.Equal(expectedScope, lifecycle.EventCode);
        Assert.Equal("authenticated-owner", lifecycle.UserId);
        if (route.EndsWith("pending-rewards")) Assert.IsType<GetPendingRewardsQuery>(probe.Requests[1]);
    }

    private static TestServer Server(CqrsTransportProbe probe) => new(new WebHostBuilder().ConfigureServices(services =>
    {
        services.AddSingleton(probe);
        services.AddCoreApplication(typeof(GameCqrsHttpTests).Assembly);
        services.AddAuthorization();
        services.AddControllers(options => options.AddGameApiFilters()).AddApplicationPart(typeof(PlayerController).Assembly);
        services.AddHrkApiResponses();
    }).Configure(app =>
    {
        app.UseHrkApiResponses();
        app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "authenticated-owner")], "test"));
            await next();
        });
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }));
    [Theory]
    [InlineData("/api/formations/LUC_DO")]
    [InlineData("/api/formation/LUC_DO")]
    public async Task Formation_routes_send_lifecycle_then_query_and_preserve_response_envelope(string route)
    {
        var probe = new CqrsTransportProbe();
        using var server = Server(probe);
        using var response = await server.CreateClient().GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.Equal(200, json.GetProperty("statusCode").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("traceId").GetString()));
        Assert.Equal("LUC_DO", json.GetProperty("data").GetProperty("code").GetString());
        Assert.Equal("authenticated-owner", Assert.IsType<InitializePlayerFormationsCommand>(probe.Requests[0]).UserId);
        var query = Assert.IsType<GetFormationDetailQuery>(probe.Requests[1]);
        Assert.Equal("authenticated-owner", query.UserId);
        Assert.Equal("LUC_DO", query.FormationCode);
    }

    [Fact]
    public async Task Tower_missing_battle_returns_business_404_through_game_filters()
    {
        var probe = new CqrsTransportProbe();
        using var server = Server(probe);
        using var response = await server.CreateClient().GetAsync("/api/events/tower/battles/missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.Equal("BATTLE_NOT_FOUND", json.GetProperty("error").GetProperty("code").GetString());
        var query = Assert.IsType<GetBattleDetailQuery>(Assert.Single(probe.Requests));
        Assert.Equal("authenticated-owner", query.UserId);
        Assert.Equal("missing", query.BattleId);
    }

    [Fact]
    public async Task Custom_avatar_keeps_binary_content_cache_headers_and_not_found_behavior()
    {
        var probe = new CqrsTransportProbe();
        using var server = Server(probe);
        using var client = server.CreateClient();
        using var response = await client.GetAsync("/api/player/avatar/custom/42");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(new byte[] { 1, 2, 3 }, await response.Content.ReadAsByteArrayAsync());
        Assert.Equal("\"hash\"", response.Headers.ETag!.Tag);
        Assert.Equal(TimeSpan.FromHours(1), response.Headers.CacheControl!.MaxAge);
        using var missing = await client.GetAsync("/api/player/avatar/custom/99");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Rabbit_adapter_preserves_message_identity_version_and_transient_retry_marker()
    {
        var probe = new CqrsTransportProbe();
        using var server = Server(probe);
        var adapter = new QuickClimbFloorRequestedHandler(server.Services.GetRequiredService<IMediator>());
        var message = new ProcessQuickClimbFloorRequestedV1(Guid.NewGuid(), "job", "owner", 7, 12);
        var error = await Assert.ThrowsAsync<QuickClimbTransientMessageException>(() => adapter.HandleAsync(message));
        Assert.IsAssignableFrom<ITransientMessageException>(error);
        Assert.IsType<QuickClimbTransientException>(error.InnerException);
        var command = Assert.IsType<ProcessQuickClimbFloorCommand>(Assert.Single(probe.Requests));
        Assert.Equal(message.EventId, command.MessageId);
        Assert.Equal(message.UserId, command.UserId);
        Assert.Equal(message.ExpectedFloor, command.ExpectedFloor);
        Assert.Equal(message.Version, command.ExpectedVersion);
        Assert.StartsWith("rabbit-", command.WorkerId);
    }
}

public sealed class CqrsTransportProbe
{
    public List<object> Requests { get; } = [];
}

public sealed class TowerLifecycleProbeHandler(CqrsTransportProbe probe)
    : IRequestHandler<RefreshTowerPeriodCommand, BaseResponse<bool>>,
      IRequestHandler<GetEventsQuery, BaseResponse<List<GameEventDto>>>,
      IRequestHandler<GetPlayerProgressQuery, BaseResponse<PlayerEventProgressDto>>,
      IRequestHandler<GetPendingRewardsQuery, BaseResponse<List<PlayerPendingRewardDto>>>
{
    public Task<BaseResponse<bool>> Handle(RefreshTowerPeriodCommand request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        return Task.FromResult(BaseResponse<bool>.SuccessResponse(true));
    }

    public Task<BaseResponse<List<GameEventDto>>> Handle(GetEventsQuery request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        return Task.FromResult(BaseResponse<List<GameEventDto>>.SuccessResponse([]));
    }

    public Task<BaseResponse<PlayerEventProgressDto>> Handle(GetPlayerProgressQuery request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        return Task.FromResult(BaseResponse<PlayerEventProgressDto>.SuccessResponse(new()));
    }

    public Task<BaseResponse<List<PlayerPendingRewardDto>>> Handle(GetPendingRewardsQuery request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        return Task.FromResult(BaseResponse<List<PlayerPendingRewardDto>>.SuccessResponse([]));
    }
}

public sealed class FormationInitializationProbeHandler(CqrsTransportProbe probe) : IRequestHandler<InitializePlayerFormationsCommand, BaseResponse<bool>>
{
    public Task<BaseResponse<bool>> Handle(InitializePlayerFormationsCommand request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        return Task.FromResult(BaseResponse<bool>.SuccessResponse(true));
    }
}

public sealed class FormationDetailProbeHandler(CqrsTransportProbe probe) : IRequestHandler<GetFormationDetailQuery, BaseResponse<FormationDetailDto?>>
{
    public Task<BaseResponse<FormationDetailDto?>> Handle(GetFormationDetailQuery request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        return Task.FromResult(BaseResponse<FormationDetailDto?>.SuccessResponse(new() { Code = request.FormationCode, Name = "Formation" }));
    }
}

public sealed class TowerBattleProbeHandler(CqrsTransportProbe probe) : IRequestHandler<GetBattleDetailQuery, BaseResponse<StartTowerBattleResultDto?>>
{
    public Task<BaseResponse<StartTowerBattleResultDto?>> Handle(GetBattleDetailQuery request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        throw new BusinessException("BATTLE_NOT_FOUND", "Battle was not found.", BusinessErrorKind.NotFound);
    }
}

public sealed class AvatarFileProbeHandler(CqrsTransportProbe probe) : IRequestHandler<GetPlayerCustomAvatarQuery, BaseResponse<PlayerCustomAvatarDto?>>
{
    public Task<BaseResponse<PlayerCustomAvatarDto?>> Handle(GetPlayerCustomAvatarQuery request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        return Task.FromResult(BaseResponse<PlayerCustomAvatarDto?>.SuccessResponse(request.PlayerId == 42 ? new() { ImageData = [1, 2, 3], ContentType = "image/png", ContentHash = "hash" } : null));
    }
}

public sealed class QuickClimbWorkerProbeHandler(CqrsTransportProbe probe) : IRequestHandler<ProcessQuickClimbFloorCommand, BaseResponse<bool>>
{
    public Task<BaseResponse<bool>> Handle(ProcessQuickClimbFloorCommand request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        throw new QuickClimbTransientException("Retry floor", new TimeoutException());
    }
}
