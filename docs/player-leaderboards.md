# Bảng xếp hạng người chơi

## Phạm vi

- Hai bảng tích lũy toàn thời gian: `power` và `diamonds-spent`.
- GAME sở hữu bảng tổng hợp và giao dịch tiêu kim cương trong DB `HRK`.
- Điểm lực chiến là kỷ lục của đội hình có `IsSelected`, gồm chỉ số tướng, trang bị và bonus trận pháp.
- Lực chiến hiện tại được phép giảm; kỷ lục chỉ tăng. Đổi tướng ở đội hình thử nghiệm trong trận không được ghi nhận, trừ khi sức mạnh thực tế của đội hình mặc định cũng thay đổi.
- Tiêu kim cương tính mọi nguồn kim cương đã được sử dụng thành công. Nhận thưởng, cấp tài nguyên khởi tạo hoặc cộng hoàn tiền không tăng điểm. Tổng tiêu là gross spending; việc trừ lại điểm khi hoàn tiền chưa nằm trong phiên bản này.
- Người chơi ngừng hoạt động (`IsActive=false`) không xuất hiện trong danh sách hoặc API hồ sơ.

## Triển khai

Chạy bản mới của [Create_GameOperationProcedures.sql](../Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Create_GameOperationProcedures.sql) trên DB GAME trước khi deploy backend để tạo SP khóa chung. Không cần thay đổi schema bảng xếp hạng cho lần refactor khóa/entity này.

1. Chạy [Migration_PlayerLeaderboards.sql](../Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_PlayerLeaderboards.sql) trên DB GAME (`HRK`) trước khi chạy phiên bản backend mới. Script tạo hai bảng, index, dòng khởi điểm cho người chơi cũ và menu `LEADERBOARD`; có thể chạy lại.
2. Chạy [Publish_Language.sql](../Services/HRKLANGUAGE/Language.Infrastructure/Data/Scripts/Publish_Language.sql) trên `HRK_LANGUAGE` để publish các nhãn mới. Worker ngôn ngữ sẽ cập nhật Redis qua outbox hiện có.
3. Build/chạy lại GAME, API Gateway và UI. Nếu GAME đang giữ cache template cũ, dùng chức năng đồng bộ template trên giao diện hoặc API refresh template hiện có để lấy menu mới.
4. `LeaderboardInitializationWorker` tính lực chiến khởi điểm bằng công thức C# dùng chung, theo batch 50 người, chỉ xử lý dòng chưa `PowerInitialized`. Không ghi đè kỷ lục hoặc tổng tiêu đã có. Nếu chưa chạy migration, worker log và thử lại sau 30 giây.

Không cần tạo job Quartz, queue RabbitMQ hoặc Redis leaderboard cho phiên bản này.

### Dữ liệu cũ

- Lực chiến tại thời điểm khởi tạo là kỷ lục khởi điểm. Không thể suy ngược kỷ lục trước đó.
- Tổng tiêu bắt đầu từ 0 tại `TrackingStartedAtUtc`; không suy từ số dư hoặc cộng lại lịch sử rời rạc.
- Script không sửa ví hoặc phát lại tài nguyên cho người chơi cũ.

## Luồng ghi lực chiến

[PlayerRankingWriter](../Services/HRKGAME/GAME.Application/Services/PlayerRankingWriter.cs) dùng `IUnitOfWork` và tính lại bằng `IFormationPowerQueryService`.

Khóa được inject qua [IGameTransactionLock](../Services/HRKGAME/GAME.Application/Interfaces/IGameTransactionLock.cs), dùng chung với tower, ví và khởi tạo người chơi. Ghi thành tích dùng entity có tracking qua [PlayerRankingStatsStore](../Services/HRKGAME/GAME.Application/Services/PlayerRankingStatsStore.cs), kiểm tra `RowVersion`; SQL đặc thù cho khóa nằm trong SP. Xem [hướng dẫn khóa giao dịch](./game-transaction-locks.md).

```text
Command
  → ICqrsDispatcher / MediatR
  → ValidationBehavior
  → PlayerPowerChangeBehavior (chỉ command được đánh dấu)
  → ExecutePowerChangeAsync
  → mở transaction và khóa game:ranking:{PlayerId}
  → đọc lại dữ liệu, kiểm tra và thực hiện nghiệp vụ
  → SaveChanges trong transaction để truy vấn thấy dữ liệu mới
  → tính lực chiến đội hình mặc định
  → cập nhật current; chỉ thay peak và thời điểm nếu vượt kỷ lục
  → commit cả nghiệp vụ và thành tích
```

