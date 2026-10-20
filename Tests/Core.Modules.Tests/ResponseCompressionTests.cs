using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Core.Common.Errors;
using Core.Web.Compression;
using Core.Web.Filters;
using Core.Web.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Core.Modules.Tests;

public sealed class ResponseCompressionTests
{
    private const string Payload = "{\"name\":\"Chiêu mộ tướng\",\"events\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"}";

    private static TestServer Server(int minimum = 16384, Action<HrkResponseCompressionOptions>? configure = null) =>
        new(new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddHrkApiResponses();
            services.AddHrkResponseCompression(new ConfigurationBuilder().Build());
            services.Configure<HrkResponseCompressionOptions>(options =>
            {
                options.EnableForHttps = true;
                options.Policies["Default"].MinimumBodyBytes = minimum;
                configure?.Invoke(options);
            });
            services.AddControllers(options => options.AddHrkApiFilters())
                .AddApplicationPart(typeof(CompressionProbeController).Assembly);
        }).Configure(app =>
        {
            app.UseHrkApiResponses();
            app.UseRouting();
            app.UseHrkResponseCompression();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapMethods("/raw/{mode}", ["GET", "HEAD"], async context =>
                {
                    context.Response.ContentType = "application/json";
                    switch (context.Request.RouteValues["mode"])
                    {
                        case "stream":
                            // Several writes below the threshold must accumulate before choosing compression.
                            for (var i = 0; i < 2000; i++)
                                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Payload), context.RequestAborted);
                            break;
                        case "writer":
                            await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat(Payload, 2000))));
                            break;
                        case "writer-chunks":
                            for (var i = 0; i < 2000; i++)
                                await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(Payload));
                            break;
                        case "complete":
                            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat(Payload, 2000))));
                            await context.Response.CompleteAsync();
                            break;
                        case "start":
                            await context.Response.StartAsync();
                            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Payload));
                            break;
                        case "flush":
                            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("{}"));
                            await context.Response.Body.FlushAsync();
                            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(new string('x', 20000)));
                            break;
                        case "length":
                            context.Response.ContentLength = Encoding.UTF8.GetByteCount(Payload);
                            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Payload));
                            break;
                        case "encoded":
                            context.Response.Headers.ContentEncoding = "custom";
                            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Payload));
                            break;
                        case "file":
                            context.Response.Headers.ContentDisposition = "attachment; filename=events.json";
                            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Payload));
                            break;
                        case "sse":
                            context.Response.ContentType = "text/event-stream";
                            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Payload));
                            break;
                        case "partial":
                            context.Response.StatusCode = 206;
                            context.Response.Headers.ContentRange = "bytes 0-4/10";
                            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Payload));
                            break;
                        case "no-transform":
                            context.Response.Headers.CacheControl = "no-transform";
                            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Payload));
                            break;
                        case "no-content":
                            context.Response.StatusCode = 204;
                            break;
                        case "failure-after-buffer":
                            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("discard-this-prefix"));
                            throw new InvalidOperationException("secret");
                        default:
                            context.Response.Headers.Vary = "Origin";
                            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Payload));
                            break;
                    }
                }).WithMetadata(new HrkResponseCompressionAttribute());
            });
        }));

    private static async Task<HttpResponseMessage> Get(TestServer server, string path, string? accept = "gzip", string scheme = "http")
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{scheme}://localhost{path}");
        if (accept is not null) request.Headers.TryAddWithoutValidation("Accept-Encoding", accept);
        return await server.CreateClient().SendAsync(request);
    }

    private static async Task<byte[]> Decode(HttpResponseMessage response)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync();
        if (response.Content.Headers.ContentEncoding.Count == 0) return bytes;
        using var input = new MemoryStream(bytes);
        using Stream decoder = response.Content.Headers.ContentEncoding.Single() == "gzip"
            ? new GZipStream(input, CompressionMode.Decompress)
            : new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        await decoder.CopyToAsync(output);
        return output.ToArray();
    }

    [Theory]
    [InlineData("gzip", "gzip")]
    [InlineData("br", "br")]
    [InlineData("gzip, br", "br")]
    [InlineData("gzip;q=1, br;q=0.5", "gzip")]
    [InlineData("gzip;q=0, br;q=1", "br")]
    [InlineData("gzip;q=0, *;q=1", "br")]
    [InlineData("br;q=0, *;q=1", "gzip")]
    [InlineData("GZIP", "gzip")]
    public async Task Negotiation_sends_real_compressed_bytes_and_preserves_json(string accept, string expected)
    {
        using var server = Server(minimum: 1);
        using var response = await Get(server, "/raw/normal", accept);
        Assert.Equal(expected, response.Content.Headers.ContentEncoding.Single());
        Assert.Equal(Payload, Encoding.UTF8.GetString(await Decode(response)));
        Assert.Contains("Origin", response.Headers.Vary);
        Assert.Contains("Accept-Encoding", response.Headers.Vary);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("deflate")]
    [InlineData("gzip;q=0, br;q=0")]
    [InlineData("gzip;q=0.5, identity;q=1")]
    [InlineData("malformed;q=no")]
    public async Task Unaccepted_encodings_keep_identity_and_vary(string? accept)
    {
        using var server = Server(minimum: 1);
        using var response = await Get(server, "/raw/normal", accept);
        Assert.Empty(response.Content.Headers.ContentEncoding);
        Assert.Equal(Payload, await response.Content.ReadAsStringAsync());
        Assert.Contains("Accept-Encoding", response.Headers.Vary);
    }

    [Fact]
    public async Task No_acceptable_encoding_is_406_before_endpoint_execution()
    {
        using var server = Server();
        using var response = await Get(server, "/raw/failure-after-buffer", "gzip;q=0, br;q=0, identity;q=0");
        Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
    }

    [Fact]
    public async Task Identity_forbidden_compresses_even_below_threshold()
    {
        using var server = Server();
        using var response = await Get(server, "/raw/normal", "gzip, identity;q=0");
        Assert.Equal("gzip", response.Content.Headers.ContentEncoding.Single());
        Assert.Equal(Payload, Encoding.UTF8.GetString(await Decode(response)));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task Threshold_uses_actual_utf8_byte_count(int delta, bool compressed)
    {
        using var server = Server(Encoding.UTF8.GetByteCount(Payload) + delta);
        using var response = await Get(server, "/raw/length");
        Assert.Equal(compressed, response.Content.Headers.ContentEncoding.Count > 0);
        Assert.Equal(Payload, Encoding.UTF8.GetString(await Decode(response)));
        if (!compressed) Assert.Equal(Encoding.UTF8.GetByteCount(Payload), response.Content.Headers.ContentLength);
    }

    [Theory]
    [InlineData("stream")]
    [InlineData("writer")]
    [InlineData("writer-chunks")]
    [InlineData("complete")]
    public async Task Large_bodies_work_with_stream_pipewriter_and_explicit_completion(string mode)
    {
        using var server = Server();
        using var response = await Get(server, "/raw/" + mode);
        Assert.Equal("gzip", response.Content.Headers.ContentEncoding.Single());
        Assert.Equal(string.Concat(Enumerable.Repeat(Payload, 2000)), Encoding.UTF8.GetString(await Decode(response)));
    }

    [Theory]
    [InlineData("start")]
    [InlineData("flush")]
    public async Task Early_start_or_flush_commits_identity_without_waiting_for_threshold(string mode)
    {
        using var server = Server();
        using var response = await Get(server, "/raw/" + mode);
        Assert.Empty(response.Content.Headers.ContentEncoding);
        Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData("encoded")]
    [InlineData("file")]
    [InlineData("sse")]
    [InlineData("partial")]
    [InlineData("no-transform")]
    [InlineData("no-content")]
    public async Task Excluded_responses_are_not_compressed(string mode)
    {
        using var server = Server(minimum: 0);
        using var response = await Get(server, "/raw/" + mode);
        Assert.DoesNotContain("gzip", response.Content.Headers.ContentEncoding);
        Assert.DoesNotContain("br", response.Content.Headers.ContentEncoding);
    }

    [Fact]
    public async Task Mvc_serializes_the_normalized_envelope_once_and_compresses_large_json()
    {
        using var server = Server();
        using var response = await Get(server, "/compression-probe/large", "gzip");
        Assert.Equal("gzip", response.Content.Headers.ContentEncoding.Single());
        var json = JsonSerializer.Deserialize<JsonElement>(await Decode(response));
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.Equal(200, json.GetProperty("statusCode").GetInt32());
        Assert.NotEmpty(json.GetProperty("traceId").GetString()!);
        Assert.Equal(4000, json.GetProperty("data").GetArrayLength());
        Assert.Equal("Chiêu mộ tướng", json.GetProperty("data")[0].GetProperty("name").GetString());
    }

    [Theory]
    [InlineData("business", 409, "ITEM_LOCKED")]
    [InlineData("system", 500, "INTERNAL_SERVER_ERROR")]
    public async Task Error_pipeline_keeps_its_original_status_and_envelope(string mode, int status, string code)
    {
        using var server = Server(minimum: 0);
        using var response = await Get(server, "/compression-probe/" + mode);
        Assert.Empty(response.Content.Headers.ContentEncoding);
        Assert.Equal(status, (int)response.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(await Decode(response));
        Assert.Equal(code, json.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Uncommitted_prefix_is_discarded_on_exception_before_exception_handler_writes()
    {
        using var server = Server();
        using var response = await Get(server, "/raw/failure-after-buffer");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("discard-this-prefix", text);
        Assert.False(JsonSerializer.Deserialize<JsonElement>(text).GetProperty("success").GetBoolean());
    }

    [Theory]
    [InlineData("/compression-unmarked/unmarked")]
    [InlineData("/compression-probe/disabled")]
    public async Task Method_metadata_can_omit_or_disable_compression(string route)
    {
        using var server = Server(minimum: 0);
        using var response = await Get(server, route);
        Assert.Empty(response.Content.Headers.ContentEncoding);
    }

    [Fact]
    public async Task Global_policy_and_https_switches_disable_compression()
    {
        foreach (var mode in new[] { "global", "policy", "https" })
        {
            using var server = Server(0, options =>
            {
                if (mode == "global") options.Enabled = false;
                if (mode == "policy") options.Policies["Default"].Enabled = false;
                if (mode == "https") options.EnableForHttps = false;
            });
            using var response = await Get(server, "/raw/normal", scheme: "https");
            Assert.Empty(response.Content.Headers.ContentEncoding);
        }
    }

    [Fact]
    public async Task Configured_encoding_allowlist_is_enforced()
    {
        using var server = Server(0, options =>
        {
            options.Policies["Default"].AllowedEncodings = ["gzip"];
            options.Policies["Default"].PreferredEncoding = "gzip";
        });
        using var response = await Get(server, "/raw/normal", "br");
        Assert.Empty(response.Content.Headers.ContentEncoding);
    }

    [Fact]
    public void Invalid_configuration_fails_validation()
    {
        var services = new ServiceCollection();
        services.AddHrkResponseCompression(new ConfigurationBuilder().Build());
        services.Configure<HrkResponseCompressionOptions>(options =>
        {
            options.Policies["Default"].AllowedEncodings = ["deflate"];
            options.Policies["Default"].MinimumBodyBytes = int.MaxValue;
            options.Policies["Default"].Level = CompressionLevel.NoCompression;
        });
        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<HrkResponseCompressionOptions>>().Value);
        Assert.Equal(4, exception.Failures.Count());
    }

    [Fact]
    public void Configuration_binding_replaces_the_encoding_allowlist_and_validates_the_game_settings()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ResponseCompression:Policies:Default:AllowedEncodings:0"] = "gzip",
            ["ResponseCompression:Policies:Default:PreferredEncoding"] = "gzip",
            ["ResponseCompression:Policies:BattleReplay:AllowedEncodings:0"] = "br",
            ["ResponseCompression:Policies:BattleReplay:AllowedEncodings:1"] = "gzip",
            ["ResponseCompression:Policies:BattleReplay:Level"] = "Fastest"
        }).Build();
        var services = new ServiceCollection();
        services.AddHrkResponseCompression(config);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HrkResponseCompressionOptions>>().Value;
        Assert.Equal(["gzip"], options.Policies["Default"].AllowedEncodings!);
        Assert.Equal(["br", "gzip"], options.Policies["BattleReplay"].AllowedEncodings!);
    }

    [Fact]
    public async Task Cancellation_discards_buffer_and_restores_the_server_owned_body_feature()
    {
        var services = new ServiceCollection();
        services.AddHrkResponseCompression(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        using var cancellation = new CancellationTokenSource();
        using var output = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = output;
        context.Request.Headers.AcceptEncoding = "gzip";
        context.RequestAborted = cancellation.Token;
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new HrkResponseCompressionAttribute()), "cancel"));
        var original = context.Features.Get<IHttpResponseBodyFeature>();
        var middleware = new HrkResponseCompressionMiddleware(async http =>
        {
            http.Response.ContentType = "application/json";
            await http.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Payload), cancellation.Token);
            cancellation.Cancel();
            await http.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Payload), cancellation.Token);
        }, provider.GetRequiredService<IOptionsMonitor<HrkResponseCompressionOptions>>(),
            NullLogger<HrkResponseCompressionMiddleware>.Instance);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));
        Assert.Same(original, context.Features.Get<IHttpResponseBodyFeature>());
        Assert.True(output.CanWrite);
        Assert.Equal(0, output.Length);
    }

    [Theory]
    [InlineData("HEAD", false)]
    [InlineData("GET", true)]
    public async Task Head_and_range_requests_bypass_compression(string method, bool range)
    {
        using var server = Server(0);
        using var request = new HttpRequestMessage(new HttpMethod(method), "/raw/normal");
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip");
        if (range) request.Headers.TryAddWithoutValidation("Range", "bytes=0-4");
        using var response = await server.CreateClient().SendAsync(request);
        Assert.Empty(response.Content.Headers.ContentEncoding);
    }
}

[ApiController]
[Route("compression-probe")]
[HrkResponseCompression]
public sealed class CompressionProbeController : ControllerBase
{
    [HttpGet("disabled")]
    [HrkResponseCompression(Enabled = false)]
    public IActionResult Disabled() => Ok(new { name = "disabled" });

    [HttpGet("large")]
    public IActionResult Large() => Ok(Enumerable.Range(0, 4000).Select(sequence => new { sequence, name = "Chiêu mộ tướng", damage = 123 }));

    [HttpGet("business")]
    public IActionResult Business() => throw new BusinessException("ITEM_LOCKED", "Item locked.");

    [HttpGet("system")]
    public IActionResult SystemFailure() => throw new InvalidOperationException("secret");
}

[ApiController]
[Route("compression-unmarked")]
public sealed class UnmarkedCompressionProbeController : ControllerBase
{
    [HttpGet("unmarked")]
    public IActionResult Unmarked() => Ok(new { name = "unmarked" });

}
