# Kiệt Mái Xéo — Kiếm Khách Phong Nhã (Character Appearance & Assets)

## I. THAM CHIẾU NHẬN DIỆN & DANH TÍNH
- Ảnh nguồn gốc: `HrkUi/src/assets/images/dcs-game/kiet-mai-xeo.jpg`
- Nhân vật: Kiệt Mái Xéo
- Phẩm chất: Legendary
- Class: Assassin
- Vị trí: Hàng sau (Back Row)
- Loại sát thương: Physical
- Output dự kiến: `HrkUi/src/assets/images/dcs-game/kiet-mai-xeo-legendary.png`

## II. QUY TẮC BẢO TOÀN NHẬN DIỆN KHUÔN MẶT (BẮT BUỘC)
- Dùng `kiet-mai-xeo.jpg` làm identity reference nghiêm ngặt.
- Giữ nguyên 100% phần đầu và khuôn mặt từ ảnh gốc:
  - Hình dáng khuôn mặt, cằm, xương hàm đặc trưng.
  - Đôi mắt, lông mày, sống mũi, khóe miệng.
  - Màu da thực tế, không qua filter làm trắng hay "làm đẹp" thành hotboy generic.
  - Kiểu tóc mái xéo biểu tượng: giữ đúng hướng chải, độ dài sợi tóc phủ trán, đường chân tóc và màu tóc.
  - Biểu cảm nhận diện sắc sảo, tự tin, phong trần của ảnh gốc.
- Tuyệt đối không thay thế bằng khuôn mặt kiếm khách anime xa lạ.
- Chỉ thay đổi trang phục, thân người, vũ khí và tư thế chiến đấu bên dưới phần đầu.

## III. THIẾT KẾ TRANG PHỤC & ĐẠO CỤ (LEGENDARY ASSASSIN)
- Phong cách: Kiếm khách phong nhã lãng tử phương Đông pha trộn giáp hiệp khách gọn gàng.
- Trang phục:
  - Áo lụa phong lôi màu xanh cổ vịt / teal sẫm viền ánh bạc và lam ngọc (`#0d9488`, `#06b6d4`, `#1e293b`).
  - Giáp vai nhẹ bằng kim loại chạm khắc hoa văn cuồng phong cách điệu.
  - Ống tay gọn gàng quấn băng kiếm sĩ, đai lưng ngọc phong kiếm.
  - Quần hiệp khách đen thẫm, ủng da cao cổ di chuyển êm ái.
- Vũ khí:
  - Một thanh trường kiếm thanh mảnh, lưỡi kiếm sáng ánh thép lạnh và gợn sóng phong khí màu teal (`#14b8a6`).
- Tư thế:
  - Dáng đứng nghiêng ba phần tư (3/4 battle stance), tay phải cầm kiếm hướng chéo xuống đất, tay trái co nhẹ tụ phong linh khí.
  - Vạt áo hơi bay nhẹ theo chiều gió kiếm.
- Đẳng cấp Legendary:
  - Đường vân sáng màu teal tinh tế trên lưỡi kiếm và giáp, không có cánh thiên thần hay hào quang thần thánh kiểu Mythic.

## IV. PROMPT TẠO ẢNH TOÀN THÂN (CHARACTER APPEARANCE PROMPT)

```text
Use case: strict identity-preserve character generation
Asset type: full-body Legendary turn-based RPG assassin hero
Input image: HrkUi/src/assets/images/dcs-game/kiet-mai-xeo.jpg as the absolute identity reference.
Strict face preservation: keep the exact facial structure, eyes, eyebrows, nose, mouth, skin tone, iconic sweeping side-fringe hairstyle (mái xéo), hairline, and facial expression from the source image. Do not alter, regenerate, beautify, or substitute the face with a generic anime swordsman.
Subject: full-body martial-arts assassin swordsman named "Kiệt Mái Xéo". Elegant teal-silk martial robe with dark slate trousers and silver-trimmed leather vambraces; holding a sleek wind-honed rapier/katana with subtle teal breeze trails; dynamic three-quarter combat ready stance, coat tails swaying gently in wind; full body visible from head to boots.
Aesthetics & Tier: Legendary quality, sharp craftsmanship, polished fantasy game concept art; no oversized divine wings, no sacred angel halos, no celestial god-tier aura.
Composition & Framing: complete full body, head-to-toe, all limbs and sword fully visible without any cropping, 10% to 12% clean alpha breathing space padding around the character.
Alpha / Transparency: true PNG transparent background with perfect alpha channel. No white background, no black background, no fake checkerboard pattern, no scenery.
```

## V. NEGATIVE PROMPT & QA
```text
different head, generic anime face, altered hairstyle, symmetrical bangs, beautified face, missing side fringe, cropped sword, cropped boots, cropped head, white solid background, black solid background, fake transparency checkerboard, divine halos, angel wings, gigantic aura, messy lines, watermark, signature.
```

## VI. PROMPT TẠO ICON SKILL & STATUS (TRANSPARENT PNG)
- Tên icon: `icon-status-phong-an.png`
- Ý nghĩa: Dấu ấn Phong Ấn (Phong linh khí tụ 3 tầng)
```text
Clean mobile RPG status buff icon, three stylized swirling wind vortex blades in luminous teal cyan (#14b8a6) forming a sharp triangular seal, minimalist game vector icon, genuine transparent background, no text, no frame, high contrast, crisp edges for small display.
```
