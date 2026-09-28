# Trường Kiệt Chu Mỏ — Character Appearance

## Ảnh nguồn và nhận diện

- Nguồn: `HrkUi/src/assets/images/dcs-game/truong-kiet-chu-mo.png`.
- Tóc đen dày, mắt nhắm, môi chu rất rõ; áo thun đen với hình thương hiệu cần loại bỏ ở bản game.
- Biểu cảm hài hước là điểm nhận diện chính, không được đổi thành gương mặt lạnh lùng generic.

## Hồ sơ nhân vật

- `heroCode`: `truong-kiet-chu-mo`; Epic; Assassin/Ngụy; hàng sau; Physical.
- Chỉ số: ATK, SPD, Crit, Accuracy cao; HP/DEF/Resistance thấp.
- Vai trò: burst đơn và Panic. Hợp đội giảm DEF/tăng Accuracy; bị khiên, Resistance và tanker vật lý khắc chế.
- Silhouette: tóc phồng, môi chu, cổ áo cao, áo khoác sát thủ ngắn, hai vòng cộng hưởng ở cổ tay.
- Màu: đen `#15141b`, magenta `#dd4d91`, tím `#713f8f`, bạc lạnh, lõi hồng trắng.
- Đầu ra: `truong-kiet-chu-mo-epic.png`.

## Prompt tạo ngoại hình

```text
Use case: identity-preserve
Asset type: full-body Epic-tier turn-based RPG character
Input image: use truong-kiet-chu-mo.png as strict identity reference; preserve the exact youthful face, thick black hairstyle, closed eyes, puckered lips, skin tone, head angle and humorous expression. Do not open the eyes, relax the lips, beautify or replace the face.
Primary request: extend him into Trường Kiệt Chu Mỏ, an Epic sonic assassin whose puckered expression launches compressed resonance attacks.
Subject: short black combat jacket with deep-magenta lining, fitted charcoal shirt without any brand mark, slim tactical trousers, light boots, two compact silver-magenta resonance rings around the wrists, small audio crystals at the belt; agile side-on stance with one hand guiding a circular sound blade, face fully visible.
Style/medium: polished stylized-realistic mobile RPG, fashionable and comedic but combat-readable, Epic detail without Mythic spectacle.
Composition: full body, three-quarter battle view, all limbs and rings visible, 10% transparent padding.
Lighting: dark neutral key with magenta rim and white sound core.
Constraints: true transparent alpha; preserve head/expression exactly; no logo, readable shirt text, lipstick exaggeration, romance scene, other person, scenery, UI, watermark, legendary aura or cropped limbs.
```

## Negative prompt và QA

`different face, open eyes, normal mouth, altered lips, new hairstyle, adult face, beauty retouch, giant lips, kiss partner, brand logo, text, microphone, gun, background, white matte, fake alpha, cropped feet`.

- Mặt và biểu cảm phải khớp ảnh gốc; motif âm thanh trừu tượng, không tình dục hóa; toàn thân/alpha thật.

