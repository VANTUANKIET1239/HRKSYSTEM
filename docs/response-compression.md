# HTTP response compression

The reusable implementation lives in `CoreEngine/Core.Web/Compression`. GAME opts in only the actions returning battle logs/replays. No DTO, CQRS handler, database script, or Angular response contract changes are required.

## Register in another API

```csharp
using Core.Web.Compression;

builder.Services.AddHrkResponseCompression(builder.Configuration);

// Keep the exception/status-code response pipeline outside compression.
app.UseHrkApiResponses();
app.UseRouting();
app.UseHrkResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Routing must run before compression so endpoint metadata is available. Do not also register a second `UseResponseCompression()` around these endpoints.

```csharp
[HttpGet("logs")]
[HrkResponseCompression("BattleReplay")]
public async Task<IActionResult> Logs(CancellationToken ct)
{
    return Ok(await mediator.Send(new GetLogsQuery(), ct));
}
```

The attribute also works on a controller. An action can override a controller policy or disable it with `[HrkResponseCompression(Enabled = false)]`. `[HrkResponseCompression]` uses the built-in `Default` policy. A named policy that does not exist is a configuration error; it is not silently ignored. Minimal endpoints can use `.WithMetadata(new HrkResponseCompressionAttribute("BattleReplay"))`.

## Configuration

GAME's `appsettings.json` contains:

```json
{
  "ResponseCompression": {
    "Enabled": true,
    "EnableForHttps": true,
    "Policies": {
      "BattleReplay": {
        "Enabled": true,
        "AllowedEncodings": ["br", "gzip"],
        "PreferredEncoding": "br",
        "Level": "Fastest",
        "MinimumBodyBytes": 16384,
        "SlowCompressionMilliseconds": 100
      }
    }
  }
}
```

| Setting | Behavior |
| --- | --- |
| `Enabled` | Global or per-policy switch. |
| `EnableForHttps` | Explicit HTTPS opt-in. The library default is `false`; GAME enables it for these JSON replay endpoints. |
| `AllowedEncodings` | `gzip`, `br`, or both. Omitted means both; configure `PreferredEncoding` to match if allowing only one. |
| `PreferredEncoding` | Server preference when client quality values tie. Client `q` values take precedence. |
| `Level` | `Fastest`, `Optimal`, `SmallestSize`. GAME starts with `Fastest`. |
| `MinimumBodyBytes` | Serialized body byte threshold, including the normalized API envelope. At or above this value compression is eligible. Range: 0–1048576. |
| `SlowCompressionMilliseconds` | Logs an informational event for a slow compression phase. `0` disables this logging. |

Options are validated on startup and when resolved after configuration reload. `IOptionsMonitor` picks up provider reloads; an in-flight response keeps its own policy snapshot. This version reads application configuration, not a database or Redis.

The slow threshold is **not a compression timeout**. It measures elapsed time from starting the compressor to finishing its output, including the remaining serialization and downstream IO. It is not a measurement of CPU time alone. Request cancellation propagates to body writes. Configure any whole-request timeout separately; switching to raw JSON after compressed bytes have been sent would corrupt the response.

## Wire behavior

1. The client sends `Accept-Encoding: gzip, br`.
2. The server negotiates an allowed algorithm, including quality values, wildcards and explicit exclusions (`q=0`). An explicitly higher `identity` preference keeps the response uncompressed.
3. The normal response filters finish constructing `success`, `statusCode`, `traceId`, `data`, etc. MVC serializes that result once.
4. A bounded prefix accumulates until the size threshold is reached. Larger bodies stream through ASP.NET Core's standard response compression middleware; the full response is never buffered by this library.
5. Compressed responses have a real compressed body and matching `Content-Encoding`. ASP.NET Core clears the original `Content-Length` and `Content-MD5`. `Content-Type` remains JSON.
6. `Vary: Accept-Encoding` is added for enabled opt-in responses, including identity responses and requests missing `Accept-Encoding`, while preserving other Vary values.
7. Browsers decode automatically. Angular continues receiving the same JSON `BaseResponse`; do not add request `Content-Encoding` or manually decompress/base64-decode the response.

Normally a body below the threshold is uncompressed. If the client explicitly rejects `identity` but accepts a configured compressor, even a smaller eligible JSON response uses that compressor. If neither an allowed compressor nor identity is acceptable, the endpoint is not executed and HTTP 406 is returned through the existing status-code response pipeline.

The library compresses successful JSON (`application/json` or a `+json` media type). Errors, HEAD, range requests/responses, 204/205, downloads, non-JSON/SSE, `Cache-Control: no-transform`, and already encoded responses are skipped. An exception before the prefix is committed discards that prefix and restores the original body feature, allowing the exception handler to write its normal error envelope.

JSON formatter `BodyWriter.FlushAsync()` checkpoints may retain bytes below the bounded threshold. Explicit `Response.StartAsync()`, direct `Response.Body.FlushAsync()`, or `IHttpResponseBodyFeature.DisableBuffering()` bypasses the size wait and commits identity if compression has not already been selected (unless identity was explicitly rejected). Use these APIs when immediate output is required. Do not opt SSE or other latency-sensitive streaming endpoints into this JSON policy.

## GAME integration

`BattleReplay` is applied to:

- `POST /api/battle/start`
- `GET /api/battle/logs`
- `POST /api/dungeon[s]/stages/{stageId}/start`
- `POST /api/events/tower/floors/{floorNumber}/start`
- `GET /api/events/tower/battles/{battleId}`
- `POST /api/battle/lab/run`

Compression occurs in GAME, where controller metadata is available. Ocelot is a pass-through for the negotiated bytes and headers. Integration tests exercise Ocelot 24 with a real Kestrel backend for Gzip, Brotli, and identity. Hosting proxies outside this repository should preserve the same headers and avoid a second compression pass.

## Verification

```powershell
dotnet test Tests/Core.Modules.Tests/Core.Modules.Tests.csproj --filter FullyQualifiedName~ResponseCompressionTests
dotnet test Tests/Oservability.Tests/Oservability.Tests.csproj --filter FullyQualifiedName~GatewayResponseCompressionTests
```

The tests inspect raw bytes and decompress them, check negotiation, UTF-8 size boundaries, streamed writes and JSON formatter checkpoints, the normalized API envelope, error handling, cancellation, feature restoration, exclusion rules, configuration binding/validation, and gateway transport. The 16 KiB and 100 ms settings are starting values for measurement, not benchmark-derived limits.

For a manual check against an authenticated game request, inspect Network → response headers in browser DevTools. Above the threshold, `Content-Encoding` should be `br` or `gzip`; the Preview tab still shows ordinary JSON. Compare transferred size with decoded size. Small responses can legitimately have no `Content-Encoding`.

References: [ASP.NET Core response compression](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-9.0), [Accept-Encoding semantics](https://www.rfc-editor.org/rfc/rfc9110.html#name-accept-encoding).
