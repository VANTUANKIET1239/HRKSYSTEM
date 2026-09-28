# Văn Trọng Điện Vàng — Skills

## Lối chơi

Pháp sư Rare điều khiển nhịp lượt bằng điện. Hiệu quả khi có nhiều mục tiêu; damage đơn mục tiêu và độ bền thấp.

## Basic — Tĩnh Điện Má Hồng

- `VAN_TRONG_DIEN_VANG_BASIC`, `NORMAL`, `ENEMY_SINGLE`, Magic, `95% Magic Damage`, có thể crit.
- 25% làm giảm action bar mục tiêu 10 điểm. Không tạo status kéo dài.
- VFX 800 ms: linh cầu lóe, tia điện vàng mảnh bắn tới mục tiêu và tạo ba nhánh nhỏ; action bar text xuất hiện sau damage.

## Energy — Điện Vàng Liên Hoàn

- `VAN_TRONG_DIEN_VANG_CHAIN_LIGHTNING`, cost 100, tối đa 3 địch khác nhau.
- Mỗi mục tiêu nhận `85% Magic Damage`, không lặp mục tiêu và không gộp số hit khi thiếu địch.
- Mỗi mục tiêu có 30% bị `STUN` 1 lượt. Không stack, refresh theo quy tắc status chung.
- Timeline 1.75 s: charge orb 400 ms; tia thứ nhất 300 ms; chain hai lần, mỗi lần 250 ms; status 200 ms; residual sparks 350 ms.
- Tia có lõi trắng-vàng, viền vàng cam, đường zigzag dày và dễ nhìn ở x4; không overlay màn hình.

## Asset raster

- `electric-mascot-orb.png`: linh cầu vàng nguyên bản với hai vây năng lượng như tai, không phải nhân vật có bản quyền; alpha thật. Lightning bolts làm bằng SVG/CSS.

```text
Use case: stylized-concept
Asset type: transparent magical projectile for a turn-based RPG
Primary request: an original golden thunder-spirit orb, round energy core with two abstract angular lightning fins resembling ears, white-hot center, amber outer corona, friendly but energetic silhouette.
Style/medium: polished game VFX asset, semi-painterly energy texture, readable at small size.
Composition/framing: isolated centered orb, symmetric enough to mirror, generous transparent padding.
Constraints: genuine transparent alpha; completely original creature; no copyrighted mascot face, red cheeks, franchise symbols, text, logo, character body, scenery, watermark, white matte or fake checkerboard.
```

## Implementation

- Dùng `DAMAGE`, `ACTION_BAR_CHANGED`, `STUN`.
- Chỉ thêm `ENEMY_RANDOM_DISTINCT_3` nếu registry chưa có equivalent; target count = 3 lấy từ parameter DB.
- Không hard-code 25/30%, hệ số hoặc duration. Chain VFX render ở host theo danh sách target event.

## Đặc tả gameplay và VFX triển khai

| Thuộc tính | Tĩnh Điện Má Hồng | Điện Vàng Liên Hoàn |
|---|---|---|
| Công thức | `0.95 × MagicDamage` | `0.85 × MagicDamage` cho tối đa 3 địch khác nhau |
| Target | `ENEMY_SINGLE` | đề xuất `ENEMY_RANDOM_DISTINCT_N`, `N=3` từ parameter |
| Crit | Theo cờ magic crit | Không |
| Utility | 25% lùi action bar 10 điểm | 30% `STUN` độc lập/mục tiêu, 1 lượt, refresh, 1 tầng |

- Basic 0.80 s: nén orb 160 ms, tia vàng lõi trắng bay 220 ms, impact ở 420 ms với ba nhánh ngắn và hit flash 70 ms; damage number ngay impact, event/action-bar label ở 560 ms nếu proc; residual sparks kết thúc ở 800 ms.
- Energy 1.75 s: charge 0–400 ms, tia đầu 400–700 ms, hai lần chain 700–1,200 ms; damage từng mục tiêu đúng thời điểm tia tới, icon stun sau mỗi hit 150 ms; sparks tan và caster về idle ở 1,750 ms. Tia zigzag dày 5–7 px ở x1, không overlay và không camera zoom.
- x2/x4: mỗi jump vẫn có frame tiếp xúc riêng; giảm số spark phụ chứ không gộp hit. Dùng vị trí mục tiêu thật để dựng path, độc lập team side.
- Đồng đội hợp: AoE follow-up và đồng minh Accuracy. Bị khắc chế bởi resistance/cleanse và đội còn một mục tiêu; khắc chế đội đông chậm, nhưng stun không bảo đảm.

## Mở rộng hệ thống cần thiết và checklist Rare

- Registry hiện chưa có selector tối đa 3 địch khác nhau: thêm selector tổng quát `ENEMY_RANDOM_DISTINCT_N`, lấy `N` từ `HrkSkillEffectParameter`; nếu kiến trúc bắt buộc code cố định mới dùng `ENEMY_RANDOM_3`.
- Registry effect chung hiện chưa có `ACTION_BAR_CHANGE`; tránh tái dùng handler Mythic đặc thù. Thêm effect data-driven `ACTION_BAR_CHANGE` với signed amount/chance, hoặc bỏ utility basic nếu chưa muốn mở rộng engine.
- Damage/chance/duration/count/action-bar delta đều từ dữ liệu; không hard-code trong VFX hay handler. 85% × tối đa 3 và stun 30% độc lập phù hợp Rare, yếu rõ khi ít mục tiêu.
