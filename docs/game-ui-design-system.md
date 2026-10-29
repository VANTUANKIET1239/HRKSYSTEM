# Hệ Thống Giao Diện Game HRK (DCS Game Design System)

**Chủ đề (Theme):** `ĐÔ THỊ HIỆN ĐẠI — BIỆT ĐỘI ANH HÙNG` (Modern Urban — Hero Squad)

Tài liệu này mô tả hệ thống Design Token, quy tắc đóng gói phạm vi (scoping) và cách sử dụng các thành phần giao diện dùng chung cho phân hệ game (`dcs-game`) trong `HrkUi`.

---

## 1. Kiến Trúc & Phạm Vi Áp Dụng (Scoping)

Ứng dụng `HrkUi` chạy mô hình Shell đa phân hệ (`hrm` và `dcs-game`) sử dụng entry point stylesheet tại `HrkUi/src/app/shell/src/styles.scss`.

Để đảm bảo giao diện mới áp dụng đầy đủ cho mọi màn hình, dialog/modal của game nhưng **không làm ảnh hưởng đến ứng dụng HRM**:

- Toàn bộ token của game được đặt tiền tố `--dcs-*` trong `HrkUi/src/libs/styles/_game-tokens.scss`.
- Các quy tắc nền, font chữ và tiện ích dùng chung của game được đóng gói trong class `.dcs-game-theme` tại `HrkUi/src/libs/styles/_game-design-system.scss`.
- Các host gốc của game tự động gắn `.dcs-game-theme`:
  - `HrkUi/src/app/dcs-game/src/app/app.ts`
  - `HrkUi/src/app/dcs-game/src/app/features/auth/pages/game-auth/game-auth.component.ts`
  - `HrkUi/src/libs/shared/ui/modal/hrk-modal.component.html` (`<dialog class="dcs-game-theme">`)

---

## 2. Bảng Design Token Chính (`--dcs-*`)

### 2.1. Màu nền & Bề mặt (Surfaces)
| Token | Giá trị | Mục đích sử dụng |
| :--- | :--- | :--- |
| `--dcs-bg-deep` | `#0b0f14` | Nền sâu cho vùng cuộn, rãnh thanh progress |
| `--dcs-bg-base` | `#10151b` | Màu nền chính của màn hình game |
| `--dcs-surface` | `#19212a` | Bề mặt panel, modal, vùng nội dung chính |
| `--dcs-surface-raised` | `#222d38` | Thẻ bài (card), ô vật phẩm, nút phụ, input |
| `--dcs-surface-hover` | `#283542` | Trạng thái di chuột (hover) trên bề mặt nổi |
| `--dcs-bg-overlay` | `rgba(9, 13, 18, 0.78)` | Lớp phủ nền sau modal/dialog |

### 2.2. Màu chữ (Typography Colors)
| Token | Giá trị | Mục đích sử dụng |
| :--- | :--- | :--- |
| `--dcs-text-primary` | `#f0f4f7` | Chữ chính, tiêu đề, tên tướng, tên vật phẩm |
| `--dcs-text-secondary` | `#a7b6c3` | Nhãn phụ, mô tả kỹ năng, thông tin phụ |
| `--dcs-text-muted` | `#6e8091` | Gợi ý (placeholder), trạng thái khóa/vô hiệu |

### 2.3. Màu nhấn & Trạng thái (Accents & Semantic States)
| Token | Giá trị | Mục đích sử dụng |
| :--- | :--- | :--- |
| `--dcs-accent` | `#bcf27b` | Màu nhấn chính (Xanh Lime): CTA chính, viền đang chọn, thông báo quan trọng |
| `--dcs-accent-hover` | `#ccf796` | Trạng thái hover của CTA chính |
| `--dcs-accent-contrast` | `#0b1305` | Màu chữ tối tương phản cao trên nền `--dcs-accent` |
| `--dcs-secondary` | `#38bdf8` | Màu nhấn phụ (Cyan): năng lượng, kim cương, thông tin phụ trợ |
| `--dcs-success` | `#4ade80` | Tăng chỉ số, hoàn thành, đã nhận thưởng |
| `--dcs-warning` | `#fbbf24` | Vàng, cảnh báo, sao tướng |
| `--dcs-danger` | `#f87171` | Sát thương, thiếu nguyên liệu, lỗi, hành động xóa/đăng xuất |

