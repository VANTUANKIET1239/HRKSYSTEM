# Quốc Nhân Giả Diện — Character Appearance

## Ảnh nguồn và nhận diện

- Nguồn: `HrkUi/src/assets/images/dcs-game/quoc-nhan-fake.png`.
- Toàn thân gầy, bộ đồ loang xanh–trắng, khăn che mặt in hàm răng, kính tối, tóc đen, găng đen và bốt trắng cao cổ.
- Giữ mặt nạ răng và bảng màu làm dấu hiệu; xóa mọi chữ/nhãn trên trang phục mới.

## Hồ sơ nhân vật

- `heroCode`: `quoc-nhan-gia-dien`; Epic; Assassin/Ngụy; hàng sau; Physical.
- Chỉ số: SPD/Crit/ATK cao, Accuracy khá; HP/DEF/Resistance thấp.
- Vai trò: săn hàng sau, Bleed và tự tăng Crit. Hợp giảm DEF/tăng SPD; bị cleanse, shield và tanker chặn sát thương khắc chế.
- Silhouette: khăn răng, kính, áo dài bất đối xứng xanh–trắng, song đao ngắn, bốt trắng.
- Màu: midnight blue `#142843`, electric blue `#2875b9`, trắng xám `#d9e0e5`, đen, đỏ nhỏ `#c63e43`.
- Đầu ra: `quoc-nhan-gia-dien-epic.png`.

## Prompt tạo ngoại hình

```text
Use case: identity-preserve
Asset type: full-body Epic-tier turn-based RPG character
Input image: use quoc-nhan-fake.png as strict identity and silhouette reference. Preserve the visible head exactly in identity: black hair, dark glasses, black skull-tooth face covering, head angle and slim proportions. Do not reveal or invent a face beneath the mask.
Primary request: refine him into Quốc Nhân Giả Diện, an Epic urban phantom assassin.
Subject: asymmetric midnight-blue and white splatter combat coat without any lettering, fitted black underlayer, compact gloves, reinforced white high-top boots, two short abstract crescent blades with blue edges, one small red signal charm at the belt; agile low stance with coat tails suggesting motion.
Style/medium: polished stylized-realistic mobile RPG, urban phantom/graffiti theme, Epic detail without military realism or Mythic spectacle.
Composition: full body, three-quarter battle view, head/mask, both blades and boots completely visible, 10% transparent padding.
Lighting: cool blue rim, white blade core and restrained red accent.
Constraints: genuine transparent alpha; preserve mask and glasses; no readable words, brand marks, real gang symbols, firearm, monument, city scene, extra people, UI, watermark, giant aura or cropped limbs.
```

## Negative prompt và QA

`unmasked face, invented face, changed skull mask, missing glasses, different hair, readable clothing text, real brand, gang logo, gun, military uniform, monument, street background, white matte, checkerboard, extra blades, cropped boots`.

- Không lộ mặt; giữ đúng mặt nạ/kính/tóc; song đao gọn, không che đầu; alpha thật và đủ toàn thân.

