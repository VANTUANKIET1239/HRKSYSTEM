# Quang Vinh THCS — Skills

## Lối chơi

Tanker Rare dễ dùng: tự tăng chống chịu bằng đánh thường và tạo khiên nhỏ cho đội bằng ultimate. Sát thương thấp, không có khiêu khích chắc chắn.

## Basic — Trực Nhật Kiên Cường

- `QUANG_VINH_THCS_BASIC`, `NORMAL`, `ENEMY_SINGLE`, Physical, `90% ATK`, có thể crit.
- Sau khi đánh, tự nhận `DEF +10%` trong 1 lượt; refresh, không stack.
- Events: `SKILL_CAST → DAMAGE → STATUS_APPLIED/REFRESHED → SKILL_COMPLETED`.
- VFX 900 ms: dựng khiên, bước tới húc nhẹ; cung va chạm kem–đỏ và bụi nhỏ; trở lại guard pose.

## Energy — Hàng Rào Danh Dự

- `QUANG_VINH_THCS_HONOR_BARRIER`, cost 100, toàn bộ địch và toàn bộ đồng minh.
- Húc khiên tạo sóng gây `65% ATK` Physical lên toàn địch, không crit.
- Sau đó tạo khiên cho toàn bộ đồng minh bằng `10% Max HP của người thi triển`, tồn tại 2 lượt; cùng source thì replace/refresh, không cộng vô hạn.
- Timeline 1.8 s: cast 400 ms; impact 450 ms; barrier lan tới đồng minh 450 ms; recovery 500 ms.
- VFX: khiên chạm đất, vòng gạch đỏ chạy ngang, các mảnh ánh sáng ghép thành mái chắn kem-vàng. Damage number ở impact; shield status sau đó 250 ms.

## Asset raster

- `school-barrier.png`: huy hiệu/khiên học đường nguyên bản, kem–đỏ–navy, không chữ; alpha thật. Các mái chắn lặp lại dùng CSS transform/opacity.

```text
Use case: stylized-concept
Asset type: transparent RPG shield VFX asset
Primary request: an original round school-guardian shield emblem, cream enamel center, brick-red rim, navy steel reinforcement and a small abstract star motif, sturdy but modest Rare-tier design.
Style/medium: polished painted game asset with clean silhouette and subtle magical glow.
Composition/framing: front-facing shield, centered, generous transparent padding.
Constraints: genuine transparent alpha; no readable text, letters, real school emblem, logo, character, scenery, watermark, white matte or fake checkerboard.
```

## Implementation

- Tái sử dụng `DAMAGE`, `STAT_BUFF`, `SHIELD`, `ENEMY_ALL`, `ALLY_ALL`.
- Coefficient, 10% DEF, shield formula và duration phải ở DB; không viết cứng trong handler.
- Đảm bảo event khiên mang `statusInstanceId` và UI xóa đúng lớp khiên khi về 0.

## Đặc tả gameplay và VFX triển khai

| Thuộc tính | Trực Nhật Kiên Cường | Hàng Rào Danh Dự |
|---|---|---|
| Công thức | `0.90 × ATK` | `0.65 × ATK` mỗi địch + khiên `0.10 × MaxHP(caster)` |
| Target | `ENEMY_SINGLE`, buff `SELF` | damage `ENEMY_ALL`, shield `ALLY_ALL` |
| Crit | Có | Không |
| Status | `DEF +10%`, 1 lượt, refresh, 1 tầng | `SHIELD`, 2 lượt, replace cùng source |

- Basic 0.90 s: thủ thế 150 ms, bước/húc 300 ms, va chạm ở 450 ms với cung sáng kem lõi vàng và bụi gạch đỏ; damage number hiện ở impact, icon DEF ở 600 ms; lùi 220 ms và về guard idle. Không xuyên qua vị trí mục tiêu.
- Energy 1.80 s: đóng khiên xuống đất 0–400 ms; shockwave kem–đỏ đi từ caster đến vùng địch 400–850 ms, damage toàn bộ ở 800 ms; sáu mảnh ánh sáng ghép thành mái chắn quanh đồng minh 850–1,300 ms; số shield và icon hiện ở 1,100 ms; recovery đến 1,800 ms. Shake 2 px/70 ms, hit flash vàng nhạt, không che thanh máu.
- x2/x4: giữ frame đóng khiên, impact và barrier hoàn chỉnh; vòng shockwave scale bằng biến tiến độ timeline. Mirror toàn bộ vector theo team side.
- Đồng đội hợp: DPS cần thời gian tích energy; yếu trước dispel/khiên phá và sát thương theo % HP. Khắc chế đội burst nhẹ/AoE kéo dài; không có taunt nên không vô hiệu hóa sát thủ hàng sau.

## Hợp đồng dữ liệu và checklist Rare

- `DAMAGE`, `STAT_BUFF`, `SHIELD`, `SELF`, `ENEMY_ALL`, `ALLY_ALL` đều đã có; DefaultSkillHandler đủ nếu thứ tự effect được giữ bằng display order.
- Coefficient, shield scaling source stat, duration và replacement key phải nằm trong `HrkSkillEffect*`; không hard-code 10% trong handler.
- Ultimate chỉ gây 65% AoE và khiên 10% Max HP, không heal/taunt/khống chế: đúng vai trò tanker Rare, thấp hơn bộ công cụ Epic/Mythic.
