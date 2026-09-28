# Kiệt Bác Sĩ — Skills

## Lối chơi

Support Epic chuyên hồi phục và chống debuff, đổi lại gần như không tạo áp lực damage. Không cleanse, hồi sinh hoặc bất tử.

## Basic — Chẩn Mạch Từ Xa

- `KIET_BAC_SI_BASIC`, `NORMAL`, `LOWEST_HP_PERCENT`, Magic/Heal, không crit.
- Hồi `14% Max HP của caster` cho đồng minh có % HP thấp nhất.
- Cho mục tiêu `Resistance +10%` trong 1 lượt; refresh, không stack, tối đa 1.
- Events: `SKILL_CAST → HEAL → STATUS_APPLIED/REFRESHED → SKILL_COMPLETED`.
- Timeline 1.00 s: quét 0–250 ms; orb bay 250–550 ms; heal number ở 550 ms; icon Resistance ở 700 ms; recovery đến 1,000 ms.

## Energy — Phác Đồ Cấp Cứu

- `KIET_BAC_SI_EMERGENCY_PROTOCOL`, `ENERGY`, cost 100, `ALLY_ALL`, không crit.
- Hồi mỗi đồng minh `20% Max HP của caster`.
- Tăng `Resistance +15%` trong 2 lượt; refresh, không stack.
- Timeline 1.90 s: scan đội 0–450 ms; năm đường nhịp sáng nối tới đồng minh 450–950 ms; heal đồng thời ở 950 ms; buff icon ở 1,150 ms; dư quang tan đến 1,900 ms.
- VFX: lõi trắng-cyan, vòng ECG trừu tượng không có chữ, particle vuông nhỏ; không overlay và không che thanh máu. x4 giữ scan/impact/icon, giảm particle phụ.

## Asset raster

- `vital-scan-orb.png`: orb máy quét y tế cyan–bạc, lõi kính, ba vạch nhịp trừu tượng không đọc được.

```text
Use case: stylized-concept
Asset type: transparent healing VFX prop
Primary request: a compact floating medical vital-scan orb, silver polymer shell, cyan glass core, three abstract pulse lines that are not readable text, restrained red diagnostic pinlights, polished Epic mobile RPG quality.
Composition: isolated three-quarter view, centered, generous transparent padding, easy to scale and mirror.
Constraints: true transparent alpha; no words, numbers, cross logo, real medical brand, hand, character, hospital, UI, watermark, white/black background or fake checkerboard.
```

## Implementation và cân bằng

- Tái sử dụng `HEAL`, `STAT_BUFF`, `LOWEST_HP_PERCENT`, `ALLY_ALL`; không cần handler mới.
- Scaling source, 14/20%, modifiers và duration đều data-driven. VFX resolve bằng skill IDs và `kiet-bac-si`.
- Heal mạnh hơn Rare nhưng không damage, cleanse hay shield; Silence và anti-heal là điểm yếu rõ.