Behavior gọi wrapper dùng cơ chế retry transaction hiện có của UnitOfWork. Khi rollback, toàn bộ trạng thái được dựng lại cho lần thử tiếp theo. Không mở transaction lồng nhau trong các handler được bọc.

[PlayerPowerChangeBehavior](../Services/HRKGAME/GAME.Application/Behaviors/PlayerPowerChangeBehavior.cs) được đăng ký sau validation qua [AddPlayerPowerChangePipeline](../Services/HRKGAME/GAME.Application/DependencyInjection/PlayerPowerPipelineRegistration.cs). Chỉ command implement [IPlayerPowerChangeCommand](../Services/HRKGAME/GAME.Application/Interfaces/IPlayerPowerChangeCommand.cs) được bọc. Handler không còn inject ranking writer hoặc tự gọi wrapper. Query và command không được đánh dấu không cần resolve dependency của behavior này.

Đã nối các luồng: chọn/sắp xếp/nâng trận pháp, nâng cấp/tăng sao tướng, mặc/tháo/tháo hết/đổi trang bị, cường hóa/hạ cấp trang bị, hoàn thành trận phó bản và khởi tạo người chơi.

Các thao tác leo tháp, nhận thưởng và xử lý từng tầng càn quét đi qua [TowerOperationRunner](../Services/HRKGAME/GAME.Application/Services/TowerOperationRunner.cs); runner khóa người chơi và cập nhật lực chiến trước commit, bao gồm trường hợp tướng lên cấp do EXP.

Khi thêm command mới ảnh hưởng sức mạnh, implement marker và gửi qua `ICqrsDispatcher.CommandAsync` hoặc MediatR; không gọi trực tiếp handler nếu cần pipeline. Ví dụ:

```csharp
public sealed record UpgradeExampleCommand(string UserId, long HeroId)
    : ICommand<BaseResponse<ExampleDto>>, IPlayerPowerChangeCommand;
```

Với command không mang danh tính trong payload, trả `null` bằng explicit interface implementation để behavior lấy `IGameRequestContext.UserId`, không thêm trường UserId công khai vào JSON:

```csharp
string? IPlayerPowerChangeCommand.UserId => null;
```

Danh tính khác null lấy từ command, phù hợp luồng worker không có HTTP context. Controller phải tiếp tục lấy UserId từ tài khoản đã xác thực, không tin UserId do client tự chọn. Worker dùng danh tính của message nội bộ đã được kiểm tra.

14 command được chuyển từ wrapper thủ công sang marker: chọn/sắp xếp/nâng trận pháp, nâng cấp/tăng sao tướng, bốn thao tác mặc/tháo/tháo hết/đổi trang bị, cường hóa/hạ cấp trang bị, vào phó bản, mua thể lực và mở túi. Hai command mua thể lực/mở túi cũng được gắn marker để pipeline bảo toàn transaction/khóa/ghi tổng tiêu đang có.

Command tự sở hữu transaction nhiều bước như khởi tạo người chơi, tháp và càn quét không gắn marker. Chúng tiếp tục gọi `RefreshPowerAsync` trước commit thông qua runner/luồng hiện có. Worker khởi tạo baseline cũng giữ transaction riêng. Không gọi sau commit hoặc fire-and-forget. Nếu đang sở hữu transaction, không dispatch command có marker lồng vào trong; dùng thao tác nghiệp vụ chung trong transaction đó.

Lần thay đổi pipeline này không thêm bảng cấu hình, event RabbitMQ hoặc migration SQL.

Điểm lưu bằng `bigint`; công thức chiến đấu hiện tại vẫn trả `int`. Phép cộng lực chiến đã dùng `checked` để lỗi tràn số không âm thầm tạo kỷ lục sai. Việc mở rộng toàn bộ chỉ số/công thức chiến đấu lên `long` là thay đổi riêng.

Thay đổi hàng loạt công thức hoặc master data không tự chạy lại mọi người chơi. Khi thực hiện thay đổi đó, cần một đợt tính lại có kiểm soát; kỷ lục lịch sử vẫn được giữ theo quy tắc hiện tại.

## Luồng tiêu kim cương

[DiamondSpendingService](../Services/HRKGAME/GAME.Application/Services/DiamondSpendingService.cs) được dùng bởi chiêu mộ, mua thể lực và mở rộng túi đồ.

