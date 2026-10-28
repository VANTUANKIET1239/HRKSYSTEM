# Core.InternalApi

Reusable .NET 9 REST client library for service-to-service calls. Inject
`IInternalApiClient` and pass a service name and API URL directly. Endpoints do
not need registration. No database, gRPC, protobuf or operation registry is required.

```csharp
services.AddInternalApi(configuration);

// Inject IInternalApiClient into a handler/service:
var player = await client.GetAsync<PlayerResponse>(
    "HRK.GAME", $"/internal/players/{playerId}",
    new InternalCallOptions { Authorization = $"Bearer {accessToken}" }, cancellationToken: ct);

var result = await client.PostAsync<GrantRewardRequest, GrantRewardResponse>(
    "HRK.GAME", "/internal/rewards", request,
    new InternalCallOptions { Authorization = $"Bearer {accessToken}" }, cancellationToken: ct);
```

```json
{
  "InternalApi": {
    "Services": {
      "HRK.GAME": {
        "BaseAddress": "https://hrk-game/",
        "TimeoutSeconds": 5,
        "Retry": {
          "Enabled": true,
          "MaxRetries": 2,
          "RetryDelayMilliseconds": 200
        }
      }
    }
  }
}
```

Supports relative paths and absolute URLs with the same scheme, host and port as
the configured service. Metadata, correlation IDs and service credentials are
resolved per call. Uses `IHttpClientFactory` + `Microsoft.Extensions.Http.Resilience`
(Polly) for connection pooling, bounded retry and a circuit breaker per service.
Configuration changes require restart.

For health probes that return JSON with HTTP 503, use
`GetResponseAsync<ServiceHealthReport>(service, "/health/dependencies", [HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable], options, ct)`.
The result contains `StatusCode` and the deserialized `Body`. Unaccepted non-success
statuses still throw `InternalApiException`; existing `GetAsync` behavior is unchanged.
Set `Retry.MaxRetries = 0`, `Retry.Enabled = false` and
`CircuitBreaker.Enabled = false` for monitoring clients so every probe makes a fresh request.

`Retry.Enabled` applies by default only to GET/HEAD/OPTIONS. Other methods do not
retry automatically. Explicit `InternalCallOptions.RetryEnabled = true` can opt a
call in; writes also require `IdempotencyKey` and receiver-side transactional
deduplication. Per-call options can override timeout and disable retry. A timeout
does not prove that a remote write was rolled back.

For HRK correlation integration use `services.AddHrkInternalApi(configuration)`
from `Oservability` instead of `AddInternalApi`. See `docs/internal-api.md` in the
HRK repository for details and migration notes.

Authorization can be supplied per call as above, or through service configuration:
`InternalApi__Services__HRK.GAME__Authorization="Bearer <valid-token>"`.
The default `ConfiguredInternalApiCredentialsProvider` reads that configured value;
per-call Authorization takes precedence. A custom `IInternalApiCredentialsProvider`
can acquire/refresh tokens dynamically. The library sends credentials, it does not
issue tokens or bypass authentication/authorization at the destination.
