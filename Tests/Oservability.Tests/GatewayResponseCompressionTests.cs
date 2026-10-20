using System.IO.Compression;
using System.Net;
using System.Text.Json;
using Core.Web.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Xunit;

namespace Oservability.Tests;

public sealed class GatewayResponseCompressionTests
{
    [Theory]
    [InlineData("gzip")]
    [InlineData("br")]
    [InlineData(null)]
    public async Task Ocelot_preserves_negotiated_encoding_bytes_and_vary(string? encoding)
    {
        var backendBuilder = WebApplication.CreateBuilder();
        backendBuilder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        backendBuilder.Services.AddHrkResponseCompression(backendBuilder.Configuration);
        await using var backend = backendBuilder.Build();
        backend.UseRouting();
        backend.UseHrkResponseCompression();
        var events = Enumerable.Range(0, 4000).Select(sequence => new { sequence, name = "Chiêu mộ tướng", damage = 123 }).ToArray();
        backend.MapGet("/replay", () => Results.Json(new { success = true, data = events }))
            .WithMetadata(new HrkResponseCompressionAttribute());
        await backend.StartAsync();
        var port = new Uri(backend.Urls.Single()).Port;

        var gatewayBuilder = WebApplication.CreateBuilder();
        gatewayBuilder.WebHost.UseTestServer();
        gatewayBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Routes:0:DownstreamPathTemplate"] = "/replay",
            ["Routes:0:DownstreamScheme"] = "http",
            ["Routes:0:DownstreamHostAndPorts:0:Host"] = "127.0.0.1",
            ["Routes:0:DownstreamHostAndPorts:0:Port"] = port.ToString(),
            ["Routes:0:UpstreamPathTemplate"] = "/proxy",
            ["Routes:0:UpstreamHttpMethod:0"] = "Get"
        });
        gatewayBuilder.Services.AddOcelot(gatewayBuilder.Configuration);
        await using var gateway = gatewayBuilder.Build();
        await gateway.UseOcelot();
        await gateway.StartAsync();
        using var client = gateway.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/proxy");
        if (encoding is not null) request.Headers.TryAddWithoutValidation("Accept-Encoding", encoding);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Assert.Contains("Accept-Encoding", response.Headers.Vary);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        if (encoding is not null)
        {
            Assert.Equal(encoding, response.Content.Headers.ContentEncoding.Single());
            using var input = new MemoryStream(bytes);
            using Stream decoder = encoding == "gzip" ? new GZipStream(input, CompressionMode.Decompress) : new BrotliStream(input, CompressionMode.Decompress);
            using var decoded = new MemoryStream();
            await decoder.CopyToAsync(decoded);
            bytes = decoded.ToArray();
        }
        else Assert.Empty(response.Content.Headers.ContentEncoding);
        var json = JsonSerializer.Deserialize<JsonElement>(bytes);
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.Equal(4000, json.GetProperty("data").GetArrayLength());
        Assert.Equal("Chiêu mộ tướng", json.GetProperty("data")[0].GetProperty("name").GetString());
    }
}
