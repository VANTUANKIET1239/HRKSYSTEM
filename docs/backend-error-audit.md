# Rà soát exception phục vụ dịch thông báo backend

Rà soát ngày 24/10/2026 các câu lệnh throw và catch trong `Services` và
`CoreEngine`. Phạm vi sửa là những lỗi nghiệp vụ trong luồng GAME/AUTH đang đi
qua shared HTTP response pipeline. Không đổi cách xử lý toàn cục cho mọi
`KeyNotFoundException`, `InvalidOperationException` hoặc `ArgumentException`.

## Những chỗ đã sửa

25 chỗ ném lỗi trong 20 file được chuyển sang `BusinessException`:

| Trường hợp | Số chỗ | Code/HTTP |
| --- | ---: | --- |
| Không tìm thấy ví người chơi | 13 | PLAYER_WALLET_NOT_FOUND / 404 |
| Rương mốc do client yêu cầu không tồn tại trong kỳ | 1 | TOWER_CHEST_NOT_FOUND / 404 |
| Mã trận pháp client yêu cầu không tồn tại hoặc bị vô hiệu hóa | 1 | FORMATION_NOT_FOUND / 404 |
| Sự kiện chưa mở, chưa đủ cấp, đã hoàn thành, còn lượt đang chạy, hết lượt | 5 | EVENT_CLOSED, PLAYER_LEVEL_REQUIRED, TOWER_COMPLETED, RUN_STILL_ACTIVE, DAILY_RUN_LIMIT / 409 |
| Chọn loại đá tăng sao sai hoặc không đủ đá đã chọn | 2 | INVALID_STAR_MATERIAL / 400; INSUFFICIENT_MATERIALS / 409 |
| Không đủ số lượng vật phẩm khi tiêu hao | 1 | INSUFFICIENT_ITEM_QUANTITY / 409 |
| Thiếu hoặc sai định dạng session claim trong base command/query | 2 | INVALID_SESSION / 401 |

13 chỗ đọc ví gồm bán/mở rộng/cường hóa hành trang, nâng cấp trận pháp,
vào phó bản/mua thể lực/nhận rương, triệu hồi và truy vấn cấu hình triệu hồi,
nâng cấp võ tướng, tăng sao và preview tăng sao, cùng bộ cấp thưởng dùng chung.

Những câu có tham số dùng `BusinessException.Localized`, giữ template và
arguments trước khi format: cấp yêu cầu, mã trận pháp, tên/số lượng vật phẩm.
Không tạo key bản dịch từ chuỗi đã chứa giá trị của từng người chơi.

`FormationSnapshotService.PreviewAsync` chỉ biến lỗi Validation/Conflict thành
kết quả preview không hợp lệ. Lỗi NotFound/Unauthorized/Forbidden được truyền
ra HTTP boundary, tránh trả 200 cho tài nguyên không tồn tại. Khi không chỉ định
trận pháp mà cấu hình mặc định thiếu, service vẫn ném lỗi nội bộ.

## Những chỗ giữ nguyên và lý do

| Nhóm | Ví dụ trong source | Xử lý |
| --- | --- | --- |
| Thiếu/sai cấu hình máy chủ | Cường hóa, tăng sao, rarity, EXP, kỹ năng/đội hình địch, cấu hình mặc định TOWER_CLIMB, tầng được suy ra từ tiến độ | Giữ lỗi hệ thống; không trả nội dung cấu hình nội bộ cho client |
| Dữ liệu đã lưu hỏng | Receipt điểm danh, kết quả triệu hồi, snapshot rương/đội hình | Giữ lỗi hệ thống và diagnostic trong log |
| Không đọc được kết quả sau khi ghi hoặc commit | Nâng cấp võ tướng, trang bị/tháo/hoán đổi, kết quả transaction trận đánh | Giữ lỗi hệ thống; không báo tài nguyên người dùng không tồn tại sau khi thao tác đã thực hiện |
| Tham số/bất biến lập trình | Amount âm, quantity <= 0 của hàm nội bộ, roll ngoài phạm vi, thiếu transaction hoặc service dependency | Giữ exception kỹ thuật; các input công khai có validation riêng |
| AUTH không xoay hoặc tạo được token | RefreshTokenCommandHandler | Giữ lỗi hệ thống; token người dùng không hợp lệ đã dùng BusinessException Unauthorized |
| RabbitMQ/Quartz/outbox | Metadata message sai, settlement thất bại, Redis lỗi, job config thiếu | Giữ cơ chế lỗi/retry/failed hiện có |
| ACTIVITY admin query | Route kiểm tra IsValid và trả 400 trước khi gọi store | Không có lỗi nghiệp vụ bị đổi nhầm thành 500 ở đường gọi này |
| API quản trị job | JobsController chủ động bắt lỗi input và trả 400 | Giữ HTTP contract hiện có |
| Cấu hình khởi động, persistence, cache, compression và observability | Thiếu secret/connection string, SQL metadata sai, operation không hỗ trợ | Giữ exception kỹ thuật |

Một `KeyNotFoundException` do truy cập dictionary sai vẫn phải trả lỗi hệ thống,
không tự động được xem là tài nguyên API không tồn tại.

ACTIVITY/REALTIME không được bổ sung localization trong đợt rà exception này;
các diagnostic của consumer, worker và cấu hình không thuộc catalog UI.

## Bản dịch và triển khai

Bổ sung 5 template Việt–Anh vào `tools/localization/catalog.json` và sinh lại
`Seed_Language.sql`, `Publish_Language.sql`: 508 key, 1.016 dòng bản dịch.
Nếu DB đã có publication, chạy **Publish_Language.sql** để nhận các key mới.
Seed chỉ bổ sung bundle chưa tồn tại. Không có thay đổi schema DB trong đợt này.
Script không được tự động chạy lên DB ứng dụng.

## Kiểm thử

- Gọi HTTP qua InventoryController và handler thật: thiếu ví trả 404, code ổn định,
  message tiếng Anh; không bị thay bằng thông báo 500.
- Exception nội bộ và KeyNotFoundException ngoài dự kiến vẫn trả 500 với thông
  báo hệ thống được dịch; không lộ diagnostic gốc.
- Preview truyền lỗi mã trận pháp không tồn tại; thiếu mặc định vẫn là lỗi nội bộ.
- Điều kiện sự kiện và lựa chọn đá trả BusinessException phù hợp, giữ arguments.
- Tiêu hao vật phẩm thất bại không thay đổi trạng thái inventory.
- Session thiếu/sai trả Unauthorized; session hợp lệ được giữ nguyên.
- SQL seed/publish được đối chiếu đầy đủ với catalog trong DB LocalDB tạm.
