using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Core.Application.DependencyInjection;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Web.Responses;
using GAME.Application.Features.Commands.GameConfig;
using GAME.Application.Features.Queries.GameConfig;
using GAME.Application.Templates;
using GAME.Infrastructure.Templates;
using HRK.GAME.Common;
using HRK.GAME.Controllers;
using HRK.GAME.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class TemplateRefreshHttpTests
{
    [Theory]
    [InlineData("Development", false, false, true)]
    [InlineData("Production", true, true, true)]
    [InlineData("Production", false, true, false)]
    [InlineData("Production", true, false, false)]
    [InlineData("Staging", false, false, false)]
    public async Task CapabilityAndPostEnforceActualEnvironmentAndAdminPolicy(string environment, bool enabled, bool admin, bool allowed)
    {
        var publisher = new Publisher();
        using var server = new TestServer(new WebHostBuilder().UseEnvironment(environment).ConfigureServices(services =>
        {
            services.AddHttpContextAccessor(); services.AddAuthorization();
            services.Configure<GameTemplateCacheOptions>(options => options.AllowManualRefresh = enabled);
            services.AddSingleton<IGameTemplateRefresh>(publisher);
            services.AddScoped<IGameTemplateRefreshAccess, TemplateRefreshAccess>();
            services.AddCoreApplication(typeof(RefreshTemplateCacheCommand).Assembly);
            services.AddControllers(options => options.AddGameApiFilters()).AddApplicationPart(typeof(GameConfigController).Assembly);
            services.AddHrkApiResponses();
        }).Configure(app =>
        {
            app.UseHrkApiResponses();
            app.Use(async (context, next) =>
            {
                var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "operator") };
                if (admin) claims.Add(new(ClaimTypes.Role, "Admin"));
                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
                await next();
            });
            app.UseRouting(); app.UseAuthorization(); app.UseEndpoints(endpoints => endpoints.MapControllers());
        }));
        using var client = server.CreateClient();
        var capability = await client.GetFromJsonAsync<BaseResponse<TemplateCacheCapabilitiesDto>>("/api/game-config/templates/capabilities");
        Assert.Equal(allowed, capability!.Data!.CanRefresh);
        using var response = await client.PostAsJsonAsync("/api/game-config/templates/refresh", new { });
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(allowed ? 1 : 0, publisher.Calls);
        if (allowed)
        {
            var result = await response.Content.ReadFromJsonAsync<BaseResponse<TemplateRefreshResult>>();
            Assert.Equal("revision", result!.Data!.Revision);
            Assert.False(string.IsNullOrWhiteSpace(result.TraceId));
        }
    }

    [Fact]
    public void DevelopmentStillRequiresAnAuthenticatedIdentity()
    {
        var context = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var access = new TemplateRefreshAccess(context, new CacheEnvironment { EnvironmentName = "Development" }, Options.Create(new GameTemplateCacheOptions()));
        Assert.False(access.CanRefresh);
    }

    private sealed class Publisher : IGameTemplateRefresh
    {
        public int Calls;
        public Task<TemplateRefreshResult> RefreshAsync<T>(CancellationToken cancellationToken = default) where T : class => RefreshAsync(cancellationToken);
        public Task<TemplateRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
        { Calls++; return Task.FromResult(new TemplateRefreshResult("revision", DateTimeOffset.UtcNow, 41, 12)); }
    }
}
