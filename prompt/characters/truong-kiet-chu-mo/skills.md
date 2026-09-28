# Trường Kiệt Chu Mỏ — Skills

## Lối chơi

Assassin Epic dồn damage đơn và gây Panic có xác suất cao. Không AoE, sustain hoặc buff phòng thủ.

## Basic — Hôn Gió Cảnh Cáo

- `TRUONG_KIET_CHU_MO_BASIC`, `NORMAL`, `ENEMY_SINGLE`, Physical, `115% ATK`, có thể crit.
- 30% gây `Accuracy -15%` trong 1 lượt; refresh, tối đa 1.
- Timeline 0.90 s: nén vòng âm 0–220 ms; lưỡi sóng cong bay 220–500 ms; impact/damage ở 500 ms; debuff icon ở 650 ms; recovery 900 ms.

## Energy — Nụ Hôn Đoạt Hồn

- `TRUONG_KIET_SOUL_KISS`, `ENERGY`, cost 100, `ENEMY_SINGLE`, Physical, `205% ATK`, có thể crit.
- 45% gây `PANIC` trong 1 lượt; refresh, không stack.
- Timeline 1.65 s: cast 0–350 ms; ba vòng cộng hưởng hội tụ 350–750 ms; projectile lõi trắng-magenta chạm ở 900 ms; damage number cùng impact, Panic icon ở 1,080 ms; vòng âm tan/recovery đến 1,650 ms.
- Shake 2 px/70 ms; không zoom/overlay. x4 giữ rõ charge, impact và status; mirror theo team anchor.

## Asset raster

- `resonance-kiss-seal.png`: ấn cộng hưởng hình đôi cung đối xứng, không phải môi người thật, magenta–tím, alpha thật.

```text
Use case: stylized-concept
Asset type: transparent sonic impact seal
Primary request: an abstract Epic resonance seal made of two opposing curved magenta sound waves around a white core, subtle violet echo rings, energetic and playful without depicting realistic lips.
Composition: centered front view, clean silhouette, generous transparent padding.
Constraints: genuine alpha; no human mouth, face, heart, text, logo, character, scenery, UI, watermark, white/black matte or checkerboard.
```

## Implementation và cân bằng

- Dùng `DAMAGE`, `STAT_DEBUFF`, `PANIC`, `ENEMY_SINGLE`; không cần handler mới.
- Coefficient/chance/duration/stat lấy từ dữ liệu. 205% đơn + Panic 45% mạnh hơn Rare nhưng đánh đổi bằng không AoE/phòng thủ.

