# Tiến Dũng Xuân — Skills

## Lối chơi

Pháp sư Rare mở giao tranh bằng debuff kháng phép nhẹ. AoE vừa phải, dễ bị sát thủ áp sát.

## Basic — Khai Bút Đầu Xuân

- `TIEN_DUNG_XUAN_BASIC`, `NORMAL`, `ENEMY_SINGLE`, Magic, `100% Magic Damage`, có thể crit nếu hệ thống cho phép magic crit.
- 20% gây `SILENCE` trong 1 lượt. Không stack, tái áp dụng refresh.
- VFX 1 s: kéo một nét mực vàng-đen trong không khí; nét mực hóa thành projectile giấy mỏng và vỡ thành cánh mai ở mục tiêu.

## Energy — Vạn Tự Khai Hoa

- `TIEN_DUNG_XUAN_TEN_THOUSAND_GLYPHS`, cost 100, `ENEMY_ALL`, Magic.
- Gây `70% Magic Damage` lên toàn bộ địch, không crit.
- Mỗi mục tiêu có 30% nhận `MAGIC_RESISTANCE -15%` trong 2 lượt; refresh, không stack.
- Timeline 1.9 s: vẽ vòng 500 ms; giấy ấn tỏa ra 450 ms; đồng loạt impact 350 ms; cánh mai tan 600 ms.
- VFX dùng ký hiệu trừu tượng, tuyệt đối không render chữ thật. Damage number cùng nhịp impact, debuff icon sau 180 ms.

## Asset raster

- `spring-calligraphy-seal.png`: ấn phép giấy đỏ/vàng với nét mực trừu tượng, không chữ, alpha thật.

```text
Use case: stylized-concept
Asset type: transparent magical calligraphy seal for a game skill
Primary request: a floating spring festival paper seal, layered vermilion paper, warm golden border, expressive abstract black ink strokes that are not readable language, a few tiny apricot blossom motifs.
Style/medium: polished hand-painted mobile RPG VFX asset, elegant and lightweight Rare-tier magic.
Composition/framing: isolated front view, centered, clean silhouette, generous transparent padding.
Lighting/mood: warm gold glow around the paper edges.
Constraints: true transparent alpha; absolutely no readable letters, words or real calligraphy; no character, scene, logo, watermark, frame, white matte or fake checkerboard.
```

## Implementation

- Dùng effect chung `DAMAGE`, `SILENCE`, `STAT_DEBUFF`; không cần handler riêng.
- Hệ số, 20/30%, duration và magic-resistance modifier lấy từ DB.
- Seal xuất hiện trên battle VFX host tại vùng đội địch; cánh hoa và ink trail dùng CSS/SVG.

## Đặc tả gameplay và VFX triển khai

| Thuộc tính | Khai Bút Đầu Xuân | Vạn Tự Khai Hoa |
|---|---|---|
| Công thức | `1.00 × MagicDamage` | `0.70 × MagicDamage` mỗi địch |
| Target | `ENEMY_SINGLE` | `ENEMY_ALL` |
| Crit | Theo cờ magic crit của hệ thống | Không |
| Status | 20% `SILENCE`, 1 lượt, refresh, 1 tầng | 30% `MagicResistance -15%`, 2 lượt, refresh, 1 tầng |

- Basic 1.00 s: nâng bút 160 ms, kéo nét mực cong 240 ms, mảnh giấy bay thẳng có easing 250 ms, chạm ở 650 ms; lõi vàng, viền mực đen và ba cánh mai, hit flash 80 ms; damage number ở impact, silence icon ở 800 ms; thu bút về idle ở 1,000 ms.
- Energy 1.90 s: vẽ vòng 0–500 ms, seal trung tâm mở và các bản sao tỏa tới anchor địch 500–950 ms, đồng loạt ép xuống ở 1,150 ms, damage number cùng nhịp; icon giảm kháng phép ở 1,330 ms nếu proc; giấy/mực tan thành cánh mai đến 1,900 ms. Không dùng chữ thật, không che UI, shake 1 px/60 ms.
- x2/x4: các seal có thể giảm particle count nhưng không bỏ frame impact; tất cả tọa độ tương đối với team anchor nên mirror tự nhiên.
- Đồng đội hợp: magic DPS theo sau debuff. Bị khắc chế bởi assassin, resistance cao và cleanse; khắc chế đội đông/magic resistance thấp.

## Hợp đồng dữ liệu và checklist Rare

- `DAMAGE`, `SILENCE`, `STAT_DEBUFF`, `ENEMY_SINGLE`, `ENEMY_ALL` đã có; không cần effect/selector/handler mới.
- Formula phải tham chiếu đúng Magic Damage thay vì ATK nếu schema hỗ trợ; chance, duration, stat code và modifier lấy từ dữ liệu.
- AoE 70% + debuff 30% và basic silence 20% không bảo đảm khống chế, không tự bảo vệ: nằm trong ngân sách Rare.
