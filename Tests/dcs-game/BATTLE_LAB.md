# Battle Lab và các chủ đề cân bằng

## Build v2 và báo cáo phân tích

Mỗi slot có `mode: CUSTOM | BUILD`. JSON v1 thiếu mode vẫn hiểu là CUSTOM.
CUSTOM nhận stats cuối, cấm build; BUILD nhận build, cấm stats (kể cả stats trống).
`build` gồm level, stars (1–5), auraTier (1–4), rollSeed và equipment.
Mỗi trang bị gồm itemTemplateId, enhancement (0–15), stars (0–5).
Backend kiểm tra cấp tối đa theo rarity DB, cấp yêu cầu của đồ và không trùng category/slot.

BUILD sử dụng HeroProgressionStatService, HeroStatCalculationService,
ItemStatCalculationService và EquipmentInstanceFactory của game. Roll sao dùng chung
HeroStarRollCalculator với nâng sao thực. Các entity tạo trong bộ nhớ, không Add/Save.
Roll trang bị/sao theo rollSeed độc lập với seed trận; snapshot được giữ nguyên suốt batch.
AuraTier hiện không có bonus số học trong bộ tính stats dùng chung. StarAura theo sao chỉ là hình ảnh.
Lab không tự bổ sung hiệu ứng aura, bộ đồ, faction hay trận pháp chưa có trong pipeline này.

Sau batch, bấm **Tải báo cáo phân tích cân bằng** và gửi file
`battle-balance-analysis-*.json` để phân tích. File chứa:

- Request, cấu hình combat, phiên bản engine và SHA256 snapshot.
- Chỉ số cuối, kỹ năng, nguồn cộng chỉ số, bonus sao và roll trang bị thực sự dùng.
- Thắng/thua/hòa, vòng trung bình/min/max/độ lệch chuẩn, khoảng Wilson 95% cho tỷ lệ thắng trái.
- Theo tướng: sát thương vật lý/phép, hồi máu, nhận damage, HP còn, sống sót và số lần cast/chiêu năng lượng.
- Theo kỹ năng: số DAMAGE events, tổng HP damage và crit; không bao gồm shield/bleed-specific events.
- Danh sách seed/kết quả và các cảnh báo giới hạn, tránh suy diễn từ một matchup.

Báo cáo phân tích không chứa timeline replay để nhẹ hơn. Nút báo cáo + replay vẫn
giữ đầy đủ batch và các replay đại diện. Không tự lưu báo cáo vào DB: tải file trước khi rời trang.
Ví dụ cấu trúc v2: `samples/battle-lab-build-v2.json` (chưa chọn đồ vì ID tùy DB).
Chọn trang bị trong UI rồi tải config để lấy đúng ID trên DB của bạn.

## Cách dùng chung

Menu Demo Battle hiện mở Battle Lab thay vì tự chạy đội hình DEFAULT và random đối thủ.
Chọn 1–5 tướng mỗi bên từ template DB, vị trí và chỉ số cuối; dùng kỹ năng active từ DB.
Tướng mới chọn mặc định CUSTOM dùng chỉ số gốc. Chuyển BUILD để tính cấp/sao/trang bị.
Chạy một trận hoặc 1–300 seed liên tiếp, tối đa 100 vòng/trận. Override K chỉ áp dụng
cho lượt thử, không UPDATE cấu hình game. Hai bên dùng cùng một bộ quy tắc.
Preset lưu trên trình duyệt, không phải seed DB. Báo cáo xuất JSON gồm settings,
tổng thống kê và replay đại diện cho mỗi kết quả (LEFT/RIGHT/DRAW).
Không gọi API dungeon/tower, không cấp EXP/thưởng, không sửa dữ liệu người chơi.

## Quyền truy cập

- Backend Development: người dùng đã đăng nhập được dùng Lab.
- Môi trường khác: cần `BattleLab:Enabled=true` và role `Admin` trong claims.
- Không tự bật trên production. API trả 404 nếu không đủ điều kiện.
- Một batch tại một thời điểm trên mỗi API instance; request khác nhận 429.
- Timeout 45 giây được kiểm tra giữa các trận, không ngắt giữa vòng mô phỏng đồng bộ.
- Không cần migration mới cho Lab. Cần triển khai đồng thời backend và frontend.

## Cách thử K nhanh

### Nhập config JSON

