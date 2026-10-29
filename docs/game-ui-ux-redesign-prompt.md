# Prompt thiết kế lại toàn bộ UX/UI game HRK

Sao chép nội dung bên dưới để giao việc triển khai. Chủ đề đề xuất là **Urban Hero HQ — Căn cứ biệt đội anh hùng**. Đây là yêu cầu thiết kế lại sản phẩm đang chạy, bao gồm bố cục, hành trình người chơi, hình ảnh và mã giao diện.

---

Bạn đóng vai Product Designer, Game UI Artist và Senior Angular Engineer. Hãy thiết kế lại và triển khai toàn bộ UX/UI game trong repository HRK, tập trung tại `HrkUi/src/app/dcs-game`, để tạo một trải nghiệm game hiện đại, đẹp, dễ hiểu và thống nhất.

## 1. Kết quả cần đạt

Thực hiện một đợt redesign thực sự: thiết kế lại home, cách tổ chức và điều hướng tính năng, bố cục các màn hình, cách người chơi lựa chọn và thực hiện hành động, hệ thống hình ảnh và phản hồi sau thao tác. Được sửa HTML, TypeScript, SCSS, cấu trúc component và các phần điều hướng cần thiết để hiện thực hóa thiết kế mới.

Home phải có cảnh nền được đầu tư, điểm nhấn nhân vật và hành động tiếp tục chơi rõ ràng. Các tính năng phải có thiết kế phù hợp với nhiệm vụ của chúng, cùng thuộc một thế giới hình ảnh. Chỉ thay token, màu, font, border hoặc giữ nguyên tất cả layout rồi phủ CSS mới chưa đáp ứng yêu cầu này.

## 2. Khảo sát source và xác lập thiết kế

Đọc các hướng dẫn repository, routes, template, component, service, model, localization và asset catalog hiện có trước khi chỉnh sửa. Những nơi cần khảo sát gồm:

- `HrkUi/src/app/dcs-game/src/app/features/`.
- `HrkUi/src/app/dcs-game/src/app/core/` và các cấu hình modal.
- `HrkUi/src/libs/shared/ui/`, `HrkUi/src/libs/core/localization/`.
- `HrkUi/src/libs/styles/`, stylesheet entry của shell.
- `HrkUi/src/assets/catalog/asset-paths.json`, `HrkUi/scripts/prepare-assets.mjs` và `check-assets.mjs`.
- `docs/game-ui-design-system.md`.

Liệt kê đầy đủ các màn hình và modal thực tế, chức năng đã hoạt động, chức năng chưa có backend và các trạng thái dữ liệu. Đối chiếu với giao diện đang chạy khi có thể. Chỉ ra vấn đề cụ thể về bố cục, mật độ thông tin, số bước thao tác, điều hướng, điểm nhấn và hình ảnh.

Sau khảo sát, chốt sitemap, quy tắc điều hướng và một concept home đủ chi tiết cho desktop lẫn mobile. Trình bày ngắn hướng thiết kế rồi tiếp tục triển khai theo hướng đã chọn. Giữ các tài liệu thiết kế nhất quán với kết quả cuối cùng.

## 3. Chủ đề và phong cách hình ảnh

Chọn hướng **Urban Hero HQ — Căn cứ biệt đội anh hùng**: game sưu tầm và xây dựng đội hình, đặt trong một đô thị hiện đại có cá tính, ánh sáng điện ảnh và cảm giác đang điều hành một biệt đội.

Tạo chiều sâu bằng kiến trúc, ánh sáng, vật liệu và nhân vật. Nền graphite hoặc navy, điểm nhấn lime và cyan có tiết chế; dùng ánh sáng ấm để cân bằng khung cảnh. Màu phẩm chất và trạng thái phải dễ phân biệt. Font hỗ trợ tiếng Việt đầy đủ, chỉ số dễ đọc, kích thước chữ và khoảng cách có phân cấp.

