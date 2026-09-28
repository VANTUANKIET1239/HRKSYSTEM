# Trọng Chưa Nổ — Skills

## Lối chơi

Sát thủ Rare tốc độ cao, dồn sát thương đơn mục tiêu và có một cơ hội khống chế nhỏ. Mỏng, không có hồi phục, khiên hoặc AoE.

## Basic — Rút Chốt Thử Thôi

- Code: `TRONG_CHUA_NO_BASIC`; loại `NORMAL`; mục tiêu `ENEMY_SINGLE`; Physical; có thể chí mạng.
- Gây `105% ATK`. Không kèm debuff để giữ đúng ngân sách sức mạnh.
- Events: `SKILL_CAST → DAMAGE → SKILL_COMPLETED`.
- VFX 850 ms: hạ kính, lướt ngắn tới mục tiêu, quét một vệt cyan-cam nhỏ, hit flash 100 ms rồi lùi về. Damage number xuất hiện đúng impact khoảng 420 ms.

## Energy — Ba Giây Chưa Nổ

- Code: `TRONG_CHUA_NO_THREE_SECONDS`; `ENERGY`, cost 100; `ENEMY_SINGLE`; Physical; có thể chí mạng.
- Gây `175% ATK`; 30% gây `PANIC` trong 1 lượt. Tái áp dụng chỉ refresh, không stack.
- Events: `SKILL_CAST → projectile travel → DAMAGE → STATUS_APPLIED (nếu trúng) → SKILL_COMPLETED`.
- Timeline 1.65 s: 0–350 ms đặt charge; 350–850 ms lùi về và đèn charge nháy ba nhịp; 850–1100 ms nổ cyan/cam; 1100–1650 ms bụi tan. Không overlay tối, camera shake rất nhẹ.

## Asset raster

- `timed-charge.png`: khối charge hư cấu nhỏ, thân than đen, lõi cyan, ba đèn cam; nhìn 3/4, silhouette sạch, PNG alpha thật. Không chữ, số, dây điện thực tế hoặc nền. Slash, sparks và smoke dùng CSS/SVG.

```text
Use case: stylized-concept
Asset type: transparent turn-based RPG skill prop
Primary request: a compact fictional timed energy charge for Trọng Chưa Nổ, charcoal armored shell, cyan glowing core and three small orange indicator lights, playful sci-fi design rather than a realistic explosive.
Style/medium: polished stylized game VFX prop, crisp silhouette, moderately detailed.
Composition/framing: isolated three-quarter view, centered with generous padding.
Lighting/mood: cyan core glow with orange warning accents.
Constraints: genuinely transparent alpha background; no text, numbers, wires, logos, scenery, character, smoke cloud, watermark, white matte or fake checkerboard; suitable for scaling and mirroring.
```

## Implementation

- Dùng `DAMAGE` + `PANIC`; không cần handler riêng.
- `1.05`, `1.75`, `30%`, duration và `CAN_CRIT` đều lấy từ effect/scaling/parameter DB.
- VFX component riêng theo skill ID; charge impact có thể render ở VFX host.

## Đặc tả gameplay và VFX triển khai

| Thuộc tính | Rút Chốt Thử Thôi | Ba Giây Chưa Nổ |
|---|---|---|
| Công thức | `finalPhysicalDamage = 1.05 × ATK` | `finalPhysicalDamage = 1.75 × ATK` |
| Target | `ENEMY_SINGLE` | `ENEMY_SINGLE` |
| Crit | Có | Có |
| Status | Không | `PANIC`, 30%, 1 lượt, refresh, tối đa 1 |
| Tổng thời lượng x1 | 0.85 s | 1.65 s |

- Basic: chuẩn bị 120 ms (hạ kính, nghiêng người), lao theo cung thấp 260 ms, vệt chém cyan lõi trắng/cam chạm ở 420 ms, hit flash 90 ms và số damage xuất hiện cùng frame; lùi đúng quỹ đạo 300 ms rồi blend về idle 80 ms. Nhân vật luôn quay mặt về phía địch; dùng dấu hướng của team để mirror, không hard-code trái/phải.
- Energy: cast 0–350 ms, charge bay theo parabol thấp 350–600 ms, bám cạnh chân mục tiêu và chớp ba nhịp 600–850 ms, nổ 850 ms với lõi trắng-cyan, vành cam, bụi than và shockwave nhỏ; damage number ở 850 ms, icon `PANIC` ở 1,030 ms nếu proc; bụi tan và caster về idle ở 1,650 ms. Camera shake biên độ 2 px trong 80 ms, không overlay màn hình.
- x2/x4: timeline scale theo battle clock; ở x4 vẫn giữ tối thiểu một frame charge sáng, một frame impact và 100 ms logic cho icon status. Không tăng riêng tốc độ nhấp nháy ambience.
- Đồng đội hợp: tanker giữ mục tiêu và support tăng SPD/energy. Bị khắc chế bởi giáp vật lý, khiên và sát thủ đánh hàng sau; khắc chế pháp sư/healer mỏng khi còn đủ năng lượng.

## Hợp đồng dữ liệu và checklist Rare

- Dùng effect chung `DAMAGE` và `PANIC`; không cần skill handler riêng. Mỗi effect mang `TargetTypeCode`, scaling, `Chance`, `Duration`, `MaxStack=1` và `CanCrit` từ dữ liệu.
- UI resolve bằng `TRONG_CHUA_NO_BASIC` / `TRONG_CHUA_NO_THREE_SECONDS` và `heroCode=trong-chua-no`; VFX sân đấu render qua `battle-skill-vfx-host`.
- 175% đơn mục tiêu kèm khống chế chỉ 30%/1 lượt, không hồi phục, không khiên, không AoE: đạt ngân sách Rare và yếu hơn rõ rệt các ultimate Mythic nhiều cơ chế.
