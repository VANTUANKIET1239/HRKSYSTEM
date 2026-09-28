# Nguyên Xàm Lớn — Character Appearance

## Ảnh nguồn và nhận diện

- Nguồn: `HrkUi/src/assets/images/dcs-game/nguyen-xam-lon.png`.
- Tóc mái đen, ánh mắt tự tin, tư thế chống hai tay vào hông; áo caro đỏ và áo phao navy.

## Định hướng

- Code `NGUYEN_XAM_LON`; Rare Warrior, phe Quần, hàng trước.
- Concept: “đại ca khu phố” dùng găng tay âm thanh, áp đảo đối phương bằng khí thế và lời nói.
- ATK/HP khá, DEF trung bình, SPD thấp; mạnh khi đánh tuyến trước, yếu trước burst phép.
- Giữ tóc, ánh mắt, áo caro đỏ và áo phao; thêm bảo hộ cẳng tay, găng khuếch đại âm và giày chiến đấu nhẹ.
- Màu: đỏ caro `#a3323c`, navy `#192d4c`, xám thép `#68768a`, trắng lạnh.

## Prompt tạo ảnh

```text
Use case: stylized-concept
Asset type: full-body turn-based RPG character render
Input image: use nguyen-xam-lon.png as identity reference; preserve the recognizable youthful face, straight black fringe, confident eyes, plaid red shirt and hands-on-hips attitude.
Primary request: redesign him as a Rare-tier street bruiser called Nguyên Xàm Lớn.
Subject: full-body fighter wearing a red plaid combat shirt, sleeveless navy padded vest, gray forearm guards, compact sound-amplifier gauntlets and practical boots; cocky stance with one fist forward and the other at the hip.
Style/medium: polished stylized-realistic mobile RPG art, grounded and humorous, clear silhouette.
Composition/framing: full body, three-quarter view, all hands and footwear visible.
Lighting/mood: cool neutral rim light, bold but not epic.
Constraints: true transparent alpha; preserve identity; no crowd, chair, background, text, logo, watermark, card UI, oversized muscles, giant weapon, legendary aura, white matte or cropped limbs.
```

## Negative prompt và kiểm tra

`crowd, meme background, text, brand, watermark, extra hands, bodybuilder, crown, throne, firearm, white background, fake transparency`.

Đầu ra phải thể hiện tư thế ngông nhưng vui, không biến thành phản diện Mythic.

## Hồ sơ đội hình và đầu ra

- `heroCode`: `nguyen-xam-lon`; Rare; Warrior/Quần; hàng trước; Physical.
- Xu hướng: ATK/HP khá, Crit/Accuracy trung bình, DEF/Resistance trung bình thấp, SPD thấp. Hợp physical DPS tận dụng giảm DEF; bị magic burst/slow khắc chế; ép tốt tanker DEF vừa.
- Silhouette: tóc mái, vest phao không tay, hai găng khuếch âm vuông nhỏ và thế chống hông. Không tăng cơ bắp quá mức hoặc thêm súng.
- Chất liệu: flannel caro, nylon chần bông, thép sơn mờ; rim light trắng lạnh và phản quang đỏ vừa đủ.
- Asset đầu ra: `nguyen-xam-lon-rare.png`. QA: nhận ra mắt/tóc/áo caro/tư thế; đủ toàn thân/hai găng; không crowd/chữ/logo; 8–12% padding; alpha sạch.
