# Tường Long Cấp 3 — Skills

## Lối chơi

Support Rare tăng nhịp và hồi máu vừa phải. Không có khống chế và gần như không đóng góp burst damage.

## Basic — Giao Bài Tận Nơi

- `TUONG_LONG_CAP_3_BASIC`, `NORMAL`, `ENEMY_SINGLE`, Physical, `90% ATK`, có thể crit.
- Sau khi trúng, đồng minh còn sống có năng lượng thấp nhất nhận `+10 ENERGY`; nếu chính Long thấp nhất thì có thể nhận.
- VFX 850 ms: ném một thẻ tiếp tế xanh-trắng vào địch, thẻ bật về đồng minh dưới dạng hạt sáng; damage trước, energy event sau 180 ms.

## Energy — Chuông Vào Tiết

- `TUONG_LONG_CAP_3_CLASS_BELL`, cost 100, `ALLY_ALL`.
- Hồi cho mỗi đồng minh lượng bằng `16% Max HP của người thi triển`.
- Đồng thời cho `SPD +10%` trong 1 lượt; refresh, không stack.
- Timeline 1.8 s: nâng chuông 350 ms; đánh chuông 250 ms; ba vòng âm lan 450 ms; heal/status 250 ms; recovery 500 ms.
- VFX xanh lá–đồng, vòng sóng âm rõ nhưng không che nhân vật; heal number và icon SPD xuất hiện tách nhau 120 ms.

## Asset raster

- `school-bell.png`: chuông đồng nhỏ có quai xanh, không chữ; alpha thật. Sóng âm và sparkle dùng CSS/SVG.

```text
Use case: stylized-concept
Asset type: transparent magical support prop for a turn-based RPG
Primary request: a compact bronze school bell with a forest-green leather handle, subtle warm healing glow and a simple original engraved ring pattern without text.
Style/medium: polished stylized game prop, clean silhouette, modest Rare-tier magic.
Composition/framing: isolated three-quarter view, centered with generous transparent padding.
Lighting/mood: warm bronze highlights and gentle green glow.
Constraints: genuine transparent alpha; no letters, school name, logo, character, hand, scenery, sound-wave background, watermark, white matte or fake checkerboard.
```

## Implementation

- Dùng `DAMAGE`, `ENERGY_CHANGE`, `HEAL`, `STAT_BUFF` và selectors hiện có.
- Quy tắc chọn đồng minh năng lượng thấp nhất nên nằm ở selector dùng lại được; không viết theo tên hero.
- 10 energy, 16% Max HP, 10% SPD và duration đều data-driven.

## Đặc tả gameplay và VFX triển khai

| Thuộc tính | Giao Bài Tận Nơi | Chuông Vào Tiết |
|---|---|---|
| Công thức | `0.90 × ATK` | heal `0.16 × MaxHP(caster)` mỗi đồng minh |
| Target | damage `ENEMY_SINGLE`; energy `ALLY_LOWEST_ENERGY` | `ALLY_ALL` |
| Crit | Có | Không |
| Utility | `+10 ENERGY`, tức thời | `SPD +10%`, 1 lượt, refresh, 1 tầng |

- Basic 0.85 s: rút thẻ 120 ms, ném theo cung thấp 230 ms, hit ở 390 ms với flash xanh-trắng; damage number cùng frame; thẻ hóa hạt sáng bay từ impact tới đồng minh được chọn 180 ms, energy number ở 600 ms; về idle ở 850 ms. Không hiển thị chữ trên thẻ.
- Energy 1.80 s: nâng chuông 0–350 ms, đánh chuông 350–600 ms, ba vòng âm đồng–xanh lan qua đội 600–1,050 ms; heal number ở 900 ms, icon SPD ở 1,020 ms; sparkle tan và hạ chuông đến 1,800 ms. Không camera shake, không che status hiện có.
- x2/x4: gộp particle phụ nhưng giữ ba vòng âm phân biệt; vector projectile và thứ tự anchor tự mirror theo team.
- Đồng đội hợp: carry cần energy và đội hình chậm. Bị khắc chế bởi burst/anti-heal/silence; khắc chế poke nhẹ nhờ heal đội, nhưng không cứu được mục tiêu bị dồn sát thương lớn.

## Mở rộng hệ thống cần thiết và checklist Rare

- `DAMAGE`, `ENERGY_CHANGE`, `HEAL`, `STAT_BUFF`, `ALLY_ALL` đã có. Cần selector tái sử dụng `ALLY_LOWEST_ENERGY` (tie-break: energy, HP%, position, id); không viết theo hero name.
- Mọi hệ số, target count, stat, duration và energy delta lấy từ `HrkSkillEffect*`. VFX resolve bằng skill ID/hero code tại host.
- Heal 16% Max HP + SPD 10%/1 lượt, không cleanse/khiên/khống chế; basic chỉ 90% và utility đơn mục tiêu: đúng cấp Rare.