- Caller phải sở hữu transaction.
- `RequestId` được ràng buộc với người chơi, lý do và tham chiếu nghiệp vụ; unique receipt ngăn ghi trùng.
- Cùng request đã xử lý: không trừ ví, không tăng điểm và không thực hiện lại phần mua.
- Request được dùng cho nghiệp vụ khác: trả conflict.
- Trừ ví, ghi receipt, tăng tổng tiêu và cấp nội dung mua cùng commit/rollback.
- Các API mua thể lực/mở túi vẫn nhận request cũ không có `RequestId`; backend cấp UUID cho mỗi lần gọi. Muốn retry HTTP an toàn, client phải giữ cùng UUID. UI hiện gửi UUID trong request; retry cùng request giữ nguyên body đó.

Khi thêm chỗ tiêu kim cương mới, sử dụng service này và không trừ `wallet.Diamonds` trực tiếp. Bản ghi 0 kim cương vẫn có receipt để bảo đảm idempotency nhưng không tăng điểm hoặc thay thời điểm phân hạng.

## API

Mọi endpoint yêu cầu đăng nhập và trả `BaseResponse`, đi qua controller → `ICqrsDispatcher` → `HRKBaseQuery` → `ILeaderboardReader` → repository/UnitOfWork.

| Endpoint tại GAME | Nội dung |
|---|---|
| `GET /api/leaderboards?type=power&page=1&pageSize=20` | Lực chiến kỷ lục |
| `GET /api/leaderboards?type=diamonds-spent&page=1&pageSize=20` | Tổng kim cương đã tiêu |
| `GET /api/leaderboards/players/{playerId}` | Hồ sơ và đội hình hiện tại |
| `GET /api/leaderboards/players/{playerId}/heroes/{heroId}` | Chi tiết tướng đang ở đội hình mặc định |

Qua gateway thay `/api` bằng `/gateway/dcs-game`. Ví dụ gọi nằm trong [Leaderboards.http](../Services/HRKGAME/HRK.GAME/Leaderboards.http).

Phân trang có giới hạn 100 dòng, sắp xếp trong SQL theo điểm giảm dần, thời điểm đạt tăng dần rồi PlayerId tăng dần. Hạng là số thứ tự duy nhất 1,2,3…; mỗi trang trả thêm hạng của chính người xem. Các lần gọi độc lập có thể thấy vị trí thay đổi khi người khác đạt điểm mới; API không đóng băng snapshot giữa các trang.

Danh sách không tính lại toàn bộ đội hình và không tải chi tiết tướng theo từng dòng. Hồ sơ chi tiết chỉ tải khi mở người chơi; trang bị/kỹ năng được tải khi chọn tướng. DTO công khai không có UserId, email, ví, kho đồ chưa mặc, khóa/yêu thích tướng hoặc các API thao tác.

## UI

Menu `LEADERBOARD` mở modal toàn màn hình với hai tab, phân trang, hạng bản thân và top 3 được nhấn mạnh. Hồ sơ mở ngay trong modal và nút quay lại giữ trang danh sách.

`PublicHeroDetailComponent` chỉ hiển thị dữ liệu, không inject service quản lý hoặc phát hành động nâng cấp/thay đồ. `HeroEquipmentPortraitComponent` được dùng chung với màn hình quản lý võ tướng hiện có. Các request danh sách/hồ sơ/tướng cũ bị hủy khi điều hướng để tránh response đến muộn thay nội dung đang xem.

Đội hình được xem là đội hình **hiện tại**, không phải snapshot tại thời điểm lập kỷ lục. Kỷ lục và lực chiến hiện tại được hiển thị riêng.

## Kiểm tra

Các kiểm thử SQL dùng DB LocalDB tạm và biến `HRK_SUMMON_SQL_TESTS=1`, không chạy trên DB ứng dụng:

```powershell
$env:HRK_SUMMON_SQL_TESTS = '1'
dotnet test Tests/dcs-game/GAME.Domain.Tests/GAME.Domain.Tests.csproj --filter 'FullyQualifiedName~LeaderboardSqlTests'
```

Bao gồm kỷ lục không giảm, giữ thời điểm khi bằng điểm, rollback giao dịch, idempotency, cập nhật đồng thời, thứ tự/phân trang toàn cục, dữ liệu công khai và từ chối xem tướng không ở đội hình.
