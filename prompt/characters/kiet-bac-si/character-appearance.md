# Kiệt Bác Sĩ — Character Appearance

## Ảnh nguồn và nhận diện

- Nguồn: `HrkUi/src/assets/images/dcs-game/kiet-bac-si.png`.
- Ảnh ghép bác sĩ bán thân: blouse trắng, ống nghe, cà vạt xanh, khoanh tay; phần đầu là nét vẽ với tóc vàng, kính/viền mắt đỏ và khẩu trang cyan.
- Giữ nguyên phong thái kỳ quặc giữa ảnh thật và nét vẽ; không “sửa” thành một người hoàn toàn khác.

## Hồ sơ nhân vật

- `heroCode`: `kiet-bac-si`; Epic; Support/Thục; hàng sau; Magic/Heal.
- Chỉ số: Magic Damage, Resistance và Accuracy khá; HP trung bình; DEF, ATK, Crit và SPD thấp–trung bình.
- Vai trò: healer ổn định, tăng kháng hiệu ứng. Hợp tanker và đội hình kéo dài; bị Silence, burst hàng sau và anti-heal khắc chế.
- Silhouette: đầu vẽ tóc vàng + khẩu trang cyan, blouse dài, ống nghe, máy quét sinh lực một tay, túi cứu thương nhỏ bên hông.
- Bảng màu: trắng lạnh `#f4f7f8`, cyan `#38d7df`, xanh navy `#183b66`, đỏ cảnh báo `#d34343`, bạc y tế.
- Chất liệu: vải blouse sạch, polymer y tế, kính và kim loại xước nhẹ. Epic thể hiện bằng đường sáng cyan có kiểm soát, không hào quang thần thánh.
- Đầu ra đề xuất: `kiet-bac-si-epic.png`.

## Prompt tạo ngoại hình

```text
Use case: identity-preserve
Asset type: full-body Epic-tier turn-based RPG character
Input image: use kiet-bac-si.png as the strict identity reference. Preserve the complete illustrated head exactly in spirit and structure: blond drawn hair, unusual red-rimmed eyes/glasses, cyan medical mask, head proportions and uncanny mixed-media identity. Do not replace it with a realistic generic doctor face.
Primary request: extend the source into a full-body Epic support healer named Kiệt Bác Sĩ, changing and completing only the body, outfit and equipment below the head.
Subject: white medical combat coat with restrained cyan luminous seams, navy shirt and blue patterned tie, stethoscope, compact life-scanner gauntlet, small emergency satchel, dark practical trousers and clean medical boots; one hand projects a small cyan vital orb, calm clinical stance.
Style/medium: polished stylized-realistic mobile RPG art while deliberately preserving the source head's hand-drawn collage character; Epic quality, not Mythic.
Composition/framing: full body, three-quarter battle view, face/head unobstructed, all limbs and equipment visible, 10% transparent padding.
Lighting/mood: clean cool key light, cyan rim and tiny red diagnostics accent.
Constraints: genuine transparent alpha; one character; preserve the source head and mask; no hospital room, patient, readable medical text, real logo, UI, card frame, watermark, white matte, giant wings, divine halo or cropped limbs.
```

## Negative prompt và QA

`different head, realistic replacement face, changed mask, changed hair, missing red eyes, beauty retouch, generic doctor, patient, hospital background, text, logo, watermark, syringe close-up, gore, angel wings, white background, fake transparency, cropped feet`.

- Nhận ra ngay đầu vẽ/khẩu trang đặc trưng; toàn thân cân đối với đầu; orb không che mặt; alpha góc ảnh bằng 0; không có nền trắng giả.