### 2.4. Độ hiếm Tướng & Vật phẩm (Rarity Tokens)
- `--dcs-rarity-common`: `#94a3b8` (Thường)
- `--dcs-rarity-uncommon`: `#4ade80` (Khá)
- `--dcs-rarity-rare`: `#38bdf8` (Hiếm)
- `--dcs-rarity-epic`: `#c084fc` (Sử Thi)
- `--dcs-rarity-legendary`: `#fbbf24` (Huyền Thoại)
- `--dcs-rarity-mythic`: `#f43f5e` (Thần Thoại)

### 2.5. Typography
- `--dcs-font-sans` / `--dcs-font-display`: `'Be Vietnam Pro', 'Segoe UI', Roboto, sans-serif` — Hỗ trợ đầy đủ dấu tiếng Việt, hiện đại và rõ nét ở mọi kích thước.
- `--dcs-font-mono`: `'JetBrains Mono', Consolas, monospace` — Dùng cho các con số cấp độ (`Lv`), chiến lực (`CP`), chỉ số (`HP/ATK/DEF/SPD`), số lượng tài nguyên và bộ đếm lượt.

---

## 3. Hướng Dẫn Sử Dụng Component Chung

1. **Modal & Confirmation Dialog (`<hrk-modal>`, `<hrk-confirmation-modal>`)**:
   - Sử dụng `<hrk-modal size="SM|MD|LG|XL" [title]="...">` cho các hộp thoại chuẩn.
   - Sử dụng `HrkConfirmationService` hoặc `<hrk-confirmation-modal>` cho hộp thoại xác nhận (ví dụ: xác nhận đăng xuất, đồng bộ template).
2. **Toast Notification (`GameToastService` / `<app-game-toast>`)**:
   - Gọi `toast.success(...)`, `toast.warning(...)`, `toast.error(...)` hoặc `toast.info(...)`.
3. **Hình ảnh & Fallback (`HrkImageDirective`, `<app-item-image>`)**:
   - Luôn gắn `imageKind="hero"` cho ảnh tướng/avatar và sử dụng `<app-item-image>` cho vật phẩm để tự động xử lý fallback khi thiếu asset.
4. **Đa ngôn ngữ (`hrkTranslate`)**:
   - Mọi chuỗi hiển thị trong template phải đi qua pipe `| hrkTranslate`.
   - Khi thêm chuỗi mới, bổ sung vào `tools/localization/catalog.json` và chạy `node tools/localization/build-seed.mjs`.


---

## 4. Environment Backgrounds & UX/UI Architecture (`Urban Hero HQ`)

Hệ thống bối cảnh môi trường 16:9 (`1344×768`) trong `HrkUi/src/assets/images/dcs-game/backgrounds/`:

- `urban-hq-home.png` / `lobby-bg.png`: Sân thượng chỉ huy căn cứ biệt đội anh hùng lúc chập tối (Home, Auth, Check-in).
- `summon-chamber-bg.png`: Phòng cổng dịch chuyển chiêu mộ công nghệ cao (Summon).
- `campaign-district-bg.png`: Bản đồ chiến thuật toàn cảnh các quận đô thị hiện đại (Campaign & Map List).
- `tower-hq-bg.png`: Tháp chọc trời trung tâm xuyên mây (Tower Climb & Event Banner).

Xem chi tiết kiến trúc điều hướng, luồng hành trình người chơi và thiết kế từng màn hình tại [docs/game-ui-ux-redesign.md](./game-ui-ux-redesign.md).
