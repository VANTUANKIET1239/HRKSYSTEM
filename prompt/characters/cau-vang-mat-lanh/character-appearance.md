# Cậu Vàng Mặt Lạnh — Character Appearance

## Ảnh nguồn và nhận diện

- Nguồn: `HrkUi/src/assets/images/dcs-game/meme-cho-hai-huoc.jpg`.
- Chó Shiba màu vàng nâu, thân tròn, ngồi lệch chân, mắt lim dim và vẻ bình thản hài hước.
- Đây là linh thú bốn chân/ngồi, không nhân hóa thành người hoặc thay bằng giống chó khác.

## Hồ sơ nhân vật

- `heroCode`: `cau-vang-mat-lanh`; Epic; Tanker/Quần; hàng trước; Physical.
- Chỉ số: HP, DEF, Resistance cao; ATK/Crit/Accuracy thấp, SPD rất thấp.
- Vai trò: bảo hộ toàn đội bằng shield và damage reduction. Hợp carry mỏng; bị phá khiên, giảm DEF và damage theo % HP khắc chế.
- Silhouette: Shiba tròn ngồi, giáp vải đỏ–nâu phủ vai/lưng, chuông ngọc trước cổ và hai tấm bảo hộ chân trước.
- Màu: vàng lông `#bf793e`, kem `#e4c69d`, đỏ trầm `#8f3630`, jade `#45a57a`, đồng.
- Đầu ra: `cau-vang-mat-lanh-epic.png`.

## Prompt tạo ngoại hình

```text
Use case: identity-preserve
Asset type: full-body Epic-tier animal guardian for a turn-based RPG
Input image: use meme-cho-hai-huoc.jpg as strict identity reference. Preserve the exact Shiba Inu head, sleepy narrowed eyes, muzzle, ears, fur colors, round body shape and deadpan seated expression. Do not turn the dog into a human, another breed or an aggressive wolf.
Primary request: redesign only the wearable equipment as Cậu Vàng Mặt Lạnh, an Epic protective spirit dog.
Subject: the same round seated Shiba wearing a modest deep-red and brown padded guardian mantle across shoulders and back, two small bronze foreleg guards, a jade bell pendant and a compact round side-shield strapped beside the body; calm immovable pose, humorous dignity.
Style/medium: polished stylized-realistic mobile RPG animal art, detailed fur and cloth, Epic craftsmanship without Mythic size or aura.
Composition: entire dog including ears, tail/body outline and all paws visible, three-quarter view, 10% transparent padding.
Lighting: warm fur key light, subtle jade rim, restrained bronze highlights.
Constraints: genuine transparent alpha; preserve dog identity and expression; no human anatomy, standing biped, weapon in paw, crown, wings, scenery, floor, text, logo, UI, watermark, white matte or cropped paws.
```

## Negative prompt và QA

`human body, anthropomorphic warrior, different dog breed, wolf, angry eyes, open mouth, changed ears, slim dog, giant armor, crown, wings, sword, meme text, room, landscape, white background, fake alpha, cropped paws`.

- Đúng Shiba tròn/mắt lim dim/tư thế gốc; giáp không che mặt hoặc phá silhouette; đủ chân và alpha sạch.

