# Quốc Nhân Tốt Nghiệp Cấp 3 — Giám Khảo Luận Văn (Character Appearance & Assets)

## I. THAM CHIẾU NHẬN DIỆN & DANH TÍNH
- Ảnh nguồn gốc: `HrkUi/src/assets/images/dcs-game/quoc-nhan-tot-nghiep-cap-3.png`
- Nhân vật: Quốc Nhân Tốt Nghiệp Cấp 3
- Phẩm chất: Legendary
- Class: Mage / Controller
- Vị trí: Hàng sau (Back Row)
- Loại sát thương: Magic
- Output dự kiến: `HrkUi/src/assets/images/dcs-game/quoc-nhan-tot-nghiep-cap-3-legendary.png`

## II. QUY TẮC BẢO TOÀN NHẬN DIỆN KHUÔN MẶT (BẮT BUỘC)
- Dùng `quoc-nhan-tot-nghiep-cap-3.png` làm identity reference nghiêm ngặt.
- Giữ nguyên 100% phần đầu và khuôn mặt từ ảnh gốc:
  - Hình dáng khuôn mặt, xương hàm và cằm.
  - Đôi mắt, lông mày, sống mũi, khóe miệng.
  - Kiểu tóc trong ảnh gốc, màu tóc và đường chân tóc.
  - Kính mắt, mũ tốt nghiệp hoặc phụ kiện đầu đặc trưng nếu có trong ảnh gốc.
  - Biểu cảm nhận diện điềm đạm, nghiêm túc pha chút tự hào của ngày tốt nghiệp.
- Không chỉnh sửa mặt thành nhân vật khác.
- Chỉ thay đổi trang phục, thân người, pháp khí luận văn và tư thế thi triển ma pháp bên dưới đầu.

## III. THIẾT KẾ TRANG PHỤC & ĐẠO CỤ (LEGENDARY MAGE/CONTROLLER)
- Phong cách: Pháp sư học viện hội đồng phản biện luận văn, trang trọng, tri thức và uy quyền pháp thuật.
- Trang phục:
  - Áo choàng pháp sư cử nhân cao cấp màu tím than - đỏ bọc đô viền chỉ vàng kim pháp thuật (`#581c87`, `#7f1d1d`, `#fbbf24`).
  - Cổ áo đứng thêu phù văn tri thức cách điệu.
  - Đai lưng pháp thuật mang theo các cuộn sách và con dấu phản biện.
  - Quần âu phép thuật đen lịch lãm và giày da pháp sư đính viền vàng.
- Vũ khí & Pháp bảo:
  - Tay cầm một cuốn Ma Đạo Luận Văn (Grimoire) bay lơ lửng, tỏa ra các trang giấy pháp thuật và bút lông ma thuật đỏ-vàng.
  - Tuyệt đối không dùng chữ viết đọc được trên trang sách để giữ tính huyền ảo.
- Tư thế:
  - Đứng đĩnh đạc hàng sau, một tay nâng cuốn sách ma thuật mở ra, tay kia vung bút ma thuật điểm chỉ luận điểm vào đối phương.
- Đẳng cấp Legendary:
  - Các trang giấy pháp thuật đỏ-vàng xoay quanh tay; không có hào quang thiên thần, không cánh thần thánh Mythic.

## IV. PROMPT TẠO ẢNH TOÀN THÂN (CHARACTER APPEARANCE PROMPT)

```text
Use case: strict identity-preserve character generation
Asset type: full-body Legendary turn-based RPG mage controller hero
Input image: HrkUi/src/assets/images/dcs-game/quoc-nhan-tot-nghiep-cap-3.png as the strict identity reference.
Strict face preservation: keep the exact facial structure, eyes, eyebrows, nose, mouth, glasses/headwear (if in source), graduation hairstyle and proud scholarly expression from the source image. Do not alter or substitute the head with a generic wizard face.
Subject: full-body academy grand thesis master mage named "Quốc Nhân Tốt Nghiệp Cấp 3". Regal deep-purple and burgundy scholar-mage robes with runic golden embroideries, levitating arcane thesis grimoire glowing with crimson-gold spell pages and a floating crystal quill; casting scholarly debuff spells; complete full body from head to boots.
Aesthetics & Tier: Legendary quality, intellectual arcane master, polished mobile RPG art; no divine halo, no celestial angel wings, no holy transcendence.
Composition & Framing: full body head-to-toe, three-quarter view, all limbs and floating thesis book fully visible, 10% to 12% alpha padding around character.
Alpha / Transparency: genuine transparent PNG background with pure alpha cutouts. No white background, no black background, no fake checkerboard pattern, no scenery.
```

## V. NEGATIVE PROMPT & QA
```text
different head, replaced face, beauty retouch, changed hairstyle, removed glasses (if present), readable text or school name on book, cropped book, cropped boots, cropped head, white solid background, black solid background, fake checkerboard, divine halo, angel wings.
```

## VI. PROMPT TẠO ICON SKILL & STATUS (TRANSPARENT PNG)
- Tên icon: `icon-status-luan-diem.png`
- Ý nghĩa: Luận Điểm (Dấu chú thích đỏ - vàng trên mục tiêu)
```text
Clean mobile RPG status debuff icon, glowing arcane thesis mark / critique quill bookmark symbol in warm gold and deep crimson (#fbbf24, #b91c1c), stylized scholarly manuscript seal, minimalist game vector icon, genuine transparent background, no text, crisp edges for small HUD status display.
```
