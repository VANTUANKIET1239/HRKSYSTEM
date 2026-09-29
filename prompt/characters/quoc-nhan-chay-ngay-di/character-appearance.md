# Quốc Nhân Chạy Ngay Đi — Người Giữ Nến Đỏ (Character Appearance & Assets)

## I. THAM CHIẾU NHẬN DIỆN & DANH TÍNH
- Ảnh nguồn gốc: `HrkUi/src/assets/images/dcs-game/quoc-nhan-chay-ngay-di.png`
- Nhân vật: Quốc Nhân Chạy Ngay Đi
- Biệt danh: Người Giữ Nến Đỏ
- Phẩm chất: Legendary
- Class: Mage / Controller
- Vị trí: Hàng sau (Back Row)
- Loại sát thương: Magic
- Output dự kiến: `HrkUi/src/assets/images/dcs-game/quoc-nhan-chay-ngay-di-legendary.png`

## II. QUY TẮC BẢO TOÀN NHẬN DIỆN KHUÔN MẶT (BẮT BUỘC)
- Dùng `quoc-nhan-chay-ngay-di.png` làm identity reference nghiêm ngặt.
- Giữ nguyên 100% phần đầu và khuôn mặt từ ảnh gốc:
  - Cấu trúc khuôn mặt, đường quai hàm, cằm.
  - Đôi mắt, dáng lông mày, sống mũi, khóe môi.
  - Màu da thực tế, không qua filter biến đổi.
  - Kiểu tóc và đường chân tóc đúng như trong ảnh gốc.
  - Biểu cảm nhận diện đặc trưng đầy kịch tính, lôi cuốn và bí ẩn.
- Không thay thế bằng khuôn mặt nhân vật khác.
- Chỉ thay đổi trang phục, thân người, giá nến đỏ và tư thế thi triển hỏa thuật bên dưới đầu.

## III. THIẾT KẾ TRANG PHỤC & ĐẠO CỤ (LEGENDARY RED CANDLE MAGE)
- Phong cách: Pháp sư bóng đêm giữ ngọn nến đỏ huyền bí (mystic crimson candle keeper), trang phục gothic bí ẩn pha chất ma pháp hiện đại.
- Trang phục:
  - Măng-tô dài phong cách dark fantasy màu đen tuyền và đỏ thẫm huyết dụ (`#0f172a`, `#991b1b`, `#ef4444`).
  - Cổ áo cao lót nhung đỏ, khuy cài bạc chạm khắc hoa văn ngọn lửa ma thuật.
  - Áo gile tối màu bên trong, găng tay da đen huyền thuật.
  - Quần ôm tối giản và bốt da đen bóng tạo dáng di chuyển linh hoạt.
- Vũ khí & Pháp khí:
  - Một đế nến cổ bằng đồng đen nạm đá đỏ, trên thắp một ngọn nến sáp đỏ đang cháy rực ngọn lửa đỏ-cam mãnh liệt.
  - Vệt tàn lửa đỏ rơi nhẹ tạo cảm giác ma mị, huyền ảo.
- Tư thế:
  - Đứng nghiêng, một tay nâng giá nến đỏ ngang ngực để ánh sáng đỏ hắt nhẹ lên một bên vai áo, tay kia vung vạt áo tạo luồng hỏa tức chuẩn bị quét dọc hàng trận đối thủ.
- Đẳng cấp Legendary:
  - Ánh lửa nến đỏ ma mị, bóng lửa nhỏ lướt dưới chân; không có hào quang thần thánh, không cánh thiên thần Mythic.

## IV. PROMPT TẠO ẢNH TOÀN THÂN (CHARACTER APPEARANCE PROMPT)

```text
Use case: strict identity-preserve character generation
Asset type: full-body Legendary turn-based RPG dark mage controller hero
Input image: HrkUi/src/assets/images/dcs-game/quoc-nhan-chay-ngay-di.png as the absolute identity reference.
Strict face preservation: keep the exact facial features, bone structure, eyes, eyebrows, nose, mouth, hair, hairline and intense dramatic expression from the source image. Do not alter or substitute the head and face with a generic anime face.
Subject: full-body crimson candle keeper mystic mage named "Quốc Nhân Chạy Ngay Đi". Sleek dark gothic trench coat with deep crimson velvet lining and occult silver clasps; holding an ornate dark-brass candlestick with an intensely burning crimson candle; subtle red embers trailing; dynamic ready stance preparing a line-sweeping flame surge; complete full body from head to boots.
Aesthetics & Tier: Legendary quality, dramatic and intense aura, polished mobile fantasy RPG art; no divine angel halos, no celestial wings, no godlike transcendence.
Composition & Framing: full body head-to-toe, three-quarter perspective, all limbs and candlestick clearly visible, 10% to 12% alpha padding around the character silhouette.
Alpha / Transparency: genuine transparent PNG background with pure alpha borders. No white background, no black background, no fake checkerboard pattern, no scenery.
```

## V. NEGATIVE PROMPT & QA
```text
different head, replaced face, beauty retouch, changed hairstyle, missing candle, cropped candlestick, cropped boots, cropped head, white solid background, black solid background, fake checkerboard, divine halos, angel wings, messy artifacts, watermark.
```

## VI. PROMPT TẠO ICON SKILL & STATUS (TRANSPARENT PNG)
- Tên icon: `icon-status-chay-ngay-di.png`
- Ý nghĩa: Dấu ấn Chạy Ngay Đi (Ngọn nến đỏ kết hợp vệt lửa lao đi)
```text
Clean mobile RPG status debuff icon, glowing mystical crimson candle flame fused with dynamic rushing flame sprint trails and ember sparks (#ef4444, #f97316), minimalist vector game icon, true transparent alpha background, no text, no frame, sharp distinct silhouette for small battle HUD display.
```
