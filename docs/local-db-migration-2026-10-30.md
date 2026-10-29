# Kết quả chạy migration local ngày 30/10/2026

SQL Server: `DESKTOP-87IELOL`. Đã chạy trên database local thực tế, sau khi dừng gateway, AUTH, GAME và LANGUAGE.

## Script đã chạy thành công

1. [Migrate_HRK_AUTH.sql](../Services/AUTH/AUTH.Infrastructure/Data/Scripts/Migrate_HRK_AUTH.sql).
2. [Create_AuthMessaging.sql](../Services/AUTH/AUTH.Infrastructure/Data/Scripts/Create_AuthMessaging.sql).
3. [Migration_PlayerInitialization.sql](../Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_PlayerInitialization.sql).
4. [Seed_PlayerInitialization.sql](../Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Seed_PlayerInitialization.sql).
5. [Publish_Language.sql](../Services/HRKLANGUAGE/Language.Infrastructure/Data/Scripts/Publish_Language.sql).

Database nguồn có cột computed `HRK_LoginSessions.IsActive`. Lần chạy đầu dừng và rollback trước khi xóa dữ liệu. Script đã được bổ sung để tạo lại computed column từ công thức gốc, giữ PERSISTED nếu có và từ chối dependency bên ngoài bảng. Ba test migration LocalDB đã qua trước khi chạy lại thành công.

## Đối chiếu sau migration

- Mười bảng AUTH đã chuyển khỏi HRK sang HRK_AUTH; số dòng từng bảng khớp baseline trước khi chuyển.
- Tài khoản: 1; phiên đăng nhập: 120; refresh token: 839; application route config: 4.
- Bảy bảng Identity cùng các PK/index/constraint được giữ; cột IsActive vẫn là computed column và giá trị đúng với LogoutTime.
- AUTH outbox/inbox riêng đã được tạo; kiểm tra constraint trong HRK_AUTH không có vi phạm.
- Khóa ngoại GAME player → AUTH user đã gỡ; số dòng các bảng GAME được đối chiếu vẫn giữ nguyên. Player hiện có vẫn là 1; không cấp thêm tài nguyên cho tài khoản cũ.
- Config DEFAULT v1 active: level 1, thể lực 200/200, 10.000 vàng, 300 kim cương, 100 nguyên liệu, sức chứa 200; ba tướng ID 1/2/3 và mười đá cường hóa cấp I.
- Ngôn ngữ hiện hành: common vi-VN r25, en-US r26; game vi-VN r27, en-US r28. Cả 28 bản ghi language outbox đã Published; không còn publication hiện hành chờ cập nhật cache.

## Backup trước khi thay đổi

Đã chạy BACKUP DATABASE WITH COPY_ONLY, CHECKSUM và RESTORE VERIFYONLY WITH CHECKSUM cho cả ba database. Các file trên SQL Server:

```text
C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\HRK_before_auth_migration_20261029_185552_09e7e3.bak
C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\HRK_AUTH_before_auth_migration_20261029_185552_09e7e3.bak
C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\HRK_LANGUAGE_before_auth_migration_20261029_185552_09e7e3.bak
```

Tên file dùng timestamp UTC; thời điểm local là ngày 30/10/2026.

## Service sau khi chạy

Đã build và deploy image mới cho AUTH, GAME, ACTIVITY; khởi động lại LANGUAGE và gateway. Không dừng Redis, RabbitMQ hoặc các service khác.

- AUTH, GAME, LANGUAGE, ACTIVITY: dependency health Healthy.
- Gateway tại http://localhost:5000: dependency health Healthy, gồm AUTH/GAME/LANGUAGE/ADMIN.
- GET `/gateway/auth/applications` trả thành công bốn application.
- GET bundle common/en-US qua gateway trả r26 và có bản dịch màn hình chuẩn bị nhân vật mới; game/en-US trả r28.

Chưa tạo tài khoản thử nghiệm trên DB thật; không thêm user hoặc player ngoài dữ liệu hiện có.
