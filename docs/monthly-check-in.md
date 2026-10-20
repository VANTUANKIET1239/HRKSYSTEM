# Điểm danh theo ngày trong tháng

GAME sở hữu lịch, phần thưởng và receipt. Người chơi bấm nhận mỗi ngày một lần;
ngày 21 nhận ô 21, không tích lũy sang ô tiếp theo và chưa có nhận bù. Sang tháng
mới dùng lịch mới, giữ toàn bộ lịch sử cũ. Không quét/reset người chơi bằng Quartz.

## Triển khai database

Chạy trên database GAME bằng công cụ hỗ trợ batch `GO` (SSMS/sqlcmd), theo thứ tự:

1. `Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Create_GameOperationProcedures.sql`
   (wallet lock dùng chung với các thao tác kinh tế hiện có).
2. `Migration_MonthlyCheckIn.sql` trong cùng thư mục để tạo ba bảng và trigger.
3. Mở `Publish_MonthlyCheckInCalendar.sql`, đặt `@MonthKey`, xem/tune phần thưởng,
   rồi chạy để publish đúng tháng được chọn. Script mẫu mặc định tháng `2026-10`.

Migration không seed lịch hoặc thay đổi trạng thái người chơi. Script publish mẫu
cho 1000 vàng/ngày và thêm 50 kim cương tại các mốc 7/14/21/28; đây là giá trị mẫu
cần duyệt theo kinh tế game trước khi dùng. Rerun không ghi đè lịch đã tồn tại.
Chuẩn bị lịch tháng tiếp theo trước khi tháng hiện tại kết thúc; nếu chưa có lịch
published/active, UI hiển thị chưa mở và server từ chối claim mới.

`@TimeZoneId` là timezone .NET/IANA; `@SqlTimeZone` là tên SQL Server tương ứng.
Mặc định `Asia/Ho_Chi_Minh` / `SE Asia Standard Time`, reset `00:00`. Nếu thay
timezone/reset, phải cấu hình tương ứng trước publish. Server xác minh Start/End
UTC khớp với tháng và reset, không lấy ngày từ client hoặc timezone của máy chủ.

## Các bảng

- `HRK_CheckInCalendars`: mã `MONTHLY_CHECKIN`, MonthKey, timezone/reset, Start/End
  UTC, cấp tối thiểu, ConfigVersion, IsPublished, IsActive.
- `HRK_CheckInRewardConfigs`: CalendarId + DayOfMonth unique, RewardsJson. Chỉ
  có đúng số ngày của tháng (28/29/30/31), một cấu hình cho mỗi ngày.
- `HRK_PlayerCheckInClaims`: PlayerId, CalendarId, CheckInDate (SQL date của ngày
  game), RequestId, ClaimedAtUtc, ConfigVersion và ResultJson chứa receipt đầy đủ.

Lịch bắt đầu ở trạng thái draft. Ghi đủ cấu hình từng ngày rồi mới set
`IsPublished = 1`. Trigger kiểm tra đủ ngày/không trùng khoảng thời gian. Sau khi
publish, không sửa/xóa/unpublish lịch hoặc thay thưởng, kể cả chưa có claim; chỉ
được pause/resume `IsActive`. Điều chỉnh phần thưởng cho tháng chưa publish kế tiếp.
Không sửa/xóa lịch sử claim. GET hiển thị quà đã nhận từ ResultJson đã lưu.

Hỗ trợ vàng, kim cương và vật phẩm tham chiếu ItemTemplateId/Code (bao gồm vật phẩm
cường hóa/tăng sao). Mỗi ngày 1-10 dòng phần thưởng, Quantity phải dương. Chưa hỗ trợ
tướng hoặc EXP. Nếu có nhiều loại item, tên/ảnh preview có thể khai báo trong JSON;
phần thưởng thực tế được resolve từ template của game khi cấp.

```json
[
  { "Type": "GOLD", "Quantity": 1000 },
  { "Type": "ITEM", "ItemTemplateId": 2101, "Name": "Vật phẩm", "Quantity": 2 }
]
```

