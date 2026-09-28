# Tiến Dũng Xuân — Character Appearance

## Ảnh nguồn và nhận diện

- Nguồn: `HrkUi/src/assets/images/dcs-game/tien-dung-xuan.png`.
- Người trưởng thành, tóc đen chải gọn, kính chữ nhật; áo dài vàng, dáng khoanh tay nghiêm nghị giữa không khí Tết.

## Định hướng

- Code `TIEN_DUNG_XUAN`; Rare Mage, phe Ngô, hàng sau.
- Concept: thư pháp sư mùa xuân, dùng bút lớn và giấy điều khiển luồng phép.
- Magic Damage/Accuracy khá; HP/DEF thấp, SPD trung bình.
- Giữ gương mặt, kính, tóc và áo dài vàng; tinh giản họa tiết, thêm bút thư pháp chiến đấu và ống cuộn giấy bên hông.
- Màu: vàng mai `#efb62d`, đỏ son `#b92d2b`, mực đen `#18171a`, xanh ngọc điểm xuyết.

## Prompt tạo ảnh

```text
Use case: stylized-concept
Asset type: full-body turn-based RPG character render
Input image: use tien-dung-xuan.png as identity reference; preserve the mature Vietnamese face, rectangular glasses, neatly styled black hair, composed expression and yellow áo dài silhouette.
Primary request: redesign him as a Rare-tier spring calligraphy mage named Tiến Dũng Xuân.
Subject: full-body elegant but modest yellow áo dài with subtle blossom weave over black trousers, ink-stained cuffs, scroll case at the waist, holding an oversized calligraphy brush like a staff; calm three-quarter casting stance.
Style/medium: polished stylized-realistic mobile RPG art, culturally respectful, clean readable silhouette.
Composition/framing: head-to-shoes, brush fully visible, generous transparent padding.
Lighting/mood: warm spring-gold key light with restrained red and ink-black accents.
Constraints: genuinely transparent alpha; preserve identity; no festival scenery, banners, readable calligraphy, text, logo, watermark, card frame, giant crown, divine aura, white matte or cropped weapon.
```

## Negative prompt và kiểm tra

`background festival, readable Chinese or Vietnamese characters, text, logo, watermark, extra brush, emperor crown, ornate Mythic armor, cropped feet, white background, fake alpha`.

Giữ chất trí thức và mùa xuân; trang phục không được biến thành hoàng đế hoặc thần tiên.

## Hồ sơ đội hình và đầu ra

- `heroCode`: `tien-dung-xuan`; Rare; Mage/Ngô; hàng sau; Magic.
- Xu hướng: Magic Damage/Accuracy khá, SPD trung bình, Crit thấp–trung bình, HP/DEF thấp, Magic Resistance trung bình. Hợp magic DPS; bị assassin/silence khắc chế; mạnh trước đội đông kháng phép thấp.
- Silhouette: tóc chải gọn + kính chữ nhật, áo dài vàng thẳng, bút dài chéo thân và ống cuộn nhỏ. Bút không vượt khung, tà áo không che giày.
- Chất liệu: gấm vàng hoa chìm, gỗ sơn đen, lông bút mực; key light vàng ấm, rim xanh ngọc rất nhẹ để không chìm trên nền vàng.
- Asset đầu ra: `tien-dung-xuan-rare.png`. QA: giữ đúng mặt/kính/tóc/áo dài; đủ bút/toàn thân; không chữ thật; 8–12% padding; alpha thật.
