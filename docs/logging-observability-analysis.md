# Phân tích áp dụng logging và observability vào HRK

Ngày phân tích: 02/10/2026. Phạm vi: source hiện tại và nội dung prompt đính kèm. Đây là bản phân tích và thiết kế triển khai; chưa thay đổi runtime, database hay Docker.

Cập nhật 03/10/2026: đã bắt đầu triển khai thiết kế này vào source. Trạng thái implementation, cấu hình và các bước migration nằm trong [runbook](observability-runbook.md); phần dưới giữ lại đối chiếu baseline trước implementation.

## 1. Kết luận kiến trúc

Áp dụng được kiến trúc trong prompt, nhưng cần điều chỉnh theo repository. Các ứng dụng hiện tại là ApiGateway, HRK.AUTH, HRK.GAME và HRK.REALTIME; project đang target .NET 9, không phải .NET 10. Inventory, battle và rewards hiện nằm trong Game, không nên tách thêm microservice chỉ để triển khai logging.

Luồng đề xuất:

```text
Gateway / Auth / Game / Realtime
  ILogger<T> -> Serilog -> Console + Seq
  Activity / Meter -> OpenTelemetry -> OTLP Collector -> traces/metrics backend

Game DB transaction
  business mutation + owning history + Outbox
    -> RabbitMQ -> ActivityService -> Activity DB + Inbox
```

Seq và telemetry exporter không thuộc điều kiện readiness của gameplay. ActivityService không nhận ILogger messages, không thay thế inventory, economy hoặc battle storage.

## 2. Đối chiếu source

| Hạng mục | Hiện trạng | Hướng áp dụng |
|---|---|---|
| Technical logging | `CoreEngine/Core.Common/Logging/LoggingService.cs` bọc `ILogger<T>`; chưa thấy package Serilog | Giữ tương thích wrapper; cấu hình provider chung cho cả 4 host |
| Host integration | 4 `Program.cs` chưa cấu hình observability chung | Gọi extension dùng chung tại startup |
| Correlation HTTP | Chưa thấy middleware/context dùng chung | Sinh/đọc/validate header, trả response header, tạo scope |
| Messaging metadata | `RabbitPublishMetadata` có MessageId, CorrelationId và Headers | Bổ sung propagation W3C; giữ payload V1 tương thích |
| Publisher | `RabbitMqPublisher.cs` ghi metadata vào AMQP properties | Tạo producer span; inject traceparent/tracestate |
| Consumer | Có manual ACK, retry buckets, DLQ và clone headers | Extract context, tạo consumer span, scope và metrics theo delivery |
| Outbox | Có writer, SQL store, worker, locking và retry | Tái sử dụng; thêm CorrelationId/TraceParent/TraceState riêng |
| Inbox | Có SQL inbox và executor giao dịch | Tái sử dụng cho ActivityService; xác minh bằng integration tests |
| Battle history | `GameDbContext.BattleLogs`, bảng `HRK_BattleLogs` | Giữ trong Game; Seq chỉ log milestone và lỗi |
| Enhancement history | `EquipmentEnhancementHistories`, bảng `HRK_EquipmentEnhancementHistory` | Giữ trong Game, phát event sau khi ghi vào cùng transaction |
| ActivityService | Chưa có trong cây project đã kiểm tra | Bổ sung sau khi pipeline technical logging và messaging hoàn chỉnh |
| Docker | Đã có Redis/RabbitMQ và 4 ứng dụng; chưa có Seq/Collector trong compose chính | Thêm hạ tầng observability, volumes, cấu hình môi trường |

Repository có nhiều thay đổi chưa commit, đặc biệt ở messaging và Game. Những thành phần mới có trong working tree không đồng nghĩa đã được kiểm chứng hoặc triển khai production.

## 3. Shared project và điểm tích hợp

Tạo `CoreEngine/Oservability`, target net9.0 theo solution hiện tại, chỉ chứa hạ tầng:

```text
Oservability/
  Logging/              Serilog setup, service metadata, trace enrichment
  Correlation/          ICorrelationContext, request middleware, outbound handler
  Tracing/              ActivitySources, OpenTelemetry setup
  Metrics/              Meter names, bounded messaging metrics
```

API đề xuất:

```csharp
builder.AddHrkObservability("HRK.GAME");
// Sau builder.Build():
app.UseHrkCorrelation();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseHrkIdentityLogging();
app.UseAuthorization();
```

