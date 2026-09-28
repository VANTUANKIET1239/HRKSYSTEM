# Tường Long Cấp 3 — Character Appearance

## Ảnh nguồn và nhận diện

- Nguồn: `HrkUi/src/assets/images/dcs-game/tuong-long-cap-3.png`.
- Tóc đen cắt ngắn, nụ cười lớn, áo sơ mi trắng, quần thể thao đen, ba lô và tay vẫy chào.

## Định hướng

- Code `TUONG_LONG_CAP_3`; Rare Support, phe Thục, hàng sau.
- Concept: liên lạc viên học đường luôn đến đúng giờ, dùng chuông và ba lô tiếp tế để tăng nhịp đội hình.
- SPD/Resistance cao; HP trung bình; ATK/DEF thấp.
- Giữ khuôn mặt, tóc, áo trắng, ba lô và cử chỉ thân thiện; thêm áo khoác ngắn xanh lá, chuông đồng nhỏ và các thẻ tiếp tế trừu tượng.
- Màu: trắng `#f3f4ef`, xanh lá `#2f7d57`, đen `#22252b`, đồng `#d49b3f`.

## Prompt tạo ảnh

```text
Use case: stylized-concept
Asset type: full-body turn-based RPG character render
Input image: use tuong-long-cap-3.png as identity reference; preserve the recognizable smiling face, short black hair, white shirt, black athletic trousers, backpack and friendly waving gesture.
Primary request: redesign him as Tường Long Cấp 3, a Rare-tier school courier support hero.
Subject: full-body agile support wearing a clean white field shirt, short forest-green courier jacket, black practical trousers, reinforced sneakers and a compact supply backpack; holding a small bronze school bell in one hand while the other waves, ready to sprint.
Style/medium: polished stylized-realistic mobile RPG character art, grounded low-rarity gear, clear friendly silhouette.
Composition/framing: full body, three-quarter view, backpack and bell visible, generous transparent padding.
Lighting/mood: bright neutral rim light, energetic and dependable.
Constraints: true transparent alpha; preserve identity; no room, door, readable school badge, text, logo, watermark, card UI, giant wings, legendary aura, white matte or cropped limbs.
```

## Negative prompt và kiểm tra

`room, door, classroom, text, school logo, watermark, extra backpack, giant bell, wings, crown, cropped shoes, white background, fake transparency`.

Đầu ra phải thân thiện, cơ động, toàn thân và giữ ba lô như điểm nhận diện.

## Hồ sơ đội hình và đầu ra

- `heroCode`: `tuong-long-cap-3`; Rare; Support/Thục; hàng sau; Physical/Heal.
- Xu hướng: SPD/Resistance cao, HP trung bình, DEF/ATK/Crit thấp. Hợp carry cần energy và đội chậm; bị burst/silence/anti-heal khắc chế; mạnh trước poke kéo dài.
- Silhouette: tóc ngắn, nụ cười, áo trắng, ba lô gọn và chuông nhỏ; một tay vẫy tạo khoảng âm rõ. Không cánh/cape hoặc chuông khổng lồ.
- Chất liệu: vải sơ mi, nylon ba lô, đồng xước và da xanh; rim light xanh lá dịu, highlight đồng ấm.
- Asset đầu ra: `tuong-long-cap-3-rare.png`. QA: giữ đúng mặt/tóc/nụ cười/ba lô; đủ chuông/tay/chân; không logo trường; 8–12% padding; alpha thật.
