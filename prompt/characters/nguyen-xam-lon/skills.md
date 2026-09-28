# Nguyên Xàm Lớn — Skills

## Lối chơi

Đấu sĩ Rare chuyên ép hàng trước. Sát thương ổn và phá DEF có điều kiện, nhưng chậm và thiếu tự bảo vệ.

## Basic — Nói Một Là Một

- `NGUYEN_XAM_LON_BASIC`, `NORMAL`, `ENEMY_SINGLE`, Physical, `110% ATK`, có thể crit.
- 20% gây `DEF -15%` trong 1 lượt; refresh, không stack.
- VFX 900 ms: chỉ tay, chữ tượng thanh trừu tượng không đọc được nén thành sóng, sau đó đấm một nhịp; hit flash đỏ-navy.

## Energy — Đại Ca Xuống Sân

- `NGUYEN_XAM_LON_BOSS_ENTERS`, cost 100, target hàng trước địch.
- Gây `105% ATK` Physical lên từng mục tiêu hàng trước, có thể crit.
- Sau impact tự nhận `ATK +10%` trong 2 lượt; refresh, không stack.
- Timeline 1.7 s: cast 350 ms; stomp 350 ms; sóng ngang 450 ms; buff 250 ms; recovery 300 ms.
- VFX: dậm chân, vòng bụi navy-đỏ bật lên; dấu áp chế giáng xuống hàng trước. Không camera zoom, shake nhẹ 80 ms.

## Asset raster

- `boss-stamp.png`: dấu ấn graffiti trừu tượng đỏ/navy, không chữ; dùng làm impact decal, alpha thật.

```text
Use case: stylized-concept
Asset type: transparent RPG impact decal
Primary request: an abstract street-boss authority stamp made from jagged red and navy paint strokes, circular impact silhouette with a cracked center, no readable lettering.
Style/medium: polished hand-painted game VFX decal, bold edges, controlled paint splatter.
Composition/framing: isolated centered emblem, front view, generous transparent padding.
Constraints: genuine transparent alpha; no words, letters, logo, character, scenery, watermark, white or black background, fake checkerboard; readable at small size.
```

## Implementation

- Dùng `DAMAGE`, `STAT_DEBUFF`, `STAT_BUFF`, selector hàng trước hiện có.
- Chance, coefficients, duration và modifier lấy từ DB. Không cần handler riêng.

## Đặc tả gameplay và VFX triển khai

| Thuộc tính | Nói Một Là Một | Đại Ca Xuống Sân |
|---|---|---|
| Công thức | `1.10 × ATK` | `1.05 × ATK` cho mỗi mục tiêu hàng trước |
| Target | `ENEMY_SINGLE` | `ENEMY_FRONT_ROW`; buff `SELF` |
| Crit | Có | Có |
| Status | 20% `DEF -15%`, 1 lượt, refresh, 1 tầng | `ATK +10%`, 2 lượt, refresh, 1 tầng |

- Basic 0.90 s: chỉ tay/căng găng 180 ms, sóng âm trừu tượng (không chữ) ép thành nắm đấm 250 ms, bước tới và impact ở 480 ms; slash trail đỏ lõi trắng, hit flash 90 ms, damage number cùng frame; icon giảm DEF ở 620 ms nếu proc; quay lại idle ở 900 ms.
- Energy 1.70 s: hạ trọng tâm 0–350 ms, dậm đất 350–700 ms, decal `boss-stamp` rơi lên tâm hàng trước và sóng ngang quét các mục tiêu 700–1,150 ms; tất cả damage number xuất hiện đồng bộ ở 1,000 ms, buff ATK ở 1,250 ms, recovery đến 1,700 ms. Bụi navy/đỏ, lõi trắng, shake 2 px/80 ms.
- Ở x4 phải giữ stamp tối thiểu 120 ms theo thời gian hiển thị; quỹ đạo và hướng sóng lấy từ anchor đội, hỗ trợ mirror.
- Đồng đội hợp: support tăng tốc và mage tận dụng giảm DEF kém hơn physical DPS; hợp nhất với physical assassin. Yếu trước magic burst, slow và hàng trước né/DEF cao; khắc chế tanker DEF trung bình nhờ debuff.

## Hợp đồng dữ liệu và checklist Rare

- Dùng effect/selector chung, không cần handler mới. Debuff và buff cùng source refresh, không cộng tầng.
- Damage, 20% proc, modifier, duration, target count và crit flag đều data-driven; VFX resolve theo skill ID qua host.
- AoE chỉ đánh hàng trước ở 105%, self-buff nhỏ và không phòng thủ/khống chế: có giá trị đầu game nhưng không vượt Rare.
