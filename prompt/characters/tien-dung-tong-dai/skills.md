# Tiến Dũng Tổng Đài — Skills

## Lối chơi

Control Mage Epic gây AoE vừa và phá nhịp lượt theo xác suất. Không khóa lượt chắc chắn hoặc tự bảo vệ.

## Basic — Ping Cuộc Gọi

- `TIEN_DUNG_TONG_DAI_BASIC`, `NORMAL`, `ENEMY_SINGLE`, Magic, `105% Magic Damage`, có thể crit nếu hệ thống cho magic crit.
- 25% gây `SILENCE` trong 1 lượt; refresh, tối đa 1.
- Timeline 0.95 s: chạm panel 0–200 ms; gói sáng cyan bay 200–500 ms; impact/damage 500 ms; Silence icon 660 ms; recovery 950 ms.

## Energy — Hội Nghị Khẩn Cấp

- `TIEN_DUNG_EMERGENCY_CONFERENCE`, `ENERGY`, cost 100, `ENEMY_ALL`, Magic, `90% Magic Damage` mỗi mục tiêu, không crit.
- Mỗi mục tiêu có 40% bị `ACTION_BAR_CHANGE = -15`; không tạo status kéo dài.
- Timeline 1.85 s: mở năm ô tín hiệu 0–450 ms; khóa anchor địch 450–800 ms; tia dữ liệu đồng loạt chạm ở 1,000 ms; damage number cùng impact, action-bar event ở 1,150 ms; panel đóng/recovery đến 1,850 ms.
- VFX cyan lõi trắng, góc cam; không render video/UI thật. x4 giữ lock-on/impact/event, giảm tia phụ.

## Asset raster

- `holo-call-panel.png`: khung gọi hologram không chữ, gồm năm ô trừu tượng và nút tín hiệu hình học.

```text
Use case: stylized-concept
Asset type: transparent holographic communications VFX
Primary request: a floating cyan Epic communications panel composed of five abstract glass tiles, thin silver frame lines and restrained orange signal nodes, no readable interface or familiar app layout.
Composition: slight three-quarter perspective, isolated, centered with transparent padding.
Constraints: real alpha; no words, digits, portraits, video-call brand, phone logo, character, room, watermark, solid rectangle, white/black background or fake checkerboard.
```

## Implementation và cân bằng

- Dùng `DAMAGE`, `SILENCE`, `ACTION_BAR_CHANGE`, `ENEMY_SINGLE`, `ENEMY_ALL`; handler/selector đã có.
- Roll action-bar độc lập từng mục tiêu; delta/chance/timing lấy từ parameter. 90% AoE + 40% utility là Epic nhưng không gây Stun và không chắc chắn.