Correlation middleware chạy trước request logging để completion log cũng có CorrelationId. Identity scope chạy sau authentication. Completion log cần enrich identity qua diagnostic context vì scope identity bên trong có thể đã dispose khi middleware ngoài ghi log. Giữ routing/CORS đúng vị trí hiện có; với Gateway, đặt middleware trước `UseOcelot()`.

Không cần thêm abstraction logging mới bên cạnh `ILogger<T>` và wrapper hiện hữu. Ưu tiên `ILogger<T>` cho code mới.

Package dự kiến, chốt version tương thích net9.0 khi triển khai:

- Serilog.AspNetCore, Serilog.Sinks.Seq; Console sink và cấu hình Serilog theo dependency thực tế.
- OpenTelemetry.Extensions.Hosting, OpenTelemetry.Exporter.OpenTelemetryProtocol.
- OpenTelemetry.Instrumentation.AspNetCore, Http và SqlClient.
- Redis instrumentation nếu tương thích với StackExchange.Redis đang dùng và có thể truy cập multiplexer. Không khẳng định Redis đã được instrument chỉ vì đăng ký distributed cache.
- gRPC nằm ngoài phạm vi triển khai hiện tại theo yêu cầu; không thêm package hoặc instrumentation gRPC.

## 4. Quy tắc logging và bảo vệ dữ liệu

Mỗi event có ServiceName, Environment, ApplicationVersion, MachineName/InstanceId; Timestamp do logging framework cung cấp. TraceId/SpanId lấy từ Activity hiện tại. CorrelationId, PlayerId, UserId, ContentVersion và RequestPath chỉ thêm khi có ngữ cảnh hợp lệ.

Claim `NameIdentifier` trong controller là UserId; PlayerId phải lấy sau khi Game resolve nhân vật. Không dùng UserId làm PlayerId. ContentVersion lấy từ phiên bản content thực sự dùng trong operation; không tin header do client gửi làm dữ liệu authoritative.

| Level | Sử dụng |
|---|---|
| Trace/Debug | Chẩn đoán, cache hit/miss, thông tin tính toán; tắt mặc định production |
| Information | Startup/shutdown, battle started/completed, consumer started, content activated |
| Warning | Retry có thể phục hồi, fallback, ACK thất bại sau handler thành công |
| Error | Operation thất bại, hết retry hoặc chuyển DLQ; truyền exception đầy đủ |
| Critical | Thành phần thiết yếu không thể tiếp tục |

Structured template: `logger.LogInformation("Battle {BattleId} completed for player {PlayerId}", battleId, playerId)`. Không nội suy, không dump DTO/entity, không log từng combat action ở Information.

Không log request/response bodies, Authorization, token, password, connection strings hoặc secrets. Đặc biệt Realtime nhận `access_token` qua query string: chỉ ghi path, không ghi query hoặc URL đầy đủ. Gateway/outbound HTTP cũng cần kiểm tra log URL. Tắt EF sensitive data logging production và không export SQL parameters. Exception có thể chứa dữ liệu nhạy cảm, nên review các boundary thay vì coi redaction theo tên property là đủ.

Consumer hiện đưa `exception.Message` vào header `x-error-message`; SQL outbox có `LastError`. Cần xử lý/sanitize dữ liệu lỗi ở cả broker và DB, không chỉ ở Seq.

## 5. Correlation và distributed tracing

HTTP đọc một giá trị X-Correlation-Id, giới hạn độ dài (đề xuất 128), allowlist ký tự và bỏ giá trị không hợp lệ; thiếu thì tạo Guid. Không sử dụng header làm khóa phân quyền/idempotency. Thêm response header bằng OnStarting. Cấu hình CORS expose header để Angular đọc được.

Scoped context phù hợp với request/consumer. Background worker tạo scope riêng cho từng message; không giữ một mutable singleton chứa CorrelationId. Scope dispose kể cả khi exception để tránh context lẫn giữa deliveries.

HttpClient dùng instrumentation để truyền W3C tự động; handler riêng chỉ forward CorrelationId. Gateway cần xác minh request thực tế qua Ocelot giữ cả trace context và correlation header; không giả định cấu hình host đủ chứng minh propagation.

Trong `Core.RabbitMQ`:

