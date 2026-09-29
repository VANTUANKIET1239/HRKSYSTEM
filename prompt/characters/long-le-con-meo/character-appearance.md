# Long Lê Con Mèo — Kỵ Sĩ Hộ Miêu (Character Appearance & Assets)

## I. THAM CHIẾU NHẬN DIỆN & DANH TÍNH
- Ảnh nguồn gốc: `HrkUi/src/assets/images/dcs-game/long-le-con-meo.jpg`
- Nhân vật: Long Lê Con Mèo
- Phẩm chất: Legendary
- Class: Support
- Vị trí: Hàng trước hoặc giữa (Front / Middle Row)
- Loại sát thương: Physical
- Output dự kiến: `HrkUi/src/assets/images/dcs-game/long-le-con-meo-legendary.png`

## II. QUY TẮC BẢO TOÀN NHẬN DIỆN (ĐẶC BIỆT BẮT BUỘC CẢ NGƯỜI VÀ MÈO)
- Dùng `long-le-con-meo.jpg` làm identity reference nghiêm ngặt.
- **Bảo toàn khuôn mặt của NGƯỜI**:
  - Hình dáng khuôn mặt, cằm, xương gò má.
  - Đôi mắt, dáng lông mày, sống mũi, miệng.
  - Màu da, kiểu tóc và đường chân tóc trong ảnh gốc.
  - Biểu cảm nhận diện chân thực, hiền hòa, yêu quý động vật.
- **Bảo toàn diện mạo của CON MÈO**:
  - Giữ nguyên 100% khuôn mặt của con mèo trong ảnh gốc:
  - Mắt mèo, dáng mũi mèo, vệt lông/đốm lông đặc trưng quanh mặt.
  - Hình dáng tai mèo và biểu cảm đáng yêu / tinh nghịch đặc trưng của chú mèo trong ảnh.
  - Không thay bằng con mèo 3D xa lạ hay giống mèo khác.
- Chỉ thay đổi trang phục kỵ sĩ, thân người, tư thế và trang bị bên dưới đầu.

## III. THIẾT KẾ TRANG PHỤC & ĐẠO CỤ (LEGENDARY SUPPORT KNIGHT)
- Phong cách: Kỵ sĩ hộ miêu đồng hành (Cat Knight Companion), ấm áp, đáng tin cậy và linh hoạt.
- Trang phục người:
  - Giáp hiệp sĩ kết hợp vải da thú êm ái màu nâu ấm, kem vàng và viền hổ phách (`#d97706`, `#78350f`, `#fef3c7`).
  - Hộ tâm phiến chạm nổi dấu chân mèo cách điệu.
  - Đai da có túi đựng thảo dược cứu thương và thức ăn cho mèo.
  - Găng tay da mềm mại, ủng đi đường chắc chắn.
- Tạo hình chú mèo:
  - Chú mèo nhỏ có quàng một chiếc khăn choàng mini màu đỏ cam hoặc gắn chuông nhỏ phong cách hiệp sĩ.
  - Trong ảnh toàn thân, chú mèo ngồi kiêu hãnh trên vai hoặc đứng tự tin dưới chân / bên cạnh Long Lê.
- Tư thế:
  - Dáng đứng che chở, một tay giữ khiên nhỏ hoặc trượng hỗ trợ phong cách móng vuốt, một tay vuốt ve hoặc cùng chú mèo sẵn sàng xuất kích.
- Đẳng cấp Legendary:
  - Ánh sáng vàng hổ phách và dấu vuốt cách điệu bao quanh; không có cánh thiên sứ hay thần tính Mythic.

## IV. PROMPT TẠO ẢNH TOÀN THÂN (CHARACTER APPEARANCE PROMPT)

```text
Use case: strict dual identity-preserve character generation
Asset type: full-body Legendary turn-based RPG support hero with companion animal
Input image: HrkUi/src/assets/images/dcs-game/long-le-con-meo.jpg as the absolute identity reference.
Strict face preservation: keep the exact human facial structure, eyes, eyebrows, nose, mouth, hair and facial expression from the source image. CRITICAL: ALSO strictly preserve the exact cat face, facial fur pattern, eyes, whiskers, and cute expression of the cat from the source image. Do not replace the human face with a generic knight, and do not replace the cat with an arbitrary stock cat.
Subject: full-body feline guardian support knight named "Long Lê Con Mèo". Warm amber, leather and mail armor with stylized paw-crest cuirass; holding a supportive scout cudgel/shield; the exact pet cat from the reference sits loyally beside his boots or on his armored pauldron, sporting a tiny crimson ranger bandana; ready for battle stance; complete full body from head to boots.
Aesthetics & Tier: Legendary quality, heartwarming heroic bond, polished mobile fantasy RPG art; no divine halos, no angel wings, no holy transcendence.
Composition & Framing: full body head-to-toe, both Long Lê and his companion cat fully visible, unobstructed, 10% to 12% alpha padding around the duo.
Alpha / Transparency: genuine transparent PNG background with clean alpha borders. No white background, no black background, no fake checkerboard pattern, no scenery.
```

## V. NEGATIVE PROMPT & QA
```text
different human head, generic face, different cat breed, generic cat face, cat missing, human missing, cropped cat, cropped human limbs, cropped head, white solid background, black solid background, fake checkerboard, giant angel wings, god halo, gore, watermark.
```

## VI. PROMPT TẠO ICON SKILL & STATUS (TRANSPARENT PNG)
- `icon-status-cat-scratch.png` (Vết Cào Thường):
```text
Clean mobile RPG status debuff icon, three dynamic slashing cat claw scratch marks in radiant bright gold yellow (#eab308, #facc15), sharp animal scratch effect, minimalist vector game icon, true transparent alpha background, no text, no frame.
```
- `icon-status-deep-cat-scratch.png` (Vết Cào Sâu):
```text
Clean mobile RPG status debuff icon, three heavy jagged deep beast claw laceration marks in intense crimson red (#dc2626, #991b1b) with sharp tearing edges, distinctly shaped like deep feline claw rend (not liquid dripping bleed droplets), minimalist game vector icon, true transparent alpha background, no text.
```
- `icon-status-cat-companion.png` (Mèo Đồng Hành):
```text
Clean mobile RPG buff / companion icon, stylized charming cat head silhouette with perked ears and glowing protective golden eyes (#f59e0b) surrounded by a soft amber protective crest, minimalist vector game icon, genuine transparent background, no text.
```
