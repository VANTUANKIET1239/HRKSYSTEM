# Internal REST API giữa các service

`CoreEngine/Core.InternalApi` là thư viện .NET 9 độc lập để gọi REST giữa các service.
Caller truyền service name và đường dẫn/URL trực tiếp. Không cần đăng ký từng API.
Thư viện không chứa nghiệp vụ, database hay gRPC/protobuf.

## Đăng ký DI và cấu hình

GAME, AUTH, ACTIVITY và REALTIME đã gọi `AddHrkInternalApi(configuration)` trong
dependency custom. Extension này bổ sung correlation ID từ context của HRK.
Ứng dụng ngoài HRK dùng `AddInternalApi(configuration)` trực tiếp, không cần phụ
thuộc `Oservability`. Chỉ gọi một trong hai extension, một lần/service provider.

Config mặc định của các host là `InternalApi:Services` rỗng. Khi cần gọi một service,
chỉ thêm cấu hình service đích:

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
        },
        "CircuitBreaker": {
          "Enabled": true,
          "FailureRatio": 0.5,
          "MinimumThroughput": 10,
          "SamplingDurationSeconds": 30,
          "BreakDurationSeconds": 5
        }
      }
    }
  }
}
```

Cấu hình được chụp và validate khi đăng ký. Thay đổi cần restart service.
Địa chỉ phải là HTTP(S), không chứa credentials, query hay fragment. Timeout phải
từ 0.01 đến 3600 giây. Retry mặc định tắt, tối đa 2 retry và delay 200 ms.
`MaxRetries` cho phép 0-4; nếu `Retry.Enabled = true` phải lớn hơn 0.

## Gọi bằng đường dẫn hoặc URL

Inject `IInternalApiClient` vào handler/service:

```csharp
var player = await internalApi.GetAsync<PlayerResponse>(
    serviceName: "HRK.GAME",
    api: $"/internal/players/{playerId}",
    options: new InternalCallOptions { Authorization = $"Bearer {accessToken}" },
    cancellationToken: ct);

var result = await internalApi.PostAsync<GrantRewardRequest, GrantRewardResponse>(
    serviceName: "HRK.GAME",
    api: "/internal/rewards",
    request: request,
    options: new InternalCallOptions { Authorization = $"Bearer {accessToken}" },
    cancellationToken: ct);
```

Thêm API mới chỉ cần thêm lời gọi tương ứng; không sửa DI hay mapping. Endpoint
nghiệp vụ phải được triển khai ở service đích. Các tên `PlayerResponse`,
`GrantRewardRequest` và URL trên là ví dụ, chưa phải endpoint có sẵn của GAME.

Service name được lookup không phân biệt hoa thường. `api` có thể là:

- `/internal/players/1`: tính từ root của host.
- `players/1`: resolve theo `BaseAddress`; dùng địa chỉ kết thúc `/` nếu có base path,
  ví dụ `https://hrk-game/internal/` + `players/1`.
- `https://hrk-game/internal/players/1`: URL đầy đủ với cùng scheme/host/port của service.

Không gửi credentials của một service tới URL thuộc service khác. URL chứa user-info,
network path (`//host`), backslash, fragment hay ký tự điều khiển bị từ chối.
Redirect tự động và cookie được tắt. Giá trị path/query phải được escape tại caller,
ví dụ `Uri.EscapeDataString(name)`.

Ngoài GET/POST có `PutAsync<TRequest,TResponse>`, `PatchAsync<TRequest,TResponse>`,
`DeleteAsync<TResponse>` và phương thức tổng quát:

```csharp
await internalApi.SendAsync<InternalApiEmpty>(
    "HRK.GAME", HttpMethod.Head, "/internal/status", cancellationToken: ct);
```

Request body và response dùng System.Text.Json web defaults. Response giữ nguyên
schema/envelope của service; thư viện không tự bóc `data` hay diễn giải lỗi nghiệp vụ
trong HTTP 200. Response không có body dùng `InternalApiEmpty`. Chưa hỗ trợ file
upload hoặc streaming response.