1. Producer tạo ActivityKind.Producer, inject W3C context của producer vào AMQP headers.
2. Consumer extract traceparent/tracestate, tạo ActivityKind.Consumer với remote parent, scope MessageId/CorrelationId/ConsumerName.
3. Decode header byte[] đúng UTF-8; context sai không làm mất khả năng xử lý message.
4. Retry và DLQ giữ MessageId/CorrelationId và trace headers; mỗi lần consume có span riêng.
5. ActivitySource phải được OpenTelemetry đăng ký; nếu không có listener thì StartActivity có thể trả null. Không dùng span giả để ghi metadata.

Giữ AMQP metadata làm carrier trước mắt. Nếu chuyển sang `IntegrationEventEnvelope<T>`, tạo contract/version và rollout producer/consumer đồng bộ; bọc JSON hiện tại ngay lập tức sẽ phá deserialization của consumer V1.

## 6. Khoảng trống Outbox cần sửa

`OutboxBackgroundService` hiện gán `CorrelationId = message.PartitionKey`. PartitionKey phục vụ partition/order nghiệp vụ, không nên bị buộc làm correlation của request.

Thêm nullable CorrelationId, TraceParent, TraceState vào OutboxMessage, EF mapping, SQL migration, insert/read mapping và writer. Writer capture context lúc ghi Outbox trong transaction nguồn. Worker phục hồi parent context từ DB rồi tạo producer span khi publish; retry vẫn dùng cùng MessageId. Không lấy trace từ polling cycle để thay trace của request ban đầu.

Không đổi PartitionKey/Sequence hoặc cơ chế locking chỉ vì bổ sung tracing. Cân nhắc link thay vì parent với job rất dài/batch fan-in; cần chọn và tài liệu hóa convention.

## 7. Metrics

Đăng ký Meter dùng chung và một meter Game. Thu thập HTTP duration, battle count/duration/failure, messaging processed/failed/duration, outbox pending count và tuổi message lâu nhất. Pending/age lấy theo lịch có giới hạn, tránh query DB trên mỗi request.

Tags dùng Service, Result, ConsumerName, EventType trong danh sách contract và BattleMode đã chuẩn hóa. Không dùng PlayerId, UserId, BattleId, JobId, MessageId, TraceId hoặc URL chứa ID làm metric labels. Histogram duration dùng giây hoặc ms thống nhất và ghi unit. Technical retries phải phân biệt với permanent failures để dashboard không báo một request lỗi nhiều lần như nhiều nghiệp vụ độc lập.

## 8. ActivityService và dữ liệu nghiệp vụ

Tạo service riêng theo Domain/Application/Infrastructure/Worker hoặc API theo convention solution. SQL lưu PlayerActivity và Inbox trong cùng transaction; unique key ConsumerName + MessageId. ActivityId ổn định theo event, nguồn service và thời gian UTC. Query có pagination, authorization theo player/admin, khoảng thời gian và giới hạn page size.

Tái sử dụng InboxExecutor nhưng không chỉ kiểm tra bằng lookup trước insert; cần kiểm chứng uniqueness dưới concurrency. ACK chỉ sau commit; duplicate ACK thành công; lỗi persistence đi retry/DLQ theo policy.

Game vẫn sở hữu enhancement history, battle logs và ledger kinh tế. Không coi PlayerActivity là ledger. Trước khi phát CurrencyChanged cần xác minh model currency hiện có và transaction boundary của operation, bổ sung ledger nếu thiếu. Critical admin audit phải được ghi ở transaction nguồn; event trung tâm chỉ là bản tổng hợp.

Luồng đầu tiên nên chọn QuickClimb vì source đã có Outbox/Inbox và contract. Log bắt đầu/kết thúc job, producer/consumer span theo floor; timeline chỉ ghi milestone hữu ích, tránh một dòng activity cho từng progress percentage. Sau đó mở rộng enhancement, battle và auth với contract riêng, payload tối thiểu.

## 9. Docker và vận hành

Thêm Seq volume `/data`, ingestion URL `http://seq:5341` qua `Seq__ServerUrl`, API key qua secret/environment. Sink Seq gửi theo batch bất đồng bộ; cấu hình buffer hữu hạn và giữ console khi Seq ngừng hoạt động. Không thêm dependency health của Seq vào Game.

Seq UI bind localhost khi development, ingestion không publish ra Internet. Production dùng reverse proxy HTTPS cho `logs.example.com`, authentication và quyền truy cập. Khởi tạo admin password hash theo tài liệu Seq; không commit password. Compose network hiện có thể dùng DNS service trên default network; explicit network là lựa chọn vận hành, không cần thay tất cả mạng chỉ để logging.

