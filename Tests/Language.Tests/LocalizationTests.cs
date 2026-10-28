using System.Net;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Common.Errors;
using Core.InternalApi;
using Core.Localization;
using Core.Web.Filters;
using Core.Web.Responses;
using Core.Web.Compression;
using HRK.LANGUAGE.Controllers;
using Language.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Language.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void InterpolatedBusinessErrorsPreserveTheTemplateAndRawArguments()
    {
        var error = BusinessException.Localized("BALANCE", $"Cần {1234:N0} Vàng, hiện có {500:N0}.");
        Assert.Equal("Cần {{p0}} Vàng, hiện có {{p1}}.", error.MessageTemplate);
        Assert.Equal(1234, error.Arguments!["p0"]);
        Assert.Equal(500, error.Arguments["p1"]);
        Assert.Equal("text_a430d84680aabd0b", TranslationIdentity.TextKey("hello"));
    }
    [Fact]
    public async Task CurrentCacheIsReadAcrossRequestsAndApiIsNotCalledOnHits()
    {
        var cache = new MemoryTranslations();
        cache.Bundles[("game", "en-US")] = Bundle(1, "First");
        var calls = 0;
        using var provider = Services(cache, _ => { calls++; return Bundle(99, "HTTP"); }).BuildServiceProvider();
        using (var scope = provider.CreateScope())
            Assert.Equal("First", await scope.ServiceProvider.GetRequiredService<ITextLocalizer>().GetAsync("game", "en-US", "GREETING", "Fallback"));
        cache.Bundles[("game", "en-US")] = Bundle(2, "Updated");
        using (var scope = provider.CreateScope())
            Assert.Equal("Updated", await scope.ServiceProvider.GetRequiredService<ITextLocalizer>().GetAsync("game", "en-US", "GREETING", "Fallback"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task MissingCacheUsesInternalApiAndMemoizesOnlyWithinRequest()
    {
        var calls = 0;
        using var provider = Services(new MemoryTranslations(), request =>
        { Assert.EndsWith("/api/language/bundles/game/en-US", request.RequestUri!.AbsolutePath); calls++; return Bundle(2, "Hello"); }).BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var localizer = scope.ServiceProvider.GetRequiredService<ITextLocalizer>();
            Assert.Equal("Hello", await localizer.GetAsync("game", "en-US", "GREETING", "Fallback"));
            Assert.Equal("Hello", await localizer.GetAsync("game", "en-US", "GREETING", "Fallback"));
        }
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task DependencyFailuresPreserveMessageAndRequestCancellationIsNotSwallowed()
    {
        var cache = new MemoryTranslations { Failure = true };
        using var provider = Services(cache, _ => throw new HttpRequestException("offline")).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var localizer = scope.ServiceProvider.GetRequiredService<ITextLocalizer>();
        Assert.Equal("Original", await localizer.GetAsync("game", "en-US", "UNKNOWN", "Original"));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new TextLocalizer(cache,
            scope.ServiceProvider.GetRequiredService<IInternalApiClient>(), Options.Create(new LocalizationOptions()),
            NullLogger<TextLocalizer>.Instance).GetAsync("game", "en-US", "UNKNOWN", "Original", ct: canceled.Token));
    }

    [Fact]
    public async Task PreciseLiteralWinsOverGenericCodeAndPlaceholdersUseArguments()
    {
        var cache = new MemoryTranslations();
        cache.Bundles[("game", "en-US")] = new("game", "en-US", 1, new Dictionary<string,string>
        { ["ITEM_LOCKED"] = "Locked", [TranslationIdentity.TextKey("Cannot sell this item.")] = "Selling is blocked.", ["LEVEL"] = "Reach level {{level}}." });
        using var provider = Services(cache, _ => throw new Exception()).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var localizer = scope.ServiceProvider.GetRequiredService<ITextLocalizer>();
        Assert.Equal("Selling is blocked.", await localizer.GetAsync("game", "en-US", "ITEM_LOCKED", "Cannot sell this item."));
        Assert.Equal("Reach level 8.", await localizer.GetAsync("game", "en-US", "LEVEL", "Original", new Dictionary<string,object?> { ["level"] = 8 }));
    }

    [Theory]
    [InlineData("W/\"game-en-US-r7\"")]
    [InlineData("\"game-en-US-r7\"")]
    [InlineData("\"other\", W/\"game-en-US-r7\"")]
    [InlineData("*")]
    public async Task BundleEndpointHonorsConditionalRequests(string condition)
    {
        using var server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(LanguageBundlesController).Assembly);
            services.AddSingleton<IPublishedLanguageReader>(new StubReader());
        }).Configure(app => { app.UseRouting(); app.UseEndpoints(endpoints => endpoints.MapControllers()); }));
        using var client = server.CreateClient();
        using var first = await client.GetAsync("/api/language/bundles/game/en-US");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("W/\"game-en-US-r7\"", first.Headers.ETag!.ToString());
        Assert.Equal(7, (await first.Content.ReadFromJsonAsync<TranslationBundle>())!.Revision);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/language/bundles/game/en-US");
        request.Headers.TryAddWithoutValidation("If-None-Match", condition);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        using var changed = new HttpRequestMessage(HttpMethod.Get, request.RequestUri);
        changed.Headers.TryAddWithoutValidation("If-None-Match", "W/\"game-en-US-r6\"");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(changed)).StatusCode);
    }

    [Theory]
    [InlineData("gzip")]
    [InlineData("br")]
    public async Task BundleCompressionPreservesEtagAndNeverWritesABodyFor304(string encoding)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["ResponseCompression:Policies:Language:AllowedEncodings:0"] = "gzip",
            ["ResponseCompression:Policies:Language:AllowedEncodings:1"] = "br",
            ["ResponseCompression:Policies:Language:MinimumBodyBytes"] = "0"
        }).Build();
        using var server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(LanguageBundlesController).Assembly);
            services.AddSingleton<IPublishedLanguageReader>(new StubReader());
            services.AddHrkResponseCompression(configuration);
        }).Configure(app => { app.UseRouting(); app.UseHrkResponseCompression(); app.UseEndpoints(endpoints => endpoints.MapControllers()); }));
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", encoding);
        using var response = await client.GetAsync("/api/language/bundles/game/en-US");
        Assert.Equal(encoding, Assert.Single(response.Content.Headers.ContentEncoding));
        Assert.Contains("Accept-Encoding", response.Headers.Vary);
        await using var body = await response.Content.ReadAsStreamAsync();
        await using Stream decoder = encoding == "gzip" ? new GZipStream(body, CompressionMode.Decompress) : new BrotliStream(body, CompressionMode.Decompress);
        var bundle = await JsonSerializer.DeserializeAsync<TranslationBundle>(decoder, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(7, bundle!.Revision);
        using var conditional = new HttpRequestMessage(HttpMethod.Get, "/api/language/bundles/game/en-US");
        conditional.Headers.IfNoneMatch.Add(response.Headers.ETag!);
        using var unchanged = await client.SendAsync(conditional);
        Assert.Equal(HttpStatusCode.NotModified, unchanged.StatusCode);
        Assert.Empty(await unchanged.Content.ReadAsByteArrayAsync());
        Assert.Empty(unchanged.Content.Headers.ContentEncoding);
    }

    [Fact]
    public async Task ErrorBoundaryTranslatesWithoutChangingStatusCodeOrErrorCode()
    {
        var cache = new MemoryTranslations();
        cache.Bundles[("game", "en-US")] = new("game", "en-US", 1, new Dictionary<string,string>
        {
            ["LEVEL"] = "Reach level {{level}}.",
            [TranslationIdentity.TextKey("Cần {{p0}} Vàng, hiện có {{p1}}.")] = "Need {{p0}} gold; available: {{p1}}."
        });
        using var server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
        {
            foreach (var item in Services(cache, _ => throw new Exception())) services.Add(item);
            services.Configure<LocalizationOptions>(options => options.Group = "game");
            services.AddControllers(options => options.AddHrkApiFilters()).AddApplicationPart(typeof(LocalizationProbeController).Assembly);
            services.AddHrkApiResponses();
        }).Configure(app => { app.UseHrkApiResponses(); app.UseRouting(); app.UseEndpoints(endpoints => endpoints.MapControllers()); }));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/localization-probe");
        request.Headers.AcceptLanguage.ParseAdd("en-US");
        using var response = await server.CreateClient().SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("LEVEL", body.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("Reach level 8.", body.GetProperty("message").GetString());
        Assert.Equal("Reach level 8.", body.GetProperty("error").GetProperty("message").GetString());
        Assert.Contains("Accept-Language", response.Headers.Vary);
        using var cookieRequest = new HttpRequestMessage(HttpMethod.Get, "/localization-probe/interpolated");
        cookieRequest.Headers.TryAddWithoutValidation("Cookie", "hrk_language=en-US");
        using var interpolated = await server.CreateClient().SendAsync(cookieRequest);
        var receipt = await interpolated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Need 1234 gold; available: 500.", receipt.GetProperty("message").GetString());
        Assert.Equal("BALANCE", receipt.GetProperty("error").GetProperty("code").GetString());
    }

    internal static IServiceCollection Services(ITranslationCache cache, Func<HttpRequestMessage, TranslationBundle> response)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(cache);
        services.AddInternalApi(new InternalApiOptions { Services = { ["language"] = new() { BaseAddress = "http://language", CircuitBreaker = new() { Enabled = false } } } });
        services.AddHttpClient(InternalApiClientNames.Rest("language")).ConfigurePrimaryHttpMessageHandler(() => new ResponseHandler(response));
        services.AddSingleton<ITranslationCache>(cache);
        services.Configure<LocalizationOptions>(_ => { });
        services.AddScoped<ITextLocalizer, TextLocalizer>();
        return services;
    }
    internal static TranslationBundle Bundle(long revision, string text) => new("game", "en-US", revision, new Dictionary<string,string> { ["GREETING"] = text });
    private sealed class ResponseHandler(Func<HttpRequestMessage, TranslationBundle> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        { ct.ThrowIfCancellationRequested(); return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response(request)) }); }
    }
    private sealed class StubReader : IPublishedLanguageReader
    {
        public Task<TranslationBundle?> GetAsync(string group, string culture, CancellationToken ct) => Task.FromResult<TranslationBundle?>(Bundle(7, "Hello"));
        public Task<TranslationBundle?> GetFromDatabaseAsync(string group, string culture, CancellationToken ct) => GetAsync(group, culture, ct);
    }
}

[ApiController, Route("localization-probe")]
public sealed class LocalizationProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => throw new BusinessException("LEVEL", "Cần đạt cấp 8.")
    { Arguments = new Dictionary<string,object?> { ["level"] = 8 } };
    [HttpGet("interpolated")]
    public IActionResult Interpolated() => throw BusinessException.Localized("BALANCE", $"Cần {1234:N0} Vàng, hiện có {500:N0}.");
}

internal sealed class MemoryTranslations : ITranslationCache
{
    public Dictionary<(string,string), TranslationBundle> Bundles { get; } = new();
    public bool Failure { get; init; }
    public Task<TranslationBundle?> GetCurrentAsync(string group, string culture, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); if (Failure) throw new IOException("Redis offline"); return Task.FromResult(Bundles.GetValueOrDefault((group,culture))); }
    public Task PublishAsync(TranslationBundle bundle, CancellationToken ct) { Bundles[(bundle.Group,bundle.Culture)] = bundle; return Task.CompletedTask; }
}