## Timeout và retry

Client dùng `IHttpClientFactory` và `Microsoft.Extensions.Http.Resilience` (Polly).
Mỗi service có một named HTTP client và circuit breaker dùng chung cho mọi URL,
không tạo client/pipeline mới cho từng endpoint hoặc query string.

- `Retry.Enabled` trong config chỉ tự áp dụng cho GET/HEAD/OPTIONS.
- POST/PUT/PATCH/DELETE và các method khác mặc định không retry, kể cả khi đã truyền key.
- `InternalCallOptions.RetryEnabled = false` tắt retry cho một lời gọi.
- `InternalCallOptions.RetryEnabled = true` bật retry riêng cho một lời gọi, dùng số lần/delay
  từ config service; nếu `MaxRetries = 0` sẽ báo lỗi sử dụng API.
- Method thay đổi dữ liệu muốn retry phải bật rõ ràng và truyền `IdempotencyKey`.

Ví dụ chỉ khi endpoint nhận thưởng đã hỗ trợ chống thực hiện trùng:

```csharp
var result = await internalApi.PostAsync<GrantRewardRequest, GrantRewardResponse>(
    "HRK.GAME", "/internal/rewards", request,
    new InternalCallOptions
    {
        RetryEnabled = true,
        IdempotencyKey = rewardOperationId,
        TimeoutSeconds = 3
    },
    cancellationToken: ct);
```

Bên nhận phải lưu key, hash request và kết quả trong cùng transaction nghiệp vụ,
có unique constraint/locking để chống request trùng chạy đồng thời. Cùng key khác
payload phải bị từ chối. Giữ nguyên key khi job/message gọi lại. Thư viện client chỉ
truyền key qua header `idempotency-key`, không tự tạo bảng deduplication.

Retry áp dụng cho `HttpRequestException` và HTTP 408/429/500/502/503/504. Có exponential
backoff, jitter và đọc `Retry-After`. `MaxRetries = 2` nghĩa là tối đa 3 lần gọi.
Không retry lỗi dữ liệu/quyền truy cập hay lỗi deserialize response thành công.
HTTP method chỉ là quy ước: endpoint GET vẫn phải thực sự chỉ đọc để retry an toàn.

Timeout tổng bao gồm lấy metadata, credentials, serialize request, các lần thử,
chờ retry và đọc response. `InternalCallOptions.TimeoutSeconds` override timeout service.
Truyền `CancellationToken`; các provider phải tuân thủ token nhận được.
Client timeout không chứng minh transaction bên nhận thất bại.

Thư viện gỡ HTTP resilience handler kế thừa từ global defaults trên client của mình,
giữ các handler khác như correlation. Tránh thêm lớp retry bên ngoài cùng lời gọi.
Nếu caller là Quartz job hoặc RabbitMQ consumer có retry, cần tính cả số lần ở đó.

## Metadata, correlation, trace và credentials

`InternalCallOptions.Metadata` truyền metadata ứng dụng dưới dạng header `x-*`,
ghi đè cùng key từ `IInternalApiMetadataProvider`. Tối đa 32 trường/tổng 8 KB, mỗi
giá trị tối đa 1024 ký tự ASCII in được. Không tự copy header của người dùng.

`AddHrkInternalApi` lấy correlation ID từ `ICorrelationContext` hiện tại. Caller có
thể override bằng `InternalCallOptions.CorrelationId`. Khi chưa có, client dùng
Activity trace ID hoặc tạo correlation ID mới. Correlation/idempotency ID tối đa
128 ký tự gồm chữ ASCII, số và `- _ . :`.

Trace context chuẩn được .NET/HTTP instrumentation truyền tự động khi có Activity.
Không tự lấy TraceId rồi ghi vào `traceparent`; background job cần Activity phù hợp
nếu muốn liên kết lời gọi vào trace của job.