Tận dụng bản sắc và ảnh các võ tướng hiện có, kể cả nét vui nhộn của dự án. Chọn cách trình bày nhân vật phù hợp với chất lượng ảnh thực tế. Không tự thay danh tính nhân vật bằng các nhân vật ngẫu nhiên. Thiết kế phải có cảm giác của một game với sân khấu nhân vật và thế giới riêng; tránh biến toàn bộ sản phẩm thành các dashboard card giống nhau.

## 4. Thiết kế lại home và điều hướng

Xây dựng home thành một sảnh/căn cứ có cảnh nền mới và bố cục mới. Xác định rõ vùng trưng bày đội hình hoặc nhân vật, thông tin người chơi, tài nguyên, tiến độ chiến dịch và lời mời tiếp tục chơi.

CTA chính phải nổi bật và dẫn tới hoạt động đang có dữ liệu thật. Các hành động phụ có thứ bậc. Gom tính năng theo nhiệm vụ của người chơi, làm rõ cách vào võ tướng, đội hình, hành trang, lò rèn, chiêu mộ, chiến dịch, leo tháp và điểm danh. Có thể thay dock hiện tại bằng cấu trúc phù hợp hơn; điều hướng desktop và mobile cần được thiết kế riêng trong cùng một hệ thống.

Giữ khả năng mở cài đặt trong khu vực hồ sơ người chơi. Đổi ngôn ngữ tiếp tục nằm trong cài đặt. Các tính năng chưa triển khai phải có trạng thái rõ ràng. Chỉ hiển thị thông báo chấm đỏ, tiến độ, nhiệm vụ hoặc phần thưởng khi có căn cứ từ dữ liệu thực tế.

Home phải còn đẹp khi chưa có đội hình, ảnh tải lỗi, tên người chơi dài, tài nguyên có nhiều chữ số hoặc dữ liệu đang tải. Không để HUD, nội dung chính và điều hướng che nhau trên màn hình thấp.

## 5. Thiết kế từng nhóm tính năng

### Võ tướng và đội hình

Thiết kế lại thư viện tướng và trang chi tiết: tìm kiếm, lọc, sắp xếp, chọn tướng, xem kỹ năng, trang bị và nâng cấp. Trình bày rõ ảnh, vai trò, phẩm chất, cấp, sao và chiến lực. Tác vụ chính cần dễ thấy; chi tiết ít dùng có thể nằm trong tab hoặc khu vực mở rộng.

Thiết kế luồng nâng cấp thể hiện chi phí, nguyên liệu, kết quả trước/sau và lý do bị khóa. Duy trì tướng đang chọn, bộ lọc và vị trí danh sách sau thao tác khi phù hợp. Đội hình phải thể hiện vị trí, vai trò, trạng thái chọn và nút lưu; trên mobile phải có cách chọn/thay tướng bằng chạm nếu desktop có kéo thả.

### Hành trang và lò rèn

Thiết kế lại danh mục, grid vật phẩm, thông tin chi tiết và các thao tác đang được backend hỗ trợ. Phân biệt rõ trang bị, nguyên liệu và vật phẩm khác. Việc so sánh, trang bị, nâng cấp hoặc bán phải cho người chơi hiểu đối tượng đang tác động và kết quả dự kiến.

Lò rèn cần có cảm giác của một không gian chế tác: điểm đặt trang bị, nguyên liệu, chi phí, mức nâng cấp và kết quả. Tránh mở liên tiếp nhiều modal cho một tác vụ thông thường. Giữ xác nhận phù hợp với thao tác có mất tài nguyên hoặc bán vật phẩm.

### Chiêu mộ

Thiết kế lại trang lựa chọn loại chiêu mộ, phần minh họa, tài nguyên sở hữu, chi phí x1/x10 và cách xem tỷ lệ. Có art riêng phù hợp với chủ đề, hiệu ứng mở thưởng và màn kết quả thống nhất.

Hiệu ứng chiêu mộ đơn phải hiển thị đúng toàn màn hình, vừa viewport và nhìn thấy nút bỏ qua ngay. Trạng thái gửi yêu cầu, kết quả chưa xác định, thử kiểm tra lại, tướng mới và tướng trùng phải rõ ràng. Giữ kết quả do server quyết định và cơ chế khôi phục/idempotency hiện có; bỏ qua animation chỉ thay cách trình bày, không gửi thêm lượt chiêu mộ.

