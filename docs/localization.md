# Ngôn ngữ Việt–Anh

Triển khai service `HRK.LANGUAGE`, thư viện `Core.Localization` và ngx-translate 18
cho Angular 20. Chưa có API hay giao diện quản trị. Không có startup seeder.

## Dữ liệu và xuất bản

Service sở hữu database riêng `HRK_LANGUAGE`:

| Bảng | Vai trò |
| --- | --- |
| HRK_LanguageCultures | Ngôn ngữ hỗ trợ: vi-VN và en-US |
| HRK_LanguageKeys | Định danh key dùng chung giữa các ngôn ngữ, duy nhất theo group + key |
| HRK_LanguageRevisions | Metadata phiên bản theo group + culture, thời gian và người publish |
| HRK_LanguageRevisionTexts | Mỗi dòng là một bản dịch: RevisionId + LanguageKeyId + Value |
| HRK_LanguagePublications | Revision hiện hành theo group + culture |
| Language_OutboxMessages | Công việc đồng bộ Redis cùng transaction publish |
| Language_ProcessedMessages | Mapping inbox dùng chung; hiện chưa có Rabbit consumer |

Revision là BIGINT identity trong DB, không tạo lại khi Redis mất dữ liệu.
Nội dung trong DB là các dòng quan hệ, không lưu dictionary JSON trong revision.
Culture nằm tại revision; một key có thể có nhiều bản dịch/ngôn ngữ/revision.
Chỉ Redis và HTTP response ghép các dòng thành dictionary JSON để .NET và
ngx-translate tải cả bộ trong một lần. Outbox chỉ chứa metadata group/culture/revision.
Các nhóm hiện có: common, auth, game, hrm. Nhóm hrm hiện chỉ dùng thông báo common.

Chạy theo thứ tự trên SQL Server muốn triển khai:

1. `Services/HRKLANGUAGE/Language.Infrastructure/Data/Scripts/Create_LanguageDatabase.sql`
2. Chọn database `HRK_LANGUAGE`, chạy `Migration_Language.sql`.
3. Chạy `Seed_Language.sql` trên database đó.

Migration chạy lặp được; nếu còn cột `TranslationsJson` của schema cũ, script
chuyển **toàn bộ revision**, bao gồm lịch sử, sang key và từng dòng bản dịch.
Revision ID, publication, thời gian/người publish và outbox được giữ nguyên.
Script đối chiếu nội dung hai chiều trước khi bỏ cột JSON, trong cùng transaction;
dữ liệu không hợp lệ sẽ rollback thay vì bỏ qua. Triển khai migration và code
cùng đợt, dừng service ngôn ngữ/worker trong thời gian chuyển schema.

Trigger cấm sửa/xóa key và nội dung revision đã publish, cũng như chèn thêm câu
vào revision cũ. Trong transaction publish, `PublishedAtUtc` tạm NULL khi chèn
các dòng, sau đó được đặt một lần để khóa revision trước khi đổi publication.
Seed chỉ thêm
bundle chưa tồn tại, giữ nguyên nội dung đã xuất bản. Các script không được tự
động chạy trên DB ứng dụng bởi quá trình build/test.

`tools/localization/catalog.json` là danh sách bản dịch đã rà từ source. Mỗi dòng
là `[source, English, VietnameseOverride?]`; nếu thiếu override, tiếng Việt dùng
source. Catalog gồm thông báo backend, nhãn giao diện và các khóa lỗi đặt tên
tường minh, được sinh thành từng dòng bản dịch Việt–Anh trong 8 bundle. Hai bundle hrm
hiện chưa có câu riêng nên không có dòng chi tiết, vẫn có revision/publication.

Để cập nhật nội dung khi chưa có admin:

```powershell
node tools/localization/build-seed.mjs
```

Lệnh kiểm tra khóa trùng, va chạm định danh và các placeholder, rồi sinh lại
`Seed_Language.sql` và `Publish_Language.sql`. Sau khi rà nội dung thay đổi, chạy
**Publish_Language.sql** trên DB ngôn ngữ để xuất bản. Bundle giống hệt bản hiện
hành không tạo thêm revision/outbox, kể cả thứ tự các dòng thay đổi.
Script insert đã chứa đủ từng câu dưới dạng `INSERT INTO @Texts ([Key], [Value])
VALUES ...`, sau đó truyền vào procedure bằng table-valued parameter
`dbo.HRK_LanguageTextTable`, không truyền một chuỗi JSON.
Việc kiểm tra placeholder giữa hai ngôn ngữ nằm trong công cụ build catalog;
table type ràng buộc key duy nhất/phân biệt hoa thường, tối đa 256 ký tự, không có
khoảng trắng đầu/cuối, và giá trị không rỗng. Procedure kiểm tra group/culture.