API đích có `[Authorize]` cần token hợp lệ. Có hai cách cung cấp header Authorization:

```csharp
// Token lấy từ luồng xác thực hiện có; chỉ chuyển tiếp khi có chủ đích.
var player = await internalApi.GetAsync<PlayerResponse>(
    "HRK.GAME", $"/internal/players/{playerId}",
    new InternalCallOptions { Authorization = $"Bearer {accessToken}" },
    cancellationToken: ct);
```

Hoặc cấu hình authorization mặc định cho từng service bằng secret/environment:

```text
InternalApi__Services__HRK.GAME__Authorization=Bearer <valid-token>
```

`ConfiguredInternalApiCredentialsProvider` là implementation mặc định của
`GetAuthorizationAsync`: đọc `InternalServiceOptions.Authorization` theo service đích.
Thứ tự ưu tiên là `InternalCallOptions.Authorization` → credentials provider → không
gửi header nếu cả hai không có giá trị. Header chỉ gắn vào request hiện tại, không
lưu vào default headers dùng chung. Dictionary metadata không nhận Authorization.

Thư viện không tự phát hành JWT hay tự gia hạn token cấu hình. Khi token hết hạn,
truyền token mới ở lời gọi tiếp theo hoặc cập nhật cấu hình rồi restart. Nếu cần lấy/
refresh token tự động, đăng ký `IInternalApiCredentialsProvider` scoped riêng để tích
hợp với issuer của hệ thống; token được lấy trong timeout tổng của lời gọi.

Không tự chuyển JWT của người chơi. Endpoint đích vẫn kiểm tra chữ ký, issuer,
audience, hạn token, role và quyền nghiệp vụ hiện có. Token người chơi và service
credential có mục đích/claims khác nhau; không tự cấp quyền Admin hoặc giả danh người
chơi cho service. Không có token hoặc token sai vẫn nhận HTTP 401; token không đủ
quyền vẫn có thể nhận 403. Dùng HTTPS khi triển khai và cung cấp credential thực qua
secret/environment thay vì commit vào appsettings.

## Lỗi, migration và kiểm thử

`InternalApiException` có `ServiceName`, `Api`, `Failure`, `HttpStatusCode`.
Phân biệt HTTP error, unavailable, timeout, circuit open và response JSON không hợp lệ.
Caller cancel vẫn nhận `OperationCanceledException`. Sai config, URL hay metadata
là lỗi sử dụng API. Log của thư viện không ghi URL/query, payload, metadata, token
hay remote response body; `Api` trên exception vẫn chứa giá trị caller đã truyền.

Migration từ thiết kế trước:

- Bỏ callback đăng ký operation trên `AddInternalApi`/`AddHrkInternalApi`.
- Đổi `RestAddress` thành `BaseAddress`; bỏ `Protocol`, `GrpcAddress`, `Operations`.
- Chuyển policy retry sang `Retry` ở cấp service và options ở từng lời gọi.
- Đổi `CallAsync(service, operation, request)` sang GET/POST/... với URL trực tiếp.
- Đã gỡ gRPC channel, mapping, package client, protobuf và các test gRPC.

```powershell
dotnet test Tests/Core.Modules.Tests/Core.Modules.Tests.csproj --filter FullyQualifiedName~InternalApiTests
dotnet test Tests/Oservability.Tests/Oservability.Tests.csproj --filter FullyQualifiedName~InternalApiContextTests
dotnet pack CoreEngine/Core.InternalApi/Core.InternalApi.csproj -c Release
```

Kiểm thử dùng ASP.NET TestServer, không cần database hay mạng ngoài. Bao gồm gọi URL
trực tiếp, HTTP verbs/body, retry opt-in/opt-out và giới hạn số lần, timeout/cancel,
metadata/credentials, URL hợp lệ, lỗi response và circuit breaker theo service.
