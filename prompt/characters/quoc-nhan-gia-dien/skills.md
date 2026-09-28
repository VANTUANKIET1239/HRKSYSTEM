# Quốc Nhân Giả Diện — Skills

## Lối chơi

Assassin Epic săn hàng sau, tạo Bleed và tăng nhịp chí mạng. Không hồi phục, khiên hoặc hard control.

## Basic — Vết Cắt Ngụy Trang

- `QUOC_NHAN_GIA_DIEN_BASIC`, `NORMAL`, `ENEMY_SINGLE`, Physical, `120% ATK`, có thể crit.
- 35% gây `BLEED` bằng `30% ATK` mỗi đầu lượt trong 2 lượt; refresh, tối đa 1 tầng từ skill này.
- Timeline 0.85 s: tan thành khói 0–180 ms; lướt chéo 180–420 ms; slash/damage 420 ms; Bleed icon 580 ms; lùi về idle 850 ms.

## Energy — Dạ Hành Phân Ảnh

- `QUOC_NHAN_NIGHT_PHANTOMS`, `ENERGY`, cost 100, `ENEMY_BACK_ROW`, Physical.
- Gây `125% ATK` cho mỗi mục tiêu hàng sau, có thể crit.
- Sau impact, caster nhận `Crit +15%` trong 2 lượt; refresh, không stack.
- Timeline 1.75 s: tạo hai dư ảnh 0–350 ms; lao theo ba quỹ đạo 350–850 ms; các mục tiêu bị chém đồng thời ở 900 ms; damage number cùng frame; buff Crit ở 1,150 ms; khói tan/recovery đến 1,750 ms.
- Nếu không còn hàng sau, selector fallback theo hệ thống. x4 giữ silhouette dư ảnh và impact; mirror theo team side.

## Asset raster

- `phantom-mask.png`: mặt nạ bóng ma xanh–trắng nguyên bản, răng trừu tượng, dùng làm afterimage stamp.

```text
Use case: stylized-concept
Asset type: transparent phantom impact emblem
Primary request: an original urban phantom mask emblem, midnight-blue shell, abstract white jagged grin, electric-blue edge glow and a tiny red accent, polished Epic game VFX quality.
Composition: centered front view, clean silhouette, generous alpha padding, readable at small size.
Constraints: true alpha; no real skull anatomy, letters, brand, gang symbol, character head, scenery, UI, watermark, white/black matte or checkerboard.
```

## Implementation và cân bằng

- Dùng `DAMAGE`, `BLEED`, `STAT_BUFF`, `ENEMY_SINGLE`, `ENEMY_BACK_ROW`; không handler mới.
- Bleed scaling/duration/chance và Crit modifier data-driven. Ultimate mạnh khi hàng sau đông nhưng mất giá trị khi chỉ còn một mục tiêu/tuyến sau trống.

