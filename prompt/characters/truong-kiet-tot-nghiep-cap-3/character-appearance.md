# Trường Kiệt Tốt Nghiệp Cấp 3 — Thủ Khoa Huyết Bào (Character Appearance & Assets)

## I. THAM CHIẾU NHẬN DIỆN & DANH TÍNH
- Ảnh nguồn gốc: `HrkUi/src/assets/images/dcs-game/truong-kiet-tot-nghiep-cap-3.png`
- Nhân vật: Trường Kiệt Tốt Nghiệp Cấp 3
- Phẩm chất: Legendary
- Class: Tanker / Warrior
- Vị trí: Hàng trước (Front Row)
- Loại sát thương: Physical
- Output dự kiến: `HrkUi/src/assets/images/dcs-game/truong-kiet-tot-nghiep-cap-3-legendary.png`

## II. QUY TẮC BẢO TOÀN NHẬN DIỆN KHUÔN MẶT (BẮT BUỘC)
- Dùng `truong-kiet-tot-nghiep-cap-3.png` làm identity reference nghiêm ngặt.
- Giữ nguyên 100% phần đầu và khuôn mặt từ ảnh gốc:
  - Cấu trúc khuôn mặt, đường nét cằm và gò má.
  - Đôi mắt, dáng lông mày, sống mũi, nụ cười rạng rỡ của lễ tốt nghiệp.
  - Màu da tự nhiên.
  - Kiểu tóc tốt nghiệp, đường chân tóc và mũ cử nhân / phụ kiện đầu (nếu có trong ảnh gốc).
- Tuyệt đối không thay thế bằng khuôn mặt chiến binh fantasy lạ lẫm.
- Chỉ thay đổi trang phục, thân người, khiên giáp và tư thế chiến đấu bên dưới phần đầu.

## III. THIẾT KẾ TRANG PHỤC & ĐẠO CỤ (LEGENDARY TANKER/WARRIOR)
- Phong cách: Thủ khoa tốt nghiệp mặc chiến bào hộ vệ kiên cố, kết hợp lễ phục tốt nghiệp cách điệu hào hùng.
- Trang phục:
  - Áo choàng cử nhân biến tấu thành chiến bào đỏ sẫm - vàng hoàng kim (`#dc2626`, `#f59e0b`, `#1e1b4b`).
  - Giáp ngực bản lớn và giáp hộ tâm kim loại mạ vàng đồng uy dũng.
  - Giáp tay và hộ uyển chắc chắn bảo vệ tối đa cho tuyến đầu.
  - Quần chiến giáp và ủng thiết giáp nặng bước đi vững chãi.
- Vũ khí & Khiên:
  - Khiên hộ vệ hình huy hiệu cử nhân / huân chương danh dự cách điệu (không dùng logo trường học có thật), viền ánh sáng hoàng kim.
- Tư thế:
  - Đứng vững vàng ở tuyến đầu, một tay nắm chắc tấm đại khiên cắm vững xuống đất, một tay giơ nắm đấm tự tin sẵn sàng che chắn cho toàn bộ đồng minh.
- Đẳng cấp Legendary:
  - Không hào quang thánh thần hay cánh thiên sứ kiểu Mythic; thể hiện uy lực bằng kim loại nặng, vạch sáng hộ thuẫn màu đỏ-vàng danh dự kiên cố.

## IV. PROMPT TẠO ẢNH TOÀN THÂN (CHARACTER APPEARANCE PROMPT)

```text
Use case: strict identity-preserve character generation
Asset type: full-body Legendary turn-based RPG tanker warrior hero
Input image: HrkUi/src/assets/images/dcs-game/truong-kiet-tot-nghiep-cap-3.png as the strict identity reference.
Strict face preservation: keep the exact facial structure, eyes, eyebrows, nose, joyful graduation smile, skin tone, hair and head details from the source image. Do not alter or substitute the head and face with a generic fantasy warrior.
Subject: full-body frontline warrior tanker named "Trường Kiệt Tốt Nghiệp Cấp 3". Grand ceremonial crimson-and-gold graduation heraldic battle-robe fused with heavy plate armor, reinforced golden lion pauldrons, solid gauntlets, heavy greaves; firmly planting a stylized crest-shaped aegis tower shield into the ground, resolute frontline protector stance; complete full body from head to armored boots.
Aesthetics & Tier: Legendary quality, imposing guardian, polished fantasy RPG art; no divine halo, no giant angel wings, no holy transcendence.
Composition & Framing: full body head-to-toe, three-quarter angle, all limbs and shield completely visible, 10% to 12% alpha margin padding around the silhouette.
Alpha / Transparency: genuine transparent PNG with crisp alpha cutouts. No white background, no black background, no fake checkerboard pattern, no scenic background.
```

## V. NEGATIVE PROMPT & QA
```text
different head, replaced face, beauty retouch, angry grimace replacing original smile, missing graduation hair/head features, cropped shield, cropped boots, cropped head, white solid background, black solid background, fake checkerboard, divine aura, angel wings, real school logo, real school text.
```

## VI. PROMPT TẠO ICON SKILL & STATUS (TRANSPARENT PNG)
- Tên icon: `icon-status-tin-chi-danh-du.png`
- Ý nghĩa: Tín Chỉ Danh Dự (3 ô sáng đỏ - vàng)
```text
Clean turn-based RPG status buff icon, three stacked glowing honors credit medals/tokens in vibrant crimson and warm gold (#f59e0b, #dc2626), heraldic academic badge style, minimalist vector game icon, true transparent alpha background, no text, no letter, crisp high-contrast icon.
```
