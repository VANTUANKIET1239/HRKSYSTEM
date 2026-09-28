# Cậu Vàng Mặt Lạnh — Skills

## Lối chơi

Tanker Epic thuần bảo hộ: damage thấp, chậm, đổi lại có shield và giảm sát thương đội. Không taunt chắc chắn, heal hoặc phản damage.

## Basic — Ngồi Im Phán Xét

- `CAU_VANG_MAT_LANH_BASIC`, `NORMAL`, `ENEMY_SINGLE`, Physical, `100% ATK`, có thể crit.
- Sau hit, tự nhận `DEF +15%` trong 2 lượt; refresh, không stack.
- Timeline 0.95 s: liếc 0–180 ms; chuông ngọc phát xung 180–480 ms; impact/damage 480 ms; DEF icon 640 ms; trở về tư thế ngồi ở 950 ms.

## Energy — Bình Thản Che Chở

- `CAU_VANG_CALM_GUARD`, `ENERGY`, cost 100, `ALLY_ALL`, không gây damage.
- Tạo shield bằng `12% Max HP của caster` cho mỗi đồng minh, tồn tại 2 lượt; cùng source replace/refresh.
- Đồng thời cho `DAMAGE_REDUCTION 12%` trong 2 lượt; refresh, không stack.
- Timeline 1.85 s: chuông rung 0–350 ms; năm ấn chân ngọc hiện dưới đồng minh 350–800 ms; barrier dựng 800–1,100 ms; shield number ở 950 ms, reduction icon ở 1,120 ms; glow tan đến 1,850 ms.
- VFX jade lõi trắng, viền đồng/đỏ, không che nhân vật. x4 giữ dấu chân, barrier và status; không shake camera.

## Asset raster

- `jade-guard-bell.png`: chuông ngọc–đồng với motif dấu chân trừu tượng, không chữ.

```text
Use case: stylized-concept
Asset type: transparent guardian bell VFX prop
Primary request: a compact jade and bronze guardian bell for an Epic spirit dog, round green stone core, deep-red braided collar loop and one abstract paw-shaped engraving without text, warm protective glow.
Composition: isolated three-quarter view, centered, generous transparent padding, easy to scale.
Constraints: genuine alpha; no letters, pet brand, dog portrait, hand, scenery, UI, watermark, white/black background or fake checkerboard.
```

## Implementation và cân bằng

- Dùng `DAMAGE`, `STAT_BUFF`, `SHIELD`, `DAMAGE_REDUCTION`, `SELF`, `ALLY_ALL`; không cần handler/selector mới.
- Shield amount/source/duration/replacement và reduction đều data-driven. Ultimate không damage/heal/cleanse; phá khiên và %HP damage vẫn khắc chế được.

