# Khóa giao dịch dùng chung trong GAME

## Thành phần

- [IGameTransactionLock](../Services/HRKGAME/GAME.Application/Interfaces/IGameTransactionLock.cs): interface inject vào handler/service khi nghiệp vụ cần khóa.
- [SqlServerGameTransactionLock](../Services/HRKGAME/GAME.Infrastructure/Locking/SqlServerGameTransactionLock.cs): thực thi SP qua kết nối và transaction của UnitOfWork. Đăng ký scoped cùng UnitOfWork.
- [GameLockResources](../Services/HRKGAME/GAME.Application/Common/GameLockResources.cs): thống nhất tên tài nguyên giữa các tính năng và instance.
- [Create_GameOperationProcedures.sql](../Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Create_GameOperationProcedures.sql): triển khai SP khóa; cần chạy trên DB GAME trước khi deploy backend mới. Script chạy lại được.

## Sử dụng

Caller mở transaction, lấy khóa **trước khi đọc dữ liệu có thể thay đổi**, rồi dùng entity qua UnitOfWork:

```csharp
await uow.ExecuteInTransactionAsync(async () =>
{
    await locks.AcquireAsync(GameLockResources.PlayerState(playerId), ct);
    // Đọc entity, kiểm tra điều kiện và thực hiện nghiệp vụ trong transaction này.
}, ct);
```

Khi cần bảo vệ dòng ví trước các thao tác SQL khác:

```csharp
await uow.ExecuteInTransactionAsync(async () =>
{
    await locks.LockWalletAsync(playerId, ct);
    var wallet = await uow.Repository<HrkPlayerWallet>().Query()
        .SingleAsync(x => x.PlayerId == playerId, ct);
    // Cập nhật wallet và các entity liên quan.
}, ct);
```

`LockWalletAsync` lấy khóa logic người chơi trước, rồi lấy `UPDLOCK, HOLDLOCK` trên dòng ví. Khóa ví không tạo ví hoặc sửa số dư. `AcquireAsync` dùng `sp_getapplock` ở chế độ exclusive, transaction-owned, mặc định đợi tối đa 15 giây. Tính năng mới có thể dùng tài nguyên riêng có tên ổn định, ví dụ `game:guild:{guildId}`; tối đa 255 ký tự. Mọi nơi sửa cùng một tài nguyên phải dùng cùng tên khóa.

Khóa được SQL Server giải phóng khi commit/rollback. Không cần hàm unlock. Gọi lặp cùng tài nguyên qua cùng instance service trong một transaction không gọi lại DB. Khi retry hoặc mở transaction khác, service lấy lại khóa. Thất bại khi lấy khóa không được ghi nhớ là đã sở hữu khóa.

Không mở transaction lồng nhau. Khi nghiệp vụ đã có transaction, chỉ gọi service khóa trong transaction đó. UnitOfWork và service scoped không hỗ trợ chạy song song các thao tác trên cùng DbContext.

## Thứ tự khóa và tương thích

Giữ thứ tự khi một luồng cần nhiều khóa: **tower → player state → wallet row**. Điều này tránh chu trình chờ khóa giữa các tính năng. Khi thêm khóa nhiều người chơi, sắp xếp ID trước khi lấy khóa.

- Tower dùng `tower:player:{userId}`.
- Player state dùng tên cũ `game:ranking:{playerId}` để phối hợp với các instance cũ; dùng cho cập nhật lực chiến và tiêu kim cương.
- Khởi tạo tài khoản dùng `player-initialize:{SHA256(userId)}`.
- SP cũ `usp_HRK_AcquireTowerPlayerLock` được giữ làm wrapper của SP chung `usp_HRK_AcquireTransactionLock`. SP khóa ví được giữ vì khóa dòng và khóa logic có tác dụng khác nhau.

`RowVersion` vẫn phát hiện dữ liệu EF bị cũ hoặc các writer không tuân theo khóa logic. Khóa không thay thế transaction hay kiểm tra concurrency.

## Ghi xếp hạng

[PlayerRankingStatsStore](../Services/HRKGAME/GAME.Application/Services/PlayerRankingStatsStore.cs) lấy khóa chung rồi đọc/tạo entity có tracking qua UnitOfWork. [PlayerRankingWriter](../Services/HRKGAME/GAME.Application/Services/PlayerRankingWriter.cs) và [DiamondSpendingService](../Services/HRKGAME/GAME.Application/Services/DiamondSpendingService.cs) cập nhật entity và `SaveChangesAsync` trong transaction của caller. Không còn câu SQL cập nhật ranking viết trực tiếp trong các service này.

Store lưu dòng mới trong transaction để các lần lookup tiếp theo thấy cùng một entity. Spending lưu ví, receipt và tổng tiêu trước khi caller cấp phần thưởng; tất cả vẫn rollback nếu phần nghiệp vụ sau đó thất bại. Thay đổi entity khác đang chờ cũng có thể được flush bởi `SaveChangesAsync`, nhưng chỉ commit khi transaction của caller thành công.