Collector nhận OTLP, có memory limiter + batch processor, export đến backend traces/metrics đã chọn. Collector đơn lẻ không phải kho trace hoặc dashboard. Tránh dùng debug exporter production vì có thể ghi lại telemetry vào console. Dùng config/environment để bật exporter; providers do host quản lý lifecycle. Serilog được host dispose/flush khi shutdown; shutdown có timeout, không chờ Seq vô hạn.

Retention đề xuất: Debug 3 ngày; Information 14 ngày; Warning/Error 60 ngày, điều chỉnh theo dung lượng VPS. Đây là policy cần cấu hình trong Seq, không phải việc thêm giá trị appsettings tự động kích hoạt. Activity, ledger, audit và battle replay có policy riêng. Chưa thấy Quartz dependency trong các project đã kiểm tra; không thêm Quartz chỉ để phục vụ logging.

## 10. Thứ tự triển khai và tiêu chí kiểm chứng

1. Oservability + Serilog/Console/Seq ở 4 host; kiểm tra event cấu trúc, startup metadata và Seq offline không làm request thất bại.
2. HTTP correlation + trace enrichment + identity; test header thiếu/sai, isolation request song song, CORS và propagation Gateway -> Game.
3. OpenTelemetry HTTP/SQL và metrics, Collector/backend; xác minh actual exported spans và không có secrets/high-cardinality labels.
4. Messaging propagation + Outbox migration; test request -> outbox -> worker -> consumer, retry giữ correlation/message ID và headers.
5. ActivityService dùng Outbox/Inbox hiện hữu; integration test SQL Server + RabbitMQ: cùng MessageId gửi hai lần chỉ có một Activity và một Inbox row; ACK sau commit, DB lỗi không ACK thành công.
6. Mở rộng events và audit theo từng operation/transaction, thêm retention và dashboard vận hành.

Không cần triển khai toàn bộ 46 mục cùng lúc. Phần logging cốt lõi là bước 1-2; tracing/metrics và lịch sử nghiệp vụ là các phần triển khai nối tiếp với tiêu chí kiểm chứng riêng.

## 11. Chốt các bổ sung của người dùng

Tên library chính xác theo yêu cầu là `Oservability`, tại `CoreEngine/Oservability`. Library chứa implementation Serilog, Seq sink configuration và OpenTelemetry, không chứa business history hoặc EF entities của Game. Hiện mới cập nhật thiết kế; chưa tạo project executable.

### Enricher và logger-only usage

Bổ sung thiết kế `TelemetryLogEnricher : ILogEventEnricher`, đọc `Activity.Current` tại thời điểm ghi log, không cache Activity hoặc ID trong singleton. Developer chỉ gọi `ILogger<T>.LogInformation(...)`; framework tự gắn TraceId/SpanId khi có Activity. Enricher có thể expose thêm properties theo schema cần dùng và service metadata, nhưng không tạo Activity cho mỗi log statement.

Serilog hiện hỗ trợ TraceId/SpanId như first-class fields lấy từ Activity.Current; khi chọn package/sink cần ưu tiên khả năng có sẵn, tránh gắn trùng fields với custom properties. Enricher là điểm mở rộng dùng chung, không phải thay thế instrumentation. OpenTelemetry logging provider cũng có automatic correlation riêng; pipeline đã chọn là Serilog -> Seq nên không thêm provider thứ hai chỉ để xuất trùng log.

Nếu không có Activity (startup, background worker chưa instrument), enricher không thể phục hồi trace đã mất hoặc tạo ra distributed trace chỉ từ một lệnh logger. Tạo Activity ở boundary HTTP/job/consumer. Log có IDs không bảo đảm backend có span tương ứng khi sampling loại span hoặc exporter thất bại.

### API request và UnitOfWork

API: một completion event cho mỗi request, có Method, path an toàn, StatusCode, Elapsed, CorrelationId và trace fields. Information cho request bình thường, Warning cho lỗi client bất thường theo policy, Error cho 5xx/unhandled exception; health checks có thể giảm xuống Debug hoặc bỏ qua. Không log start/end trong mỗi controller nếu middleware đã làm việc đó. Request completion và business milestone là hai loại event hợp lệ khác nhau.

