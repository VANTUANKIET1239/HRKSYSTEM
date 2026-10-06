# HRK logging và observability

Implementation: 03/10/2026. Library dùng tên `Oservability` theo yêu cầu.

## Thành phần đã nối

- Gateway, Auth, Game, Realtime và Activity host dùng `AddHrkObservability`.
- ILogger đi qua Serilog đến structured JSON console và Seq sink khi cấu hình URL. Seq sink gửi batch bất đồng bộ; mất kết nối không làm request gameplay thất bại. Buffer memory có giới hạn mặc định của sink; log có thể mất khi buffer đầy hoặc process bị kill.
- TraceId/SpanId được lấy từ Activity.Current bởi enricher; ILogger không tạo span riêng cho mỗi log.
- HTTP middleware validate/generate X-Correlation-Id, canonicalize request header, trả response header và tạo LogContext. HttpClientFactory forward correlation; W3C tracing do ASP.NET/HttpClient instrumentation phụ trách.
- HTTP/SQL instrumentation; Redis telemetry attach đúng cache multiplexer của Game và SignalR backplane của Realtime khi connection được tạo. Redis instrumentation package hiện ở beta; không thu verbose commands/keys.
- GameOperation cung cấp custom span và metrics `game.operation.count`, `game.operation.duration`, tags operation/result giới hạn. Đã nối battle API và enhancement thực thi. Rabbit metrics đếm attempt processed/failed và consumer duration; không dùng ID làm metric tags.
- Rabbit producer/consumer dùng W3C AMQP headers, scoped MessageContext, CorrelationContext và structured logging. Retry/DLQ giữ MessageId/CorrelationId/trace headers.
- Outbox lưu CorrelationId/TraceParent/TraceState riêng với PartitionKey. Worker phục hồi context nguồn khi dispatch.
- UnitOfWork log Save/Commit ở Debug; vượt Database:SlowTransactionMilliseconds (mặc định 1000) dùng Warning. Không dump entities/SQL/connection strings hoặc log Error cùng exception ở mỗi tầng.
- ActivityService ghi timeline SQL với Inbox trong cùng transaction, ACK sau handler commit. API `/api/activities` và `/api/activities/{activityId}` yêu cầu JWT có role Admin và audience `activity-api`, phân trang tối đa 100. API chưa mở qua Gateway, không có API ghi activity công khai.
- Game phát summary ItemEnhanced và QuickClimbStarted/Completed/Failed/Cancelled qua Outbox. History gốc giữ nguyên ở Game. Không copy toàn bộ replay/history sang Activity.

## Migration trước khi chạy

Không tự động migrate database hiện hữu khi startup. Dừng worker cũ khi rollout schema thay đổi.

1. Game DB: chạy script messaging hiện hữu nếu chưa có Outbox/Inbox, rồi `Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_OutboxObservability.sql`. Script thêm ba cột nullable và có thể chạy lại. Message cũ không có trace context vẫn dispatch được.
2. Trên cùng SQL Server của Game/Auth, chạy `Services/HRKACTIVITY/Activity.Infrastructure/Scripts/CreateActivityDatabase.sql` bằng SSMS hoặc sqlcmd với tài khoản có quyền tạo database. Script tạo `HRK_Activity`, các bảng Activity/Inbox và map login `sa1` hiện có sang database mới với quyền đọc/ghi. Có thể chạy lại script. Không tạo password mới hoặc chuyển history Game.
3. Start Activity consumer/topology trước khi Game phát activity đầu tiên: topic exchange không lưu message khi chưa có queue binding. Đảm bảo queue timeline được tạo trước production rollout.
4. Khởi động các host mới. Nếu rollback binary, giữ các cột nullable mới.

Inbox Activity chưa bật cleanup tự động: tránh replay cũ bị ghi lặp khi tùy tiện xóa deduplication records. ActivityId cũng có unique index. Retention/archival của timeline và Inbox cần được quyết định cùng thời gian replay tối đa; không xóa history Game chỉ vì Seq hết retention.

## Cấu hình host

```json
{
  "Seq": { "ServerUrl": "http://localhost:5340" },
  "Telemetry": {
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://localhost:4318",
      "Protocol": "HttpProtobuf"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  }
}
```

