# Tiến Dũng Tổng Đài — Character Appearance

## Ảnh nguồn và nhận diện

- Nguồn: `HrkUi/src/assets/images/dcs-game/tien-dung-call-video.png`.
- Gương mặt trưởng thành góc cạnh, kính chữ nhật đen, tóc đen vuốt gọn; áo xanh navy và tư thế đang nhìn màn hình.

## Hồ sơ nhân vật

- `heroCode`: `tien-dung-tong-dai`; Epic; Mage/Ngô; hàng sau; Magic.
- Chỉ số: Magic Damage, Accuracy và SPD khá; HP/DEF thấp, Resistance trung bình.
- Vai trò: AoE và lùi action bar. Hợp đội nhanh/AoE; bị assassin, Silence và Resistance khắc chế.
- Silhouette: kính, tóc vuốt, áo khoác điều phối navy dài ngang hông, headset một tai, bảng gọi hologram nổi bên tay.
- Màu: navy `#17335c`, cyan dữ liệu `#40c8e8`, cam tín hiệu `#ef8a35`, bạc và đen.
- Đầu ra: `tien-dung-tong-dai-epic.png`.

## Prompt tạo ngoại hình

```text
Use case: identity-preserve
Asset type: full-body Epic-tier turn-based RPG character
Input image: use tien-dung-call-video.png as strict identity reference. Preserve the exact mature face, rectangular black glasses, neatly swept black hair, skin tone, focused expression and head proportions. Do not remove or redesign the glasses and do not replace the person.
Primary request: extend him into Tiến Dũng Tổng Đài, an Epic communications mage controlling battlefield tempo.
Subject: fitted navy dispatcher coat over a simple dark-blue shirt, silver-cyan seam hardware, one-ear tactical headset positioned without covering the face, compact signal bracer and a floating translucent call-panel made only of abstract shapes with no readable text; dark trousers and practical boots; poised casting stance with one hand routing light lines.
Style/medium: polished stylized-realistic mobile RPG, near-future communications fantasy, Epic detail, clean silhouette.
Composition: full body, three-quarter view, face and glasses clear, all limbs visible, 10% transparent padding.
Lighting: cyan screen glow, soft orange signal accents, neutral rim.
Constraints: true transparent alpha; preserve identity; no chair, room, monitor background, readable UI, phone brand, logo, watermark, card frame, giant satellite, Mythic aura or cropped limbs.
```

## Negative prompt và QA

`different face, missing glasses, changed hairstyle, smile change, anime face, gamer chair, bedroom, computer monitor, readable interface, brand, logo, text, giant headset, background, white matte, fake transparency, cropped shoes`.

- Giữ mặt/kính/tóc nguyên bản; panel không chứa chữ; toàn thân, silhouette hàng sau và alpha sạch.

