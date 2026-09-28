# Năm tướng Epic mới — 29/09/2026

Năm ảnh được xác định bằng mốc sửa liên tiếp từ 00:01 đến 00:06 ngày 29/09/2026 và đã được quan sát trực tiếp. Ảnh gốc chỉ là tham chiếu nhận diện, không đổi tên hoặc chỉnh sửa trong bước thiết kế.

| Ảnh nguồn | Tên đề xuất | `heroCode` | Class | Faction | Damage | Vai trò | Basic | Energy |
|---|---|---|---|---|---|---|---|---|
| `kiet-bac-si.png` | Kiệt Bác Sĩ | `kiet-bac-si` | Support | Thục | Magic/Heal | Hồi phục và kháng hiệu ứng | Chẩn Mạch Từ Xa | Phác Đồ Cấp Cứu |
| `truong-kiet-chu-mo.png` | Trường Kiệt Chu Mỏ | `truong-kiet-chu-mo` | Assassin | Ngụy | Physical | Dồn sát thương, gây Hoảng Loạn | Hôn Gió Cảnh Cáo | Nụ Hôn Đoạt Hồn |
| `tien-dung-call-video.png` | Tiến Dũng Tổng Đài | `tien-dung-tong-dai` | Mage | Ngô | Magic | AoE và trì hoãn lượt | Ping Cuộc Gọi | Hội Nghị Khẩn Cấp |
| `quoc-nhan-fake.png` | Quốc Nhân Giả Diện | `quoc-nhan-gia-dien` | Assassin | Ngụy | Physical | Đánh hàng sau, Bleed | Vết Cắt Ngụy Trang | Dạ Hành Phân Ảnh |
| `meme-cho-hai-huoc.jpg` | Cậu Vàng Mặt Lạnh | `cau-vang-mat-lanh` | Tanker | Quần | Physical | Khiên đội và giảm sát thương | Ngồi Im Phán Xét | Bình Thản Che Chở |

## Phân tích trực quan

| Nhân vật | Dấu hiệu nhận diện | Hướng ngoại hình Epic | Điểm mạnh | Điểm yếu |
|---|---|---|---|---|
| Kiệt Bác Sĩ | Áo blouse, ống nghe, cà vạt xanh, khẩu trang; đầu vẽ tay với tóc vàng và mắt đỏ | Y sư công nghệ trắng–cyan, túi cứu thương và máy quét sinh lực | Heal đội, Resistance | Damage thấp, sợ Silence |
| Trường Kiệt Chu Mỏ | Tóc đen dày, mắt nhắm, môi chu rõ, áo đen | Sát thủ âm ba/sonic, áo khoác đen–hồng tím, đoản khí cộng hưởng | Burst đơn, Panic | Mỏng, phụ thuộc Accuracy |
| Tiến Dũng Tổng Đài | Mặt góc cạnh, kính chữ nhật, tóc vuốt, áo xanh navy | Pháp sư viễn thông với bảng gọi hologram và headset | AoE, action bar | Mỏng, damage đơn vừa |
| Quốc Nhân Giả Diện | Bộ đồ xanh–trắng loang, khăn mặt nạ răng, kính, bốt trắng | Sát thủ graffiti đêm, song đao ngắn và khói lam | Hàng sau, Bleed | Kém trước cleanse/DEF |
| Cậu Vàng Mặt Lạnh | Chó Shiba mập ngồi, mắt lim dim, vẻ bình thản | Linh thú hộ vệ với giáp vải đỏ–nâu và chuông ngọc | Khiên, giảm damage | Chậm, damage thấp |

## Khung cân bằng Epic

| Nhân vật | Basic | Energy | Utility | Điểm yếu giữ lại | Đánh giá |
|---|---:|---:|---|---|---|
| Kiệt Bác Sĩ | Heal 14% Max HP caster | Heal đội 20% Max HP caster | Resistance +15%, 2 lượt | Không gây damage | Epic support chuyên biệt |
| Trường Kiệt Chu Mỏ | 115% ATK | 205% ATK đơn | Panic 45%, 1 lượt | Không AoE/phòng thủ | Epic assassin hợp lý |
| Tiến Dũng Tổng Đài | 105% Magic | 90% Magic toàn địch | 40% lùi action bar 15 | HP/DEF thấp | Epic control mage |
| Quốc Nhân Giả Diện | 120% ATK | 125% ATK hàng sau | Bleed 35%; tự Crit +15% | Không sustain | Epic backline hunter |
| Cậu Vàng Mặt Lạnh | 100% ATK | Không damage | Khiên 12% Max HP + giảm damage 12% | Chậm, ít áp lực | Epic protector |

Các hero Epic có utility rõ hơn Rare nhưng vẫn chỉ có một basic và một energy skill. Không hero nào có hồi sinh, bất tử, resource riêng, kích nổ nhiều tầng hoặc phản ứng toàn trận kiểu Mythic.

## Asset raster đề xuất

- `skills/kiet-bac-si/vital-scan-orb.png`
- `skills/truong-kiet-chu-mo/resonance-kiss-seal.png`
- `skills/tien-dung-tong-dai/holo-call-panel.png`
- `skills/quoc-nhan-gia-dien/phantom-mask.png`
- `skills/cau-vang-mat-lanh/jade-guard-bell.png`

Các vòng quét, sóng âm, tia dữ liệu, vệt chém, khói, hit flash và barrier đơn giản nên dựng bằng CSS/SVG. Raster phải có alpha thật, không chữ/logo/UI/background.

## Khả năng tái sử dụng hệ thống

- Effect hiện có: `DAMAGE`, `HEAL`, `SHIELD`, `STAT_BUFF`, `BLEED`, `PANIC`, `SILENCE`, `DAMAGE_REDUCTION`, `ACTION_BAR_CHANGE`.
- Selector hiện có: `SELF`, `ALLY_ALL`, `LOWEST_HP_PERCENT`, `ENEMY_SINGLE`, `ENEMY_ALL`, `ENEMY_BACK_ROW`.
- Cả năm bộ kỹ năng có thể biểu diễn bằng effect và selector hiện tại; không đề xuất handler hoặc selector mới.
- Tất cả coefficient, chance, duration, max stack, target count và delta phải lấy từ `HrkSkillEffect*`; VFX resolve bằng `skillId`/`heroCode` qua `battle-skill-vfx-host`.

