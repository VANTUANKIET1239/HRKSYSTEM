using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Core.Application.DependencyInjection;
using Core.Common.Errors;
using Core.InternalApi;
using Core.Localization;
using Core.Persistence.Repositories;
using Core.Web.Responses;
using GAME.Application.Features.Commands.Inventory.ExpandCapacity;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using HRK.GAME.Controllers;
using HRK.GAME.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAME.Domain.Tests;

public sealed partial class BusinessErrorLocalizationTests
{
    [Theory]
    [InlineData("/api/inventory/expand-capacity", "{\"slotsToAdd\":5}")]
    [InlineData("/api/inventory/sell", "{\"items\":[{\"inventoryItemId\":1,\"count\":1}]}")]
    public async Task MissingWalletThroughProductionHandlersReturnsLocalized404(string route, string payload)
    {
        using var server = Server();
        using var request = new HttpRequestMessage(HttpMethod.Post, route)
        { Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json") };
        request.Headers.AcceptLanguage.ParseAdd("en-US");
        using var response = await server.CreateClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PLAYER_WALLET_NOT_FOUND", body.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("Player wallet not found.", body.GetProperty("message").GetString());
        Assert.Equal("Player wallet not found.", body.GetProperty("error").GetProperty("message").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").ValueKind);
    }

    [Theory]
    [InlineData("configuration")]
    [InlineData("unexpected-key")]
    public async Task InternalFailuresRemainGeneric500Responses(string failure)
    {
        using var server = Server();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/business-error-probe/" + failure);
        request.Headers.AcceptLanguage.ParseAdd("en-US");
        using var response = await server.CreateClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INTERNAL_SERVER_ERROR", body.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("A system error occurred. Please try again later.", body.GetProperty("message").GetString());
        Assert.DoesNotContain("private diagnostic", body.ToString());
    }

    [Fact]
    public void InsufficientInventoryQuantityPreservesStateAndCarriesTranslationArguments()
    {
        var item = new HrkPlayerInventory { Id = 7, Count = 2, ItemTemplate = new() { Name = "Stone" } };
        var error = Assert.Throws<BusinessException>(() => item.ConsumeQuantity(3));
        Assert.Equal("INSUFFICIENT_ITEM_QUANTITY", error.Code);
        Assert.Equal(BusinessErrorKind.Conflict, error.Kind);
        Assert.Equal("Stone", error.Arguments!["p0"]);
        Assert.Equal(2, error.Arguments["p1"]);
        Assert.Equal(3, error.Arguments["p2"]);
        Assert.Equal(2, item.Count);
        Assert.True(item.IsActive);
    }

    private static TestServer Server() => new(new WebHostBuilder().ConfigureServices(services =>
    {
        var store = new TowerTestStore();
        store.Data<HrkPlayer>().Add(new() { Id = 1, UserId = "owner", IsActive = true });
        services.AddSingleton<IUnitOfWork>(store);
        services.AddSingleton<IGamePlayerService>(TestGamePlayers.Create(store));
        services.AddSingleton<IGameRequestContext>(new Actor("owner"));
        services.AddCoreApplication(typeof(ExpandInventoryCapacityCommandHandler).Assembly);
        services.AddAuthorization();
        services.AddControllers(options => options.AddGameApiFilters())
            .AddApplicationPart(typeof(InventoryController).Assembly).AddApplicationPart(typeof(BusinessErrorProbeController).Assembly);
        services.AddHrkApiResponses();
        services.AddInternalApi(new InternalApiOptions());
        services.AddSingleton<ITranslationCache>(new Translations());
        services.AddHrkLocalization(new ConfigurationBuilder().Build(), "game");
    }).Configure(app =>
    {
        app.UseHrkApiResponses();
        app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "owner")], "test"));
            await next();
        });
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }));
}
