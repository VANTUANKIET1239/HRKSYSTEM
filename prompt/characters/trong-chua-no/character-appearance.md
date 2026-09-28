# Trọng Chưa Nổ — Character Appearance

## Ảnh nguồn và nhận diện

- Nguồn: `HrkUi/src/assets/images/dcs-game/trong-chua-no.png`.
- Dáng cao, mảnh; tóc đen; nụ cười rộng; mũ bảo hiểm đen và kính chắn lớn là dấu hiệu mạnh nhất.
- Trang phục thể thao xanh đen, tư thế giơ dấu V tạo cảm giác liều lĩnh nhưng vui vẻ.

## Định hướng

- Tên: **Trọng Chưa Nổ**; code: `TRONG_CHUA_NO`; file đề xuất: `trong-chua-no.png`.
- Rare Assassin, phe Ngụy, hàng sau; ATK/SPD/Accuracy khá, HP/DEF thấp.
- Concept: trinh sát phá hoại đô thị, luôn nói “chưa nổ đâu” ngay trước lúc bom nổ.
- Bảng màu: than đen `#151922`, xanh thép `#315b78`, cyan `#43c8df`, cam cảnh báo `#ff7a28`.
- Giữ mũ, kính, nụ cười và dáng mảnh; thay điện thoại bằng bộ kích nổ cầm tay, thêm áo giáp nhẹ và túi chất nổ nhỏ.

## Prompt tạo ảnh

```text
Use case: stylized-concept
Asset type: full-body game character render for a turn-based RPG
Input image: use trong-chua-no.png strictly as the identity reference; preserve the recognizable youthful face, black hair, broad smile, slim body proportions, black helmet and oversized protective visor.
Primary request: redesign him as a Rare-tier urban demolition scout named Trọng Chưa Nổ, playful and reckless rather than elite or legendary.
Subject: full body, lightweight charcoal-and-steel-blue combat sports suit, compact protective pads, orange warning tabs, utility belt with two small fictional energy charges, handheld remote detonator, black helmet and clear smoky visor; relaxed combat stance with one hand holding the detonator and the other making a V sign.
Style/medium: polished stylized-realistic mobile RPG character art, clean readable silhouette, believable fabric and light armor materials.
Composition/framing: full body from head to boots, three-quarter view, all equipment visible, generous transparent padding.
Lighting/mood: neutral studio rim light with restrained cyan and orange highlights; mischievous, agile, low-rarity adventurer mood.
Constraints: genuinely transparent background with alpha; preserve identity; no scenery, text, logo, UI, card frame, explosion behind him, white matte, or rectangular shadow; no cropped limbs; no real-world brand marks; do not make him Legendary or Mythic.
```

## Negative prompt

`background, room, bed, wall, text, watermark, logo, brand, cropped feet, cropped helmet, extra fingers, extra weapons, military insignia, photorealistic bomb, gore, legendary aura, giant armor, white background, checkerboard background`

## Checklist

- Khuôn mặt, mũ, kính và nụ cười còn nhận ra được.
- Đủ toàn thân, silhouette gọn cho hàng sau.
- Bộ kích nổ là đồ hư cấu, không mô tả cấu tạo bom thực tế.
- Alpha thật, không viền trắng và không có background.

## Hồ sơ đội hình và đầu ra

- `heroCode`: `trong-chua-no`; phẩm chất Rare; Assassin/Ngụy; ưu tiên hàng sau; Physical.
- Xu hướng chỉ số: SPD > ATK > Accuracy/Crit; HP, DEF và Magic Resistance thấp. Đồng đội hợp: tanker giữ tuyến và support nạp energy. Đối thủ khắc chế: sát thủ đánh hàng sau, khiên và DEF vật lý cao. Mục tiêu thuận lợi: mage/support mỏng.
- Silhouette: mũ tròn + visor lớn, thân hình cao mảnh, hai túi charge nhỏ và remote vuông; tránh ba lô/giáp nặng làm mất dáng trinh sát.
- Chất liệu: vải thể thao lì, giáp polymer xước nhẹ, kính khói bán phản chiếu; ánh sáng viền cyan đủ rõ trên nền sáng lẫn tối.
- Asset đầu ra đề xuất: `trong-chua-no-rare.png`, PNG/WebP alpha, toàn thân, không tự đổi tên ảnh nguồn.
- QA: mặt và nụ cười giống ảnh; đủ mũ/kính/tay/chân/remote; không chi tiết chất nổ thực tế; không logo; bounding box có 8–12% khoảng trống; alpha góc ảnh bằng 0.
