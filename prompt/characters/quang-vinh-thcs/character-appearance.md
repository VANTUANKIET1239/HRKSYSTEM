# Quang Vinh THCS — Character Appearance

## Ảnh nguồn và nhận diện

- Nguồn: `HrkUi/src/assets/images/dcs-game/quang-vinh-thcs.png`.
- Khuôn mặt tròn, tóc đen ngắn, nụ cười hiền; áo thể dục học sinh kem viền đỏ là dấu hiệu chính.

## Định hướng

- Code `QUANG_VINH_THCS`; Rare Tanker, phe Thục, hàng trước.
- Concept: hộ vệ học đường dùng tấm bảng/khiên chống đỡ cho bạn bè.
- HP/DEF/Resistance cao trong nhóm Rare; ATK/SPD thấp.
- Giữ khuôn mặt, tóc, vóc dáng chắc và màu áo kem–đỏ; nâng áo thể dục thành giáp vải dày, thêm khiên huy hiệu tròn và dây đeo cặp.
- Màu: kem `#f1dfb8`, đỏ gạch `#9e2f32`, navy `#233a5e`, vàng nhạt `#e7b84b`.

## Prompt tạo ảnh

```text
Use case: stylized-concept
Asset type: full-body game character render for a turn-based RPG
Input image: use quang-vinh-thcs.png as identity reference; preserve the round youthful face, short black hair, warm smile and sturdy proportions.
Primary request: redesign him as a Rare-tier school guardian named Quang Vinh THCS.
Subject: full-body front-line protector wearing a cream training tunic with brick-red trim over simple navy padded armor, practical sneakers and a backpack strap; carrying an original round school-emblem shield with no readable text; friendly but determined guarding stance.
Style/medium: polished stylized-realistic mobile RPG character art, readable low-rarity equipment, cloth and painted metal textures.
Composition/framing: full body, three-quarter view, shield not covering the face, all limbs visible, transparent padding.
Lighting/mood: warm neutral light, dependable and approachable.
Constraints: genuine transparent alpha; preserve identity; no school logo, readable writing, scenery, other people, card frame, UI, watermark, white matte, giant fantasy armor or legendary aura.
```

## Negative prompt và kiểm tra

`school background, readable badge, text, logo, watermark, cropped shield, hidden face, extra limbs, giant armor, cape, crown, white background, fake transparency`.

Đầu ra phải giữ khuôn mặt hiền, silhouette chắc, toàn thân và alpha sạch.

## Hồ sơ đội hình và đầu ra

- `heroCode`: `quang-vinh-thcs`; Rare; Tanker/Thục; hàng trước; Physical.
- Xu hướng: HP/DEF/Resistance cao trong Rare, SPD/ATK/Crit thấp, Accuracy trung bình. Hợp DPS cần được che chắn; bị khắc chế bởi phá khiên, giảm DEF và magic burst; khắc chế poke/AoE nhẹ.
- Silhouette: thân chắc, khiên tròn cỡ vừa lệch khỏi mặt, dây ba lô chéo; không cape, không đại giáp. Tư thế chân rộng, vai hạ, khiên hướng về địch.
- Chất liệu: vải cotton dày, đệm navy, kim loại sơn men kem–đỏ; rim light vàng nhạt và outline navy giúp đọc trên sân sáng/tối.
- Asset đầu ra: `quang-vinh-thcs-rare.png`. QA: giữ mặt/tóc/nụ cười và màu áo gốc; đủ toàn thân/khiên; huy hiệu hoàn toàn trừu tượng; 8–12% padding; alpha thật, không viền trắng.