Kết quả x10 phải dễ quét phẩm chất và phân biệt phần thưởng, với hành động tiếp theo dễ thấy. Cân nhắc giới hạn animation dài khi người chơi thao tác lặp lại.

### Chiến dịch, bản đồ và leo tháp

Thiết kế lại lựa chọn hoạt động, chương/bản đồ, các nút ải và bảng thông tin trước trận. Giữ quan hệ không gian giữa bản đồ và vị trí ải; phân biệt ải hiện tại, đã hoàn thành, bị khóa và boss bằng hình dáng/nhãn cùng màu sắc.

Luồng chọn ải → xem đối thủ/phần thưởng → chuẩn bị đội hình → bắt đầu phải mạch lạc. Leo tháp có đặc trưng tiến lên theo tầng, thể hiện tầng hiện tại, tiến độ, phần thưởng và các thao tác đang được hỗ trợ. Art mới cần phù hợp với nội dung map và tầng, không làm sai topology hoặc điều kiện mở khóa từ dữ liệu.

### Chiến đấu và kết quả trận

Thiết kế lại HUD, nền chiến đấu, vị trí đơn vị, thanh HP/mana, trạng thái, điều khiển và combat log. Đảm bảo người chơi nhận ra ai đang hành động, mục tiêu và thay đổi quan trọng. Giữ nguyên quy tắc, timeline và dữ liệu trận đấu.

Hiệu ứng cần giúp đọc diễn biến, không che đơn vị hoặc nút điều khiển. Kết quả thắng/thua phải làm rõ phần thưởng và các hành động tiếp tục, đánh lại hoặc quay về theo chức năng hiện có.

### Điểm danh và hoạt động hằng ngày

Thiết kế lịch thưởng theo ngày trong tháng hiện có. Nhận biết ngay hôm nay, đã nhận, bỏ lỡ và ngày tương lai. Làm nổi bật phần thưởng hôm nay, nút nhận và thời gian reset theo server. Thay phần quà dạng danh sách thô bằng cách trình bày vật phẩm rõ ràng, sinh động và vẫn dễ đọc trên mobile.

### Đăng nhập, đăng ký, hồ sơ, cài đặt và thư viện

Thiết kế cùng thế giới hình ảnh với home. Form dễ đọc, báo lỗi gần trường liên quan, trạng thái loading rõ. Cài đặt tổ chức hợp lý các lựa chọn đã có, bao gồm ngôn ngữ. Thư viện và chọn avatar phải được rà soát và redesign cùng đợt; không bỏ sót vì là modal hoặc chức năng phụ.

## 6. Asset hình ảnh và cách tạo ảnh

Lập danh sách asset cần tạo/thay, nêu màn hình sử dụng, vai trò, tỷ lệ, kích thước, vùng dành cho chữ/nút và cách crop mobile. Rà soát ảnh sẵn có trước để giữ những tài nguyên còn phù hợp.

Ưu tiên tạo cảnh home mới, art chiêu mộ và art cho chiến dịch/không gian tính năng khi asset hiện có không đáp ứng concept. Auth có thể dùng biến thể cùng thế giới với home. Icon UI nhỏ nên dùng hệ icon/vector thống nhất; dùng công cụ tạo ảnh cho cảnh nền, minh họa, texture hoặc cutout cần raster.

Khi có công cụ tạo ảnh, chủ động tạo asset cần thiết rồi tích hợp thật vào giao diện. Một số định hướng prompt ảnh:

