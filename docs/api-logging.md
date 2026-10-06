# Log vòng đời HTTP và Activity

Áp dụng từ 07/10/2026 cho Gateway, Auth, Game, Realtime và Activity thông qua `AddCustomDependency` → `AddHrkObservability`, và `UseHrkCorrelation` trong pipeline.

## HTTP

`Oservability/Http/ApiRequestLoggingMiddleware.cs` thay request completion logger cũ. Mỗi request được chọn có một `ApiStarted` và một `ApiEnded`; các log nghiệp vụ ở giữa dùng cùng request Activity/TraceId. SpanId có thể khác khi log bên trong một child span. Middleware không đọc DB và không gắn JobId/Floor/ItemId vào log chung.

Các field chung: EventName, ServiceName, Environment, RequestId/HttpRequestId, RequestMethod, RequestPath, CorrelationId, TraceId/SpanId; ApiEnded thêm StatusCode, DurationMs, Outcome, UserId nếu xác định được và ResponseStarted. Không ghi query string, headers hoặc token. HTTP 4xx dùng Warning; 5xx/exception dùng Error; request bị hủy dùng Warning/Aborted. Outcome mô tả HTTP, không suy luận kết quả gameplay từ HTTP 200.

`ApiEnded` được ghi khi pipeline phía dưới trả về hoặc ném lỗi, không xác nhận client nhận response. Nếu exception chưa chuyển thành response và response chưa bắt đầu, logger báo 500 nhưng không thay response hoặc nuốt exception. Nếu bổ sung exception handler để map exception sang response, đặt handler phía dưới API logging middleware để ApiEnded quan sát status cuối cùng. Health và Swagger được bỏ qua mặc định; request bị process kill không bảo đảm có dòng ended.

Gateway và downstream mỗi nơi có cặp log riêng. Dùng `ServiceName` cùng `HttpRequestId` để xem một cặp (RequestId trong log nghiệp vụ có thể là mã idempotency riêng); dùng TraceId để xem cả luồng xuyên service. Các request HTTP độc lập có TraceId độc lập; worker có thể tiếp tục trace được truyền qua Outbox/Rabbit sau khi HTTP đã kết thúc.

## Cấu hình body

Mặc định trong appsettings của 5 host:

```json
"Observability": {
  "ApiLogging": {
    "Enabled": true,
    "LogRequestBody": false,
    "LogResponseBody": false,
    "MaxBodyBytes": 4096,
    "BodyPaths": [],
    "AllowedBodyFields": []
  }
}
```

Enabled chỉ điều khiển log HTTP, không tắt logger nghiệp vụ hoặc tracing. Nếu cần điều tra input/output, cấu hình trong appsettings.Development.json của host cần xem:

```json
"Observability": {
  "ApiLogging": {
    "LogRequestBody": true,
    "LogResponseBody": true,
    "BodyPaths": ["/api/inventory/enhance"],
    "AllowedBodyFields": ["requestId", "inventoryItemId", "success"]
  }
},
"Serilog": {
  "MinimumLevel": {
    "Override": {
      "Oservability.Http.ApiRequestLoggingMiddleware": "Debug"
    }
  }
}
```

Chỉ endpoint và field được cho phép mới có ApiInput/ApiOutput ở Debug. BodyPaths hỗ trợ đường dẫn chính xác hoặc prefix kết thúc bằng `/*`. AllowedBodyFields là tên property JSON, không phân biệt hoa/thường, áp dụng cả object lồng nhau; credential fields vẫn bị che dù nằm trong danh sách cho phép. Giá trị field khác bị che. Không ghi body của WebSocket, và không ghi raw body không phải JSON hoặc JSON lỗi. Form/multipart/binary chỉ ghi lý do bỏ qua.

MaxBodyBytes là giới hạn mỗi body log, từ 1 đến 65536 byte, không phải giới hạn request của API. Body vượt ngưỡng được bỏ qua với `BodyTooLarge`, không ghi prefix JSON chưa lọc. JSON sau lọc vẫn vượt ngưỡng cũng được bỏ qua. Request stream được khôi phục để endpoint đọc bình thường; response được chuyển thẳng tới client, chỉ giữ bản sao có giới hạn để log, không giữ toàn bộ response hay trì hoãn streaming. Cấu hình body và allowlist nên đặt riêng ở host sở hữu endpoint; giữ Gateway tắt body để tránh ghi hai lần.

## QuickClimb và Activity

QuickClimbJobCreated được ghi sau khi TowerOperationRunner hoàn tất commit; start idempotent chỉ ghi QuickClimbJobReused ở Debug. ApiEnded kết thúc HTTP tạo/nhận job, không chờ worker. QuickClimbFloorProcessed ở Debug và QuickClimbEnded ở Information mô tả vòng đời nền. Jaeger có span `QuickClimb.ProcessFloor` với tag `quickclimb.job.id`, `quickclimb.floor`, kết quả và trạng thái. RealtimeStatusSent ở Debug báo SendAsync phía server hoàn tất, không xác nhận client nhận thông báo.

Các tính năng phát event có kiểu rõ ràng qua IPlayerActivityEvents: ItemEnhancedEvent, QuickClimbStartedEvent, QuickClimbEndedEvent. PlayerActivityEventMapper ở Game.Infrastructure tập trung ánh xạ payload, tên activity và identity; PlayerActivityEvents thêm contract vào Outbox của cùng GameDbContext trước SaveChanges/commit. Không có remote call hoặc commit riêng. EventId được tạo ổn định theo RequestId/player hoặc JobId/lifecycle để retry cùng sự kiện có cùng identity. Các message status/từng tầng vẫn dùng routing key riêng.

Event được khai báo tại điểm nghiệp vụ thay đổi trạng thái; đây không phải HTTP filter và không tự tạo activity cho mọi SELECT/UPDATE. Các tính năng mới thêm typed event và mapper tương ứng. Scoped event publisher bỏ qua lần raise trùng trong cùng transaction và re-enlist Outbox khi retry chuyển sang transaction mới sau rollback. Activity consumer tiếp tục commit timeline cùng Inbox trước ACK; history gốc vẫn nằm trong Game. Khi tạo attempt retry phải tái dựng/reset EF state phù hợp; EventId ổn định không thay thế transaction hoặc Inbox.

## Tra cứu Seq

```sql
TraceId = '...'
```

Chỉ xem cặp HTTP:

```sql
ServiceName = 'HRK.GAME'
and (EventName = 'ApiStarted' or EventName = 'ApiEnded')
```

Xem lỗi HTTP:

```sql
EventName = 'ApiEnded' and Outcome = 'ServerError'
```

Lọc QuickClimb bằng JobId trên log nghiệp vụ; lọc Jaeger bằng tag `quickclimb.job.id=<job-id>` và operation `QuickClimb.ProcessFloor`.

## Kiểm chứng 07/10/2026

- Build 5 host thành công.
- 11 test HTTP/config pass: correlation isolation, TraceId/HttpRequestId, 4xx/5xx, exception, cancellation, body redaction/giới hạn và stream không bị thay đổi.
- SQL integration pass: business + Activity Outbox rollback cùng nhau, raise trùng sau SaveChanges không thêm record, retry cùng scoped publisher/DbContext re-enlist cùng EventId.
- Các integration RabbitMQ/Seq/Collector/Jaeger và Ocelot propagation hiện có pass.
- 279 test Game pass. Containers test riêng được dispose; không chạy migration hay sửa dữ liệu Game/Activity đang dùng.
