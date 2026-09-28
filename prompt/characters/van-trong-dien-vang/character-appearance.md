# Văn Trọng Điện Vàng — Character Appearance

## Ảnh nguồn và nhận diện

- Nguồn: `HrkUi/src/assets/images/dcs-game/van-trong-pikachu.png`.
- Gương mặt trẻ, biểu cảm bình thản hài hước; mũ trùm thú màu vàng với hai tai dài là điểm nhận diện.

## Định hướng

- Tên tránh dùng thương hiệu trong dữ liệu mới: **Văn Trọng Điện Vàng**; code `VAN_TRONG_DIEN_VANG`; Rare Mage, phe Quần, hàng sau.
- Concept: pháp sư điện trong áo choàng linh thú vàng nguyên bản, điều khiển linh cầu điện.
- SPD/Magic Damage khá, Accuracy trung bình; HP/DEF thấp.
- Giữ gương mặt và mũ vàng hai tai, nhưng thiết kế lại motif thành linh thú sấm sét nguyên bản, không sao chép nhân vật có bản quyền.
- Màu: vàng điện `#ffd83d`, vàng trắng `#fff4aa`, than `#252632`, đỏ san hô điểm xuyết.

## Prompt tạo ảnh

```text
Use case: stylized-concept
Asset type: full-body turn-based RPG character render
Input image: use van-trong-pikachu.png only as identity and mood reference; preserve the recognizable youthful face, calm playful expression and yellow long-eared hood concept.
Primary request: redesign him as Văn Trọng Điện Vàng, a Rare-tier lightning mage with a completely original thunder-beast costume.
Subject: full-body character in a yellow hooded mantle with two long angular lightning-shaped ears, charcoal inner tunic, small insulated gloves and boots, holding a floating golden electric orb; modest adventurer clothing, not mascot pajamas.
Style/medium: polished stylized-realistic mobile RPG character art, crisp silhouette, original design.
Composition/framing: full body, three-quarter casting pose, both ears and feet fully visible, transparent padding.
Lighting/mood: warm golden electricity with subtle white core sparks; playful but combat-ready.
Constraints: genuine transparent alpha; preserve face; original thunder creature only; no copyrighted mascot markings, red cheek circles, recognizable franchise symbols, text, logo, scenery, watermark, card UI, legendary storm or white matte.
```

## Negative prompt và kiểm tra

`copyrighted mascot, Pokémon, Pikachu, red cheek circles, franchise logo, text, watermark, background, extra ears, giant lightning dragon, Mythic armor, white background, fake transparency`.

Đầu ra phải nguyên bản, giữ mũ vàng tai dài nhưng không sao chép thiết kế thương hiệu.

## Hồ sơ đội hình và đầu ra

- `heroCode`: `van-trong-dien-vang`; Rare; Mage/Quần; hàng sau; Magic.
- Xu hướng: SPD/Magic Damage khá, Accuracy trung bình, HP/DEF/Resistance thấp. Hợp AoE follow-up và buff Accuracy; bị cleanse/kháng hiệu ứng/assassin khắc chế; mạnh hơn khi địch còn đông.
- Silhouette: mũ vàng với hai tai sét góc cạnh hoàn toàn nguyên bản, áo choàng ngắn, orb nổi bên tay; không đuôi, má đỏ hay motif thương hiệu.
- Chất liệu: nỉ mờ, vải cách điện than, orb năng lượng bán trong; lõi sáng trắng-vàng và rim tím than giúp rõ ở nền sáng/tối.
- Asset đầu ra: `van-trong-dien-vang-rare.png`. QA: giữ mặt/cảm xúc/mũ tai dài nhưng thiết kế nguyên bản; đủ tai/chân/orb; không biểu tượng franchise; 8–12% padding; alpha sạch.
