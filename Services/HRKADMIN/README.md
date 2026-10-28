# HRK Admin Service

Ba layer, mỗi type một file:

- `HRK.ADMIN`: Controller, xác thực/phân quyền, API filters, config và DI của host.
- `Admin.Application`: CQRS, DTO/options, interface và điều phối health monitoring.
- `Admin.Infrastructure`: adapter Internal API và đăng ký các implementation I/O.

```text
GET /api/admin/health/services
  -> HealthController
  -> ICqrsDispatcher (Core.Application)
  -> GetServicesHealthQueryHandler / HRKBaseQuery (Core.Web)
  -> IServiceHealthAggregator / ServiceHealthAggregator
  -> IServiceHealthProbe / InternalApiHealthProbe
  -> IInternalApiClient
  -> service đích /health/live và /health/dependencies
```

Query handler kế thừa `HRKBaseQuery<GetServicesHealthQuery, BaseResponse<SystemHealthReport>>`.
`HRKBaseQuery` và `HRKBaseCommand` generic của `Core.Web.CQRS` triển khai interface
handler từ `Core.Application`, dùng lại context/response helper của base hiện có.
Controller dùng `ICqrsDispatcher.QueryAsync`; command quản trị sau này dùng
`ICqrsDispatcher.CommandAsync` và `HRKBaseCommand<TCommand, TResponse>`.
MediatR/validation nằm phía sau dispatcher trong CoreEngine.

Query handler được scan qua `AddCoreApplication`. Host gọi `AddAdminApplication`
và `AddAdminInfrastructure` trong các hàm đăng ký riêng. Application không tham
chiếu Infrastructure, API host hoặc thư viện Internal API.

Admin.Application tham chiếu `Core.Web` để dùng HRK handler base nhận
`IHttpContextAccessor`. Host đăng ký accessor qua `AddApiServices`; khi resolve
handler trong unit test, đăng ký accessor tương tự. Aggregator và probe vẫn nhận
context cần thiết qua tham số/interface, không phụ thuộc vào HttpContext.

API yêu cầu JWT có role `Admin`; route Gateway là `/gateway/admin/health/services`.
Response dùng `BaseResponse<SystemHealthReport>`: `success` thể hiện việc tạo báo
cáo thành công, trạng thái hệ thống nằm ở `data.status`. Kết quả Unhealthy vẫn là
HTTP 200 của API quản trị. Client trước đây đọc `response.status`/`response.services`
cần chuyển sang `response.data.status`/`response.data.services`.

`/health/live` và `/health/dependencies` của chính Admin là endpoint của thư viện
health check, trả JSON contract nguyên bản và không thực hiện aggregation.

Appsettings không chứa signing key mặc định. Đặt `JwtSettings__SecretKey` cùng
signing key của AUTH; mặc định issuer `hrkapp`, audience `game-api`. Danh sách
target/policy HTTP vẫn dùng `HealthMonitoring` và `InternalApi` như trước.

```powershell
dotnet run --project Services/HRKADMIN/HRK.ADMIN/HRK.ADMIN.csproj --launch-profile http
dotnet test Tests/HealthChecks.Tests/HealthChecks.Tests.csproj
docker compose up -d --build hrk.admin
```

Dockerfile copy toàn bộ `Services/HRKADMIN` trước restore, bao gồm cả ba project.
Service hiện không cần DB/Redis/RabbitMQ riêng.

Tài liệu: [health check](../../docs/health-checks.md),
[tạo service mới](../../docs/new-service-guide.md).