Trong Docker: Seq URL `http://seq:5341`, OTLP endpoint `http://otel-collector:4318`. Có thể dùng `OTEL_EXPORTER_OTLP_ENDPOINT` và `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf` thay cho Telemetry:Otlp:Endpoint/Protocol; cấu hình Telemetry explicit được ưu tiên. Bật exporter bằng `Telemetry__Otlp__Enabled=true`. Console logging luôn bật; Seq URL trống và OTLP disabled là default để các host vẫn chạy độc lập.

API keys/secrets dùng environment hoặc secret manager: `Seq__ApiKey`, `JwtSettings__SecretKey`, `Database__ConnectionString` (Activity vẫn hỗ trợ `ConnectionStrings__Activity` để tương thích). Không commit giá trị thật. Cấu hình Serilog MinimumLevel riêng cho Debug khi troubleshooting, không bật EF sensitive data logging. Log HTTP mặc định ghi ApiStarted/ApiEnded, chỉ ghi path, không ghi query `access_token` hay Authorization; body chỉ bật theo allowlist ở Debug. Cấu hình và vòng đời: [API logging](api-logging.md).

## Docker local

Compose bổ sung nằm ở `docker-compose.observability.yml`; không tự động bật khi dùng compose cũ. Chuẩn bị các biến trong local `.env`:

```dotenv
SEQ_ADMIN_PASSWORD_HASH=<hash-created-by-seq>
SEQ_INGESTION_API_KEY=<optional-local-ingestion-key>
ACTIVITY_DB_CONNECTION_STRING='Server=host.docker.internal,1433;Database=HRK_Activity;User Id=<game-sql-login>;Password=<game-sql-password>;Encrypt=True;TrustServerCertificate=True;'
JWT_SECRET_KEY=<shared-jwt-signing-secret>
```

Tạo password hash bằng `docker run --rm -it datalust/seq:2025.2 config hash`; nhập password qua STDIN theo hướng dẫn CLI, không đưa password vào command history. Seq có thể yêu cầu cờ `-i` tùy cách dùng shell. Biến FIRSTRUN chỉ áp dụng khi khởi tạo volume lần đầu; đổi biến không đổi password của volume đã tồn tại.

```powershell
docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d seq otel-collector jaeger prometheus
```

Collector có memory limiter/batch, export traces đến Jaeger và metrics cho Prometheus scrape. Jaeger trong cấu hình local lưu trace trong memory và mất dữ liệu khi restart; production cần backend trace có persistent storage. Prometheus giữ 14 ngày và volume riêng. Seq giữ volume `/data`.

Activity dùng cùng SQL Server, port và login/password với Game/Auth, chỉ đổi database thành `HRK_Activity`; không cần container SQL riêng. Khi chạy trực tiếp dùng cấu hình `Database` trong appsettings; trong Docker dùng `ACTIVITY_DB_CONNECTION_STRING` từ `.env` với host `host.docker.internal`. Kết nối SSMS bằng thông tin SQL của Game/Auth và chạy `CreateActivityDatabase.sql`. Sau khi database/schema sẵn sàng:

```powershell
docker compose -f docker-compose.yml -f docker-compose.observability.yml --profile activity up -d --build hrk.activity
docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d --build apigateway hrk.auth hrk.game hrk.realtime
```

Game dùng connection SQL hiện có của dự án, không bị đổi sang Activity database. Cần chạy migration Game trước khi khởi động worker mới.

UI local: Seq `http://localhost:5340`, Jaeger `http://localhost:16686`, Prometheus `http://localhost:9090`, Activity API `http://localhost:5004`. Ingestion/Collector không publish ra Internet. Khi chạy host bằng Visual Studio/dotnet trên máy, dùng Seq `http://localhost:5340` và OTLP `http://localhost:4318`; Collector đã publish port 4318 chỉ trên localhost. Biến môi trường của Compose không tự áp dụng cho process chạy từ Visual Studio; đặt `Seq__ServerUrl`, `Telemetry__Otlp__Enabled`, `Telemetry__Otlp__Endpoint` và `Telemetry__Otlp__Protocol` trong launch profile của từng host.

## Tra cứu

Seq: lọc `ServiceName = 'HRK.GAME'`, `CorrelationId = '...'`, `TraceId = '...'`, `MessageId = '...'` hoặc PlayerId nếu đã resolve nhân vật. UserId là tài khoản và PlayerId là nhân vật, không đổi tên thay thế cho nhau.