Script publish cũng thêm feature `CHECK_IN` vào `HRK_GameFeatureConfigs` nếu chưa có,
placement `TOP_EVENT`. Nếu hệ thống đang cache bảng feature, refresh template cache
bằng chức năng admin hiện có hoặc restart theo quy trình triển khai.

## API / giao diện

API có `[Authorize]`, lấy UserId từ JWT, không nhận PlayerId/ngày/thưởng từ body:

- `GET /api/check-in`: lịch hiện tại; các ô CLAIMED/CLAIMABLE/MISSED/LOCKED,
  gameDate, serverTimeUtc, nextResetAtUtc, canClaim và lý do khóa.
- `POST /api/check-in/claim`, body `{ "requestId": "<guid mới cho thao tác>" }`.
  Trả requestId, calendarId, checkInDate, claimedAtUtc, configVersion,
  alreadyClaimed và rewards thực tế. RequestId rỗng bị từ chối.

Qua gateway dùng `/gateway/dcs-game/check-in` và `/gateway/dcs-game/check-in/claim`;
được phục vụ bởi route GAME wildcard hiện có. Giao diện tại `/dcs-game/check-in`,
mở từ nút Điểm danh trong sảnh (ActionCode CHECK_IN). UI tự đếm tới reset theo
server time, tải lại lịch khi qua ngày, vô hiệu hóa nút trong lúc nhận và giữ
nguyên requestId khi chưa xác nhận được kết quả. Ngày nhận do server quyết định.

## Transaction, retry và activity

Command dùng UnitOfWork transaction, khóa row ví qua `usp_HRK_LockPlayerWallet`,
đọc receipt trước khi cấp thưởng. Unique `(PlayerId, CalendarId, CheckInDate)` và
`(PlayerId, RequestId)` chống nhận trùng trên nhiều instance/thiết bị.
Replay cùng RequestId trả receipt cũ kể cả đã qua ngày/tháng hoặc lịch cũ đã pause.
RequestId khác nhưng cùng ngày cũng trả receipt đã nhận, không cộng thêm.

`GameRewardGranter` là phần cấp vàng/kim cương/item dùng chung với TowerRewardService.
Tower giữ cơ chế thưởng chờ/EXP hiện tại; điểm danh không dùng period hay pending
reward của Tower. Nếu bất kỳ item nào không vừa túi, trả INVENTORY_FULL, rollback
toàn bộ tiền/item/claim, không mất lượt. Dọn túi rồi gửi lại request có thể nhận.

Claim + phần thưởng + activity Outbox commit cùng transaction. Event
`CheckInClaimed` có danh tính ổn định theo player/calendar/ngày; metadata gồm tháng,
ngày, request ID, config version và tóm tắt vật phẩm. HRK ACTIVITY đã chấp nhận loại
event mới. Các trường GameEventId/EventPeriodId/JobExecutionId để null; PeriodKey
là ngày game. Không gọi đồng bộ Internal API tới ACTIVITY trong luồng nhận thưởng.
Triển khai ACTIVITY có whitelist mới trước khi cho phép GAME phát event mới.

## Kiểm thử

```powershell
dotnet test Tests/dcs-game/GAME.Domain.Tests/GAME.Domain.Tests.csproj
$env:HRK_SUMMON_SQL_TESTS = '1'
dotnet test Tests/dcs-game/GAME.Domain.Tests/GAME.Domain.Tests.csproj --filter FullyQualifiedName~CheckInSqlTests
```

SQL tests tạo/xóa database LocalDB ngẫu nhiên qua fixture chung, không dùng DB game
thực. Kiểm tra nhận đồng thời, replay, đổi tháng, pause lịch, túi đầy và rollback,
Outbox, migration/publish rerun và bảo vệ dữ liệu đã publish. Unit tests kiểm tra
tháng nhuận, ngày game/reset, GET không ghi dữ liệu và snapshot lịch sử.

UI tests: chạy Angular test với include
`src/app/dcs-game/src/app/features/check-in/check-in-page.component.spec.ts`.