Ví dụ publish riêng **toàn bộ nội dung** của một bundle nhỏ:

```sql
DECLARE @Texts dbo.HRK_LanguageTextTable;
INSERT INTO @Texts ([Key], [Value]) VALUES
    (N'CHECKIN_ALREADY_CLAIMED', N'Bạn đã điểm danh hôm nay.'),
    (N'CHECKIN_LEVEL_REQUIRED', N'Cần đạt cấp {{level}} để điểm danh.');
EXEC dbo.HRK_PublishLanguageBundle
    @Group=N'game', @Culture=N'vi-VN', @Texts=@Texts, @PublishedBy=N'deploy';
```

Đây là publish cả bundle, không phải upsert riêng vài câu: key không nằm trong
`@Texts` sẽ không có ở revision mới. Để cập nhật một câu trong bundle đã có, lấy
đầy đủ nội dung hiện hành vào `@Texts`, sửa dòng cần đổi rồi publish:

```sql
DECLARE @Texts dbo.HRK_LanguageTextTable;
INSERT INTO @Texts ([Key], [Value])
SELECT k.[Key], t.[Value]
FROM dbo.HRK_LanguagePublications p
JOIN dbo.HRK_LanguageRevisionTexts t ON t.RevisionId=p.RevisionId
JOIN dbo.HRK_LanguageKeys k ON k.Id=t.LanguageKeyId
WHERE p.[Group]=N'game' AND p.Culture=N'vi-VN';
UPDATE @Texts SET [Value]=N'Bạn đã nhận thưởng hôm nay.'
WHERE [Key]=N'CHECKIN_ALREADY_CLAIMED';
EXEC dbo.HRK_PublishLanguageBundle
    @Group=N'game', @Culture=N'vi-VN', @Texts=@Texts, @PublishedBy=N'deploy';
```