- **Home:** cinematic illustrated rooftop headquarters overlooking a contemporary city at blue hour; graphite architecture, subtle cyan technology, warm amber practical lights, open central platform for the game's existing hero characters, quiet upper corners for HUD, clear lower area for navigation, layered depth, polished mobile RPG environment art; no text, no interface, no embedded characters.
- **Chiêu mộ:** modern hero recruitment chamber inside the same headquarters, central illuminated arrival platform, restrained cyan and lime energy, architectural side framing, cinematic depth, clean central focal point for separately rendered hero reveals; no text, no interface, no characters.
- **Chiến dịch:** illustrated urban district viewed from a readable isometric angle, distinct traversable districts and landmarks, clear areas for separately rendered stage nodes, matching headquarters architecture and lighting; no text, no interface, no painted stage numbers or buttons.

Điều chỉnh prompt theo layout đã chốt và asset tham chiếu thực tế. Khi cần cutout, xuất nền trong suốt. Chữ, số, CTA, trạng thái và thông tin game phải render bằng HTML để hỗ trợ tương tác và dịch ngôn ngữ.

Nguồn ảnh desktop có thể hướng tới 2560×1440; mobile cần crop riêng hoặc ảnh dọc khoảng 1080×1920 nếu bố cục yêu cầu. Chọn kích thước xuất theo công cụ hỗ trợ, sau đó tạo bản phân phối tối ưu WebP/AVIF có fallback khi phù hợp. Khai báo kích thước/tỷ lệ để hạn chế layout shift.

Lưu nguồn ở asset canonical của dự án, cập nhật catalog/alias khi cần. Không chỉnh thủ công `.generated/legacy-assets`. Ghi lại prompt, nguồn và quy tắc dùng trong tài liệu asset. Nếu công cụ tạo ảnh không khả dụng, nêu rõ asset còn thiếu và fallback đang dùng; không trình bày placeholder như art đã hoàn thiện.

## 7. Quy tắc UX chung

Chuẩn hóa luồng quay lại, đóng màn hình, xác nhận, lưu và nhận thưởng. Phân biệt khi dùng trang độc lập, màn toàn màn hình, modal nhỏ và panel; giảm modal lồng nhau khi đang làm cùng một tác vụ.

Mỗi màn hình phải có trạng thái tải, rỗng, lỗi/thử lại, thành công, bị khóa và đang xử lý phù hợp. Hiển thị lý do thiếu tài nguyên hoặc không đủ điều kiện. Ngăn thao tác lặp trong lúc gửi request. Giữ trạng thái có ích như bộ lọc, lựa chọn và vị trí cuộn khi người chơi quay lại.

Desktop và mobile phải được bố trí theo cách sử dụng thực tế, không chỉ co nhỏ cùng một màn hình. Điều hướng, CTA và nội dung quan trọng phải tiếp cận được mà không bị header/footer che. Cho phép cuộn có chủ đích ở nội dung dài, tránh nhiều vùng cuộn lồng nhau và tràn ngang toàn trang.

## 8. Hệ thống component và cấu trúc code

Sau khi xác lập thiết kế, xây dựng hoặc cập nhật các thành phần dùng chung cần thiết: game shell, header, navigation, resource counter, hero/item card, detail panel, filter bar, action bar, reward presentation và các feedback state. Chia component theo trách nhiệm và khả năng tái sử dụng thực tế.

Có thể viết lại template và logic trạng thái giao diện để hỗ trợ UX mới. Dùng API/service/model đang có; không tự thêm endpoint hoặc dựng dữ liệu game giả để làm ảnh chụp đẹp. Nếu một ý tưởng đòi hỏi backend chưa có, ghi nhận như phần mở rộng và chọn cách trình bày hoạt động được với dữ liệu hiện tại.

Giữ phạm vi style của game, kiểm tra tác động của shared modal/component tới HRM và shell. Cập nhật token theo hệ thống mới, xử lý xung đột CSS tại nguồn thay vì chồng thêm override và `!important`. Dọn layout/style cũ đã được thay thế.

## 9. Tính đúng đắn và localization

Giữ nguyên auth, phân quyền, route đang dùng hoặc bổ sung tương thích khi đổi điều hướng, API contract, điều kiện nghiệp vụ và nguồn sự thật từ server. Không thay đổi thuật toán chiến đấu, chi phí/phần thưởng, tỷ lệ chiêu mộ hoặc quy tắc điểm danh trong đợt redesign.