Trong mục **Cấu hình từ file JSON**, bấm **Tải config hiện tại (.json)** để lấy
file với ID tướng đúng của DB hiện tại. Sửa file, chọn lại và bấm **Áp dụng config vào Lab**.
File tối đa 64 KB; file sai không thay đổi form. Đây là thay thế toàn bộ config,
không merge với form đang mở, không cập nhật DB và không tự chạy trận.
Config xuất mới có dạng `{ "schemaVersion": 2, "settings": { ... } }`; settings gồm
`seed`, `count`, `maxRounds`, `defenseConstant`, `left`, `right`.
Mỗi phần tử đội gồm `heroTemplateId`, `position`, `mode`, và `stats` hoặc `build`.
Trong CUSTOM, có thể bỏ qua các trường trong `stats`: giá trị thiếu lấy từ template DB.
Không thêm kỹ năng vào JSON; kỹ năng do backend lấy từ DB.
Nút tải báo cáo JSON + replay là định dạng khác, không dùng làm file config.
Sau khi áp dụng config, báo cáo và baseline cũ được xóa khỏi giao diện.

Kiểm tra bộ đọc config: `node --test Tests/dcs-game/battle-lab-config.test.cjs`.

1. Chọn Thanh Thái bên trái và tank bên phải.
2. Nhập đúng chỉ số cần kiểm tra (ATK, phép, DEF/MR, HP, SPD, crit).
3. Giữ nguyên seed, số trận, đội hình; chạy K=100, ghim baseline.
4. Chạy K=1000, so sánh tỷ lệ thắng, số vòng và sát thương mỗi tướng.
5. Mở replay đại diện và panel log để xem các hit vật lý/phép.
6. Xuất báo cáo trước khi chỉnh DB; chỉ đổi một nhóm thông số mỗi lần.

## Các topic cân bằng

| Topic | Thông số cần kiểm soát | Tiêu chí / bài test |
|---|---|---|
| Luật sát thương | K, giáp/MR, xuyên giáp, bạo kích, khiên, đỡ hộ | Damage theo các mốc DEF/MR, không trừ giáp hai lần |
| Võ tướng và vai trò | Chỉ số gốc/tăng trưởng, tank/DPS/support, phẩm chất | Cùng đầu tư, thay một tướng trong đội chuẩn, đo tỷ lệ thắng |
| Kỹ năng | Hệ số, số hit/mục tiêu, điều kiện, lan truyền | Single/AOE, burst/sustained, thời gian kết liễu |
| Tốc độ và năng lượng | SPD, năng lượng đầu trận, gain/cost | Số lần dùng chiêu, lợi thế đi trước, vòng lặp hồi năng lượng |
| Hiệu ứng | Khống chế, kháng hiệu ứng, stack, dispel, refresh | Số lượt không được hành động, uptime và chuỗi khống chế |
| Hồi phục và phòng hộ | Heal, shield, lifesteal, giảm damage | Effective heal, overheal, khả năng gây bế tắc |
| Đội hình và phối hợp | Vị trí, target, faction, tương tác nội tại | Đổi bên, đổi vị trí, ma trận đội hình khắc chế |
| Trang bị | Main/substats, roll, set, rarity, enhancement | Mức tăng sức mạnh theo chi phí; đồ cũ và đồ sinh mới |
| Tiến trình | Level, sao, aura, đột phá | Chênh lệch mỗi mốc; tránh bước nhảy không kiểm soát |
| PvE | Quái/boss, hệ số tầng, cơ chế, giới hạn mạng | Tỷ lệ qua ải, spike độ khó, ngưỡng gear/level |
| Kinh tế | Drop, gold/EXP, vật liệu, refund, giới hạn nhận | Thời gian đạt nâng cấp, nguồn/tiêu tài nguyên, exploit |
| Lực chiến | Trọng số và giá trị hiệp lực | Sai lệch dự báo; không ép mọi đội cùng power thắng 50% |

## Giới hạn cần hiểu

- Lab chưa có draft kỹ năng, DB preset/versioning,
  chạy ma trận tự động hoặc breakdown sát thương từng bước.
- Chưa có metrics riêng cho shield/đỡ hộ/CC uptime; báo cáo dùng statistics hiện có.
- Dùng cùng mapper và engine như BattleService. Những thuộc tính chưa được engine
  hỗ trợ không tự có tác dụng chỉ vì xuất hiện trên UI tướng.
- Kỹ năng lấy mới từ DB mỗi batch. Cùng seed nhưng DB kỹ năng thay đổi thì không
  đảm bảo kết quả cũ; replay đã xuất giữ nguyên timeline để xem lại.
- CUSTOM chỉ lưu chỉ số cuối; BUILD mô phỏng build dựa trên template và cấu hình DB.
- UI/API live với dữ liệu SQL thật cần kiểm tra sau deploy; build/unit test không thay thế bước này.

## Kiểm thử

`dotnet test Tests/dcs-game/GAME.Domain.Tests/GAME.Domain.Tests.csproj --no-restore`

Kiểm tra thực tế: mở menu với Development; chọn cả hai đội; test duplicate vị trí,
thiếu skill NORMAL, batch và replay; xác nhận ví/hành trang/EXP không thay đổi;
kiểm tra 404 ngoài Development và 429 khi chạy song song.