Các script catalog được sinh lại là nguồn đầy đủ khi publish; cần đồng bộ những
chỉnh sửa SQL thủ công vào catalog trước lần chạy `Publish_Language.sql` tiếp theo.
`node tools/localization/build-seed.mjs --check` xác minh script khớp catalog mà
không ghi lại file. TVP là tham số bảng READONLY theo
[tài liệu SQL Server](https://learn.microsoft.com/en-us/sql/relational-databases/tables/use-table-valued-parameters-database-engine).

Procedure khóa theo group để các culture dùng chung key an toàn, lưu revision và
từng dòng nội dung, đổi pointer và ghi outbox trong
một transaction. Worker `LanguageCacheOutboxWorker` dùng outbox store hiện có để
claim/retry, đọc publication **mới nhất** rồi ghi Redis. Không dùng RabbitMQ
broadcast: các backend hiện đọc Redis chung và không giữ cache giữa các request.
Không đăng ký thêm generic Rabbit outbox dispatcher trên bảng outbox này.

Redis chỉ giữ **bundle hiện hành** trong một hash key theo group/culture:

```text
hrk:dev:i18n:{game:vi-VN}:bundle
  revision = "12"
  payload  = JSON của TranslationBundle revision 12
```

`GetCurrentAsync` đọc payload bằng một lệnh HGET; không cần đọc pointer rồi lấy
key revision thứ hai. `PublishAsync(bundle)` dùng script riêng
`RedisTranslationScripts.PublishCurrentIfNewer` để so sánh revision và cập nhật
hai field cùng TTL trong một lần thực thi. Revision cũ không thay đổi payload hay
TTL; gửi lại cùng revision hợp lệ sẽ làm mới TTL. So sánh chuỗi số nguyên dương
giữ chính xác toàn bộ khoảng SQL BIGINT, không chuyển revision sang số Lua.
Lịch sử nội dung chỉ nằm trong DB; Redis không tạo key riêng cho từng revision.

Kết nối được quản lý bởi `IRedisConnectionProvider` trong `Core.Caching`, đăng ký
singleton qua `AddHrkRedisConnections` và được inject vào `RedisTranslationCache`.
Provider khởi tạo khi có thao tác đầu tiên, dùng chung một kết nối cho cùng chuỗi
cấu hình và đóng kết nối khi service dừng. Hủy một request chỉ ngừng chờ, không
hủy task kết nối mà request khác đang dùng. Cache không tự tạo/đóng kết nối.
Writer vẫn ném lỗi Redis để outbox biết lần đồng bộ chưa thành công và retry.

Nếu cache thiếu, API đọc publication trong DB và phục hồi Redis; Redis lỗi vẫn
có thể trả dữ liệu DB. Reader sử dụng UnitOfWork, join
publication/revision/culture/text/key trong một query, không query riêng từng câu.
Key bundle có TTL để phục hồi cả khi bị eviction hoặc sau sự cố worker. Đọc không
gia hạn TTL. Publish có tính nhất quán cuối cùng: DB commit trước, Redis được cập
nhật khi worker xử lý xong.

Khi nâng cấp từ cache hai key cũ, triển khai các service đọc/ghi cùng đợt.
`:current` và `:revision:<id>` cũ tự hết hạn theo TTL; code mới không đọc/ghi các
key đó. Key `:bundle` mới tự được nạp từ outbox hoặc API khi cache thiếu. Không
cần chạy lại migration/seed SQL cho thay đổi cache này.

Outbox lỗi tạm thời sẽ retry theo `TransactionalMessaging:Outbox`; quá giới hạn
retry thì trạng thái Failed (4). Theo dõi log và LastError. Sau khi sửa nguyên
nhân, có thể đưa riêng các bản ghi LanguageCache bị lỗi trở lại Pending (1), đặt
RetryCount=0, NextAttemptAt=SYSUTCDATETIME(), LockToken/LockedUntil=NULL. Worker sẽ
đồng bộ publication mới nhất; không cần tái tạo revision.

## Cấu hình và chạy

`dotnet run --project Services/HRKLANGUAGE/HRK.LANGUAGE` dùng launch profile ở cổng
5005; cấu hình Development dùng LocalDB. Trên môi trường khác phải cung cấp
`Database:ConnectionString`. Endpoint `/health/ready` kiểm tra kết nối DB.

Tất cả backend đọc ngôn ngữ phải dùng cùng cấu hình:

```json
{
  "Localization": {
    "Enabled": true,
    "DefaultCulture": "vi-VN",
    "RedisConnectionString": "localhost:6379,abortConnect=false,connectTimeout=1000,asyncTimeout=1000",
    "RedisKeyPrefix": "hrk:dev:i18n:",
    "ApiTimeoutSeconds": 2
  },
  "InternalApi": {
    "Services": { "language": { "BaseAddress": "http://localhost:5005", "TimeoutSeconds": 2 } }
  }
}
```

Prefix phải giống nhau giữa các service, và khác nhau giữa dev/staging/prod.
Cache ngôn ngữ **không** dùng `Redis:InstanceName` riêng của GAME. Khi không khai
báo Localization:RedisConnectionString, thư viện dùng Redis:ConnectionString nếu
có. Docker Compose đã cấu hình địa chỉ nội bộ `http://hrk.language:8080`; cung cấp
biến `LANGUAGE_DATABASE_CONNECTION` và prefix theo môi trường trước khi chạy.

Chỉ service ngôn ngữ ghi vào vùng cache này. Các caller dùng quyền đọc vùng key
ngôn ngữ trong môi trường triển khai. API đọc chỉ trả bản đã publish và truy cập
được trước đăng nhập; không đưa bí mật hay dữ liệu cá nhân vào catalog.

## Backend

GAME đăng ký `AddHrkLocalization(configuration, "game")`; AUTH dùng `"auth"` sau
AddHrkInternalApi. Bộ dịch đọc Accept-Language trước, tiếp đến cookie
`hrk_language`, rồi mặc định vi-VN. Lookup đi qua group và common, tiếp đến ngôn
ngữ mặc định, rồi message gốc. Không tự suy luận locale từ tài khoản.

`ApiResponseLocalizer` dịch Message, Errors và ValidationErrors tại HTTP boundary,
giữ nguyên code, status và trace ID. Áp dụng cho shared exception/result filters,
exception middleware và status code pages. Chỉ dịch các trường thông báo; không
đệ quy sửa dữ liệu nghiệp vụ, tên võ tướng/vật phẩm hay nội dung replay trong DTO.
Lỗi 500 vẫn được che bằng thông báo hệ thống an toàn trước khi dịch.

Lỗi nghiệp vụ dự kiến phải dùng `BusinessException` với code và kind phù hợp.
Các exception kỹ thuật được giữ ở 500, chỉ thông báo hệ thống được dịch. Chi tiết
các chỗ đã rà/sửa nằm trong [backend-error-audit.md](backend-error-audit.md).

Thông báo cố định hiện có dùng fingerprint UTF-8/FNV-1a 64-bit ổn định, khớp giữa
C# và TypeScript. Ưu tiên khóa của literal chính xác trước khóa lỗi chung để cùng
code với nhiều thông báo không bị dịch nhầm. Không cần gọi API trong từng handler.

Với thông báo có tham số, giữ template và các giá trị riêng:

```csharp
throw BusinessException.Localized("INSUFFICIENT_GOLD",
    $"Số dư Vàng không đủ. Cần: {amount:N0} Vàng (Hiện có: {wallet.Gold:N0} Vàng).");
```

Factory tạo template `... {{p0}} ... {{p1}} ...` cùng Arguments. Thêm template đó
vào catalog; bộ dịch định dạng các giá trị theo ngôn ngữ yêu cầu. Message gốc vẫn
giữ cách định dạng cũ để fallback khi dịch không khả dụng. Có thể dùng MessageKey
và Arguments cho khóa đặt tên tường minh; điểm danh dùng CHECKIN_LEVEL_REQUIRED.

`ITextLocalizer` scoped, chỉ gom lookup trùng **trong một request**, không giữ
revision giữa các request. Nó đọc Redis trước và gọi Core.InternalApi REST khi
cache thiếu/lỗi. Dependency lỗi sẽ dùng message dự phòng; dịch response có tổng
ngân sách thời gian riêng để không kéo dài lỗi nghiệp vụ. Request cancellation
vẫn được truyền tiếp.

## Frontend

Bộ chọn ngôn ngữ nằm trong popup **Cài đặt**, mở bằng nút bánh răng cạnh tên
người chơi ở khung thông tin góc trên bên trái sảnh game. Bộ chọn dùng bố cục
inline trong modal, không còn widget nổi ở root của ứng dụng. Lựa chọn áp dụng
ngay và được lưu trong cookie để khôi phục khi tải lại trang.

Loader gọi `HrkApiService.CallApi<LanguageBundle>` với endpoint chung
`ApiEndpoints.Localization.Bundle(group, culture)` và `loading: 'none'`. Loader
giữ timeout theo `LOCALIZATION_CONFIG.bundleTimeoutMs`, kiểm tra bundle, gom
request đang chạy và fallback; không dựa vào `CallOptions.timeoutMs` chưa được
thực thi. Bundle được lấy qua `GET /gateway/language/bundles/{group}/{culture}`. Response
là resource `{group,culture,revision,translations}`, không phải BaseResponse.
Loader nạp dictionary vào ngx-translate, tải common và ứng dụng đang mở; thêm nhóm
khi chuyển route. Các request cùng bundle đang chạy được gom lại. Cookie chỉ lưu
vi-VN/en-US, có Path=/, SameSite=Lax và Secure khi HTTPS. Header Accept-Language chỉ
được thêm vào request gửi về gateway của ứng dụng.

`BrowserCookieService` trong `libs/core/storage` là nơi đọc/ghi/xóa cookie frontend:
encode/decode giá trị, Path, Domain, SameSite, Secure và Max-Age. Server rendering
không truy cập cookie trình duyệt. `LanguageService` và `LanguageInterceptor` sử
dụng service này; `language-preference.ts` chỉ chuẩn hóa giá trị ngôn ngữ đã đọc.
`localization.config.ts` tập trung ngôn ngữ mặc định, danh sách ngôn ngữ, tên và
thời hạn cookie, timeout bundle. Bộ chọn ngôn ngữ lấy danh sách từ config này.

`text-key.ts` chỉ sinh key và giữ cache key. Các hằng số FNV-1a 64-bit, UTF-8,
prefix `text_` và hex 16 ký tự là quy ước cố định, khớp backend và script SQL;
không đưa thuật toán vào cấu hình theo môi trường. Refactor này giữ nguyên key,
không cần migration hoặc publish lại dữ liệu ngôn ngữ.

API có ETag yếu, so sánh If-None-Match bằng parser chuẩn, hỗ trợ danh sách ETag và
`*`; bản không đổi trả 304 không có body. `Cache-Control: private, no-cache` cho
phép browser lưu và revalidate. HTTP cache do browser quản lý, không tự nhét ETag
vào cookie hoặc tự xử lý 304 trong Angular. Gateway đã expose ETag và
Content-Language qua CORS. Response JSON dùng compression br/gzip qua Core.Web.

Các nhãn, nút, bộ lọc, tooltip và trạng thái được gắn `HrkTranslatePipe` tại chỗ
hiển thị, bao gồm template HTML và template inline: auth, sảnh, tướng, đội hình,
hành trang, lò rèn, thư viện, avatar, điểm danh, chiêu mộ, chiến dịch và chiến đấu.
Tên menu/danh mục lấy từ API cũng được dịch tại template; giá trị lọc, mã tính
năng, sự kiện click và tên người chơi giữ nguyên. Các câu có số liệu dùng tham số
để bản dịch có thể đổi thứ tự từ. Nội dung game lấy từ DB chỉ dịch được khi có
câu tương ứng trong catalog; tên riêng và chuỗi thiếu khóa dùng nguyên văn.
Modal có tham số được dịch trước khi thay {used}/{limit}. Bộ dịch tối thiểu giữ
thông báo mạng hoạt động khi API lỗi; chọn ngôn ngữ hoặc chuyển route sẽ tải lại
bundle common/ứng dụng còn thiếu, kể cả khi ngx-translate đã giữ bộ dự phòng.
Khi thêm giao diện, dùng `HrkTranslatePipe` cho nội dung hiển thị và thêm bản dịch
vào catalog. Với DB đã có publication, chạy `Publish_Language.sql` để thêm nhãn
mới; `Seed_Language.sql` giữ nguyên bundle đã tồn tại. Sau publish, tải lại trang
để lấy dictionary mới.

Tab đang mở giữ dictionary hiện tại; không có push refresh. Mở lại trang sẽ dùng
HTTP revalidation. Đổi ngôn ngữ nạp dictionary tương ứng; không gọi API khi render
từng thông báo. Ngôn ngữ mặc định và chuỗi gốc là fallback khi thiếu khóa.

Thông báo động ở game dùng tagged template để giữ khóa ổn định trước khi thay giá
trị, tương tự BusinessException.Localized ở backend:

```typescript
this.toast.success(this.toast.text`Đã bán ${count} vật phẩm, nhận ${gold} vàng.`);
```

Thêm source `Đã bán {{p0}} vật phẩm, nhận {{p1}} vàng.` vào catalog. Các giá trị tên
riêng được giữ nguyên. Điểm danh và kết quả cường hóa có thêm key/arguments trong
DTO để giao diện dịch thông báo có tham số mà không phân tích chuỗi đã render.

## Kiểm thử và rà source

```powershell
dotnet test Tests/Language.Tests/Language.Tests.csproj
$env:HRK_LANGUAGE_SQL_TESTS='1'
dotnet test Tests/Language.Tests/Language.Tests.csproj --filter FullyQualifiedName~LanguageSqlTests
node tools/localization/coverage.mjs
node tools/localization/audit.mjs
$env:HRK_LANGUAGE_REDIS_TESTS='1'
$env:HRK_LANGUAGE_REDIS_CONNECTION='localhost:6379,abortConnect=false'
dotnet test Tests/Language.Tests/Language.Tests.csproj --filter FullyQualifiedName~RedisTranslationTests
```

SQL tests tạo rồi xóa riêng DB `HRK_LanguageTests_<guid>` trên LocalDB, không dùng
DB ứng dụng. Coverage rà literal BusinessException, FluentValidation.WithMessage
và các literal thông báo frontend; không chứng minh mọi chuỗi động/nhãn/lore trong
source đều có bản dịch. Audit rộng hơn xuất file báo cáo được gitignore để rà tay.
Frontend tests nằm trong `src/libs/core/localization/localization.spec.ts` và
`ui-localization.spec.ts` (render template thực, đổi Việt–Anh, tham số và dữ liệu
điều hướng), cùng regression tests điểm danh, toast và modal. Redis Lua có kiểm thử opt-in riêng khi
cung cấp Redis test; không chạy FLUSHDB hay tác động key ứng dụng.