Jaeger: chọn service và trace. CorrelationId có thể nhóm nhiều trace qua job/replay; retry tiếp tục parent có thể giữ cùng TraceId. MessageId là khóa deduplication độc lập.

Activity API: `GET /api/activities?playerId=42&page=1&pageSize=50`, hoặc filter correlationId/entityType/entityId. Chỉ Admin được đọc ở implementation đầu tiên; chưa có endpoint tự phục vụ player. Không log payload activity hoặc nội dung battle replay vào Seq.

## Logging RabbitMQ

Warning khi retry được publish thành công, Error khi hết retry/permanent và chuyển DLQ, Debug khi duplicate/success từng message. Failed retry/DLQ transfer không ACK original như xử lý thành công; NACK requeue để recovery. Nếu handler đã commit nhưng ACK lỗi, Warning và dựa Inbox chống lặp khi redelivery. Raw exception.Message không được ghi vào broker error header hoặc Outbox LastError; exception đầy đủ vẫn được log ở boundary, vì vậy exception do business code tạo cần tránh chứa secrets.

## Production và phần mở rộng

- Reverse proxy HTTPS `logs.example.com` đến Seq UI trên network riêng, bật authentication, tạo ingestion-only API key và yêu cầu authentication ingestion. Không public port ingestion/OTLP.
- Retention Seq: đề xuất Debug 3 ngày, Information 14 ngày, Warning/Error 60 ngày; phải cấu hình retention thực tế trong Seq theo dung lượng VPS. Những giá trị này chưa được tự động provisioning.
- Không readiness-check Seq/Collector để quyết định gameplay healthy. Activity `/health/live` kiểm tra process; `/health/ready` kiểm tra DB. Consumer khởi tạo Rabbit topology khi startup.
- Host quản lý dispose Serilog/OpenTelemetry; shutdown có giới hạn thời gian. Không bảo đảm flush khi SIGKILL; message chưa ACK được Rabbit redeliver.
- Chưa thêm gRPC. Chưa phát event từ Auth login, content/admin, reward hoặc toàn bộ currency mutations: cần transaction/ledger/audit ownership tương ứng trước khi nối. Library và timeline contract sẵn sàng cho các event mới; enum allowlist cần mở rộng khi thêm contract.
- Outbox pending age/backlog dashboard, archival jobs và admin audit transaction cần triển khai theo nghiệp vụ thực tế; chúng không được giả lập bằng technical log.

## Tests

```powershell
dotnet test Tests/Oservability.Tests/Oservability.Tests.csproj
dotnet test Tests/dcs-game/GAME.Domain.Tests/GAME.Domain.Tests.csproj
$env:HRK_RUN_INTEGRATION = '1'
dotnet test Tests/Oservability.Tests/Oservability.Tests.csproj
```

Integration tests chạy SQL Server, RabbitMQ, Seq, Collector và Jaeger trong containers tạm, không dùng database/container đang chạy của game; tự dispose containers. Lần đầu có thể cần tải images. Tests kiểm tra correlation isolation, logger-only enrichment, W3C propagation, Outbox SQL mapping/claim/publish, transaction rollback trước commit, duplicate/ACK, permanent failure/DLQ, Seq properties/token exclusion và exporter traces/metrics. Testcontainers authentication-disabled Seq chỉ dùng trong container test tạm; Compose thực tế yêu cầu admin password hash.

Tài liệu nền: [Serilog ASP.NET Core](https://github.com/serilog/serilog-aspnetcore), [Seq Docker](https://datalust.co/docs/getting-started?platform=docker), [Redis instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.StackExchangeRedis).

## Kết quả kiểm chứng 03/10/2026

- Build cả 5 host: Gateway, Auth, Game, Realtime, Activity thành công.
- 13 observability tests pass, không skip ở lần chạy cuối (bao gồm SQL Server/RabbitMQ, Seq, Collector/Jaeger và Ocelot propagation).
- 277 Game tests hiện hữu pass.
- Compose base + observability override + activity profile qua `config --quiet`; `git diff --check` không có lỗi whitespace.
- Còn các warning có sẵn của Core.Common (duplicate JWT PackageReference) và nullable ở source hiện hữu. Không có build error.
- Testcontainers tự dọn các containers test. Chưa chạy migrations lên Game DB hiện hữu, chưa deploy stack production và chưa thay đổi secrets của người dùng.