Mọi chuỗi hiển thị mới đi qua localization hiện có, dùng placeholder cho argument thay vì ghép chuỗi động rồi hash. Bổ sung catalog và tạo lại seed/publish script bằng workflow hiện có nếu thêm nội dung. Kiểm tra cả tiếng Việt và tiếng Anh, kể cả nhãn dài, tên dài và số lớn.

## 10. Accessibility và hiệu năng

Dùng button/link đúng ngữ nghĩa, focus có thể nhìn thấy, thao tác bàn phím, nhãn truy cập và quản lý focus khi mở/đóng modal. Nội dung thiết yếu không phụ thuộc vào hover hoặc chỉ dùng màu. Touch target đủ lớn; hỗ trợ `prefers-reduced-motion`.

Ảnh ưu tiên theo màn hình: tải sớm phần home cần hiển thị ngay, lazy-load ảnh ngoài vùng nhìn và asset tính năng chưa mở. Dùng ảnh responsive, dành sẵn không gian ảnh, giới hạn lớp blur và animation liên tục. Giữ lazy-loading route và tránh đưa toàn bộ art vào bundle JavaScript ban đầu.

## 11. Triển khai và kiểm chứng

Lưu ảnh/bằng chứng giao diện cũ khi có thể. Bắt đầu bằng một lát cắt hoàn chỉnh gồm home, điều hướng, một tính năng quản lý và chiêu mộ, với asset thật, để kiểm chứng thiết kế xuyên suốt. Sau đó hoàn thành tất cả màn hình và modal đã kiểm kê; lát cắt đầu tiên chỉ là một mốc triển khai.

Build frontend, kiểm tra asset và chạy các test hiện có phù hợp với thay đổi. Bổ sung test khi thay đổi logic tương tác hoặc cần ngăn một regression hành vi thực tế; tránh test chỉ kiểm tra tên class CSS.

Kiểm tra trực quan UI thật ở desktop 1920×1080, laptop 1366×768, tablet khoảng 768×1024, điện thoại 390×844, màn hình hẹp 360px và chế độ ngang thấp. Chạy các hành trình chính: home → tiếp tục chiến dịch; chọn/nâng cấp tướng; đổi/lưu đội hình; xem/trang bị/bán vật phẩm; dùng lò rèn; chiêu mộ x1/x10 và bỏ qua hiệu ứng; nhận điểm danh; vào trận và xem kết quả; đổi ngôn ngữ trong cài đặt.

Thử dữ liệu rỗng, ảnh lỗi, API chậm/lỗi, thiếu tài nguyên, tên dài, số lớn, cả hai ngôn ngữ và reduced motion. Có ảnh chụp các màn quan trọng trước/sau khi công cụ cho phép. Build thành công chỉ xác nhận khả năng biên dịch; việc đánh giá bố cục phải dựa vào giao diện đã render. Ghi rõ phần chưa thể kiểm chứng và nguyên nhân nếu môi trường chặn.

## 12. Đầu ra và tiêu chí hoàn thành

Bàn giao code triển khai, asset mới đã tích hợp, danh sách màn hình/luồng đã redesign, tài liệu design system và asset được cập nhật, kết quả kiểm chứng cùng giới hạn còn lại. Có thể tạo `docs/game-ui-ux-redesign.md` để ghi quyết định thiết kế và tình trạng hoàn thành từng màn.

Chỉ kết thúc khi home có thay đổi rõ về cảnh nền và bố cục; các tính năng có thiết kế mới phù hợp nhiệm vụ; điều hướng và hành trình chơi đã được cải thiện; các phần thống nhất về hình ảnh; chức năng hiện có hoạt động; nội dung chính và CTA tiếp cận được trên desktop/mobile; asset, bản dịch và trạng thái giao diện đầy đủ trong phạm vi đã triển khai.

Hãy thực hiện việc khảo sát, thiết kế, tạo asset cần thiết, implement và kiểm chứng theo yêu cầu này. Tiến hành các quyết định giao diện thông thường trong phạm vi được giao và báo cáo kết quả cụ thể.