UnitOfWork hiện chưa inject ILogger. Không thêm Information mỗi Begin/Save/Commit/Rollback. EF/SqlClient instrumentation phục vụ SQL diagnostics; có thể thêm Debug transaction lifecycle và Warning khi transaction chậm vượt ngưỡng. Error nên ghi tại boundary biết nghiệp vụ và chịu trách nhiệm xử lý exception (HTTP exception handler hoặc consumer), không ghi cùng stack trace ở repository, UnitOfWork, service rồi middleware. Nếu rollback tự nó thất bại, ghi lỗi rollback riêng với context transaction và giữ được lỗi gốc.

Quan sát cần review riêng: `ExecuteInTransactionAsync` gọi SaveChangesAsync rồi CommitTransactionAsync, trong khi CommitTransactionAsync cũng gọi SaveChangesAsync; `SaveChangesAsync` bắt exception validation thuộc EF6 dù DbContext là EF Core. Đây là vấn đề flow/exception cần đánh giá, không giải quyết bằng việc thêm log và chưa sửa trong tác vụ phân tích này.

### RabbitMQ và retry

| Tình huống | Level đề xuất | Metadata |
|---|---|---|
| Consumer khởi động/dừng | Information | ConsumerName, Queue, ServiceName |
| Publish/consume thành công mỗi message | Debug mặc định | MessageId, EventType, Duration; business milestone log riêng |
| Lỗi transient và đã schedule retry thành công | Warning, kèm exception | MessageId, CorrelationId, Attempt, MaxAttempts, RetryDelay, Category |
| Hết retry/permanent và chuyển DLQ thành công | Error, kèm exception | MessageId, EventType, ConsumerName, Attempt, Category, Dlq |
| Republish retry/DLQ thất bại | Error | Cùng message context; không ACK original như đã xử lý thành công |
| Handler đã commit nhưng ACK thất bại | Warning | MessageId, ConsumerName; chờ redelivery và Inbox deduplicate |
| Duplicate đã xử lý | Debug | MessageId, ConsumerName; ACK duplicate |
| Host cancellation bình thường | Debug hoặc bỏ qua | Không tính là business failure |

Consumer hiện log Error trước khi phân loại Duplicate/Transient; cần dời quyết định level sau classifier và quyết định retry/DLQ. Outbox hiện cũng log Error mỗi publish failure; transient còn retry nên dùng Warning, exhausted/permanent dùng Error. Ghi log sau khi retry/DLQ publish thành công mới khẳng định message đã được chuyển; nếu publish hoặc ACK lỗi thì event phải phản ánh đúng bước lỗi. Mỗi delivery có scope và consumer Activity riêng; metrics phân biệt attempt failure, final failure và duplicate.

### CorrelationId và TraceId

Hai ID có thể cùng phục vụ tìm log nhưng không cùng ý nghĩa. TraceId xác định một distributed trace; SpanId xác định một operation trong trace. CorrelationId do ứng dụng quản lý để nhóm một operation nghiệp vụ qua nhiều trace/request hoặc replay. Retry có thể giữ cùng TraceId nếu tiếp tục parent, hoặc có TraceId mới nếu dùng trace mới + link; không được mặc định mọi retry đều đổi TraceId.

Với HRK có Outbox, job và replay, giữ cả hai. Giữ CorrelationId ổn định xuyên suốt operation nghiệp vụ, propagate W3C context riêng và MessageId riêng cho deduplication. Không ép CorrelationId bằng TraceId, không dùng một trong hai làm MessageId. Nếu hệ thống chỉ có luồng request đơn giản thì có thể dùng riêng TraceId để tìm technical logs, nhưng đó không phải kiến trúc hiện tại đã chọn.

History vẫn nằm ở Game; ActivityService nhận event qua RabbitMQ + Outbox để lưu timeline tập trung phục vụ hỗ trợ, tra cứu và audit tổng hợp. Không sao chép nguyên mọi bảng history hoặc dùng timeline tập trung làm nguồn dữ liệu gameplay. Điều này đã có trong mục 8 và được giữ nguyên.

## Tài liệu chính thức đối chiếu

- Serilog ASP.NET Core: https://github.com/serilog/serilog-aspnetcore
- Seq Docker và khởi tạo credentials: https://datalust.co/docs/getting-started?platform=docker
- Serilog automatic trace fields: https://github.com/serilog/serilog/releases
- OpenTelemetry .NET log correlation: https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/logs/correlation/README.md

Chưa chạy build/tests vì kết quả của tác vụ này là phân tích và tài liệu; chưa có thay đổi executable để xác nhận pipeline hoạt động.

