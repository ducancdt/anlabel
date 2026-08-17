# Thiết kế panel công nghiệp: Workspace và Properties

**Phiên bản áp dụng:** v0.201  
**Figma:** [ANLAbel UI exploration](https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5) — overview `ANLAbel — Frequency-first Panels v0.198` (`8:2`), compact state v0.199 (`13:2`), tabbed state `ANLAbel — Properties tabs v0.200` (`18:69`) và component xác minh Excel v0.201 (`22:82`).  
**Full main-shell recreation (NiceLabel map):** [ANLAbel — NiceLabel Shell Recreation](https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc) frame `ANLAbel — Full Shell v1` (`2:2`). Research: `docs/NICELABEL_DESIGNER_SHELL_RESEARCH.md`.

## 1. Mục tiêu

Hai panel phải giúp người vận hành trả lời nhanh bốn câu hỏi:

1. Tôi đang thêm hoặc chọn object nào?
2. Tem đang dùng dữ liệu nào và đang xem dòng nào?
3. Object được chọn nằm ở đâu, có nội dung gì và trông như thế nào?
4. Trước khi in còn thiếu cấu hình hay có lỗi binding nào không?

Panel không phải nơi phô bày toàn bộ khả năng của ứng dụng. Mục thường dùng phải nhìn thấy ngay; mục kỹ thuật hoặc ít dùng chỉ mở khi người dùng chủ động cần.

## 2. Nguyên tắc thông tin

### Frequency first

- Luôn hiện: loại ngữ cảnh hiện tại, hành động chính, vị trí/kích thước, content, trạng thái lỗi.
- Hiện theo ngữ cảnh: Text Style chỉ cho Text/TextBox, Shape Style chỉ cho shape, Barcode chỉ cho barcode, bảng row chỉ khi có dữ liệu.
- Mặc định thu gọn: tracking key, copies per record, transforms, shared sources, arrange/layer nâng cao và formula builder.

### Progressive disclosure

- Tab chỉ được dùng khi nó chuyển giữa hai nhiệm vụ thật. `Layers` và `Data` là tab thật; không tạo tab trang trí hoặc tab chỉ có một mục.
- `Expander` dùng cho cấu hình phụ trong cùng nhiệm vụ. Header phải nói rõ bên trong có gì, không dùng tên chung chung như `More`.
- Trạng thái rỗng là card nhỏ có hướng dẫn và một hành động tiếp theo; không chiếm toàn bộ chiều cao panel.

### Một chức năng, một vị trí chính

- Zoom belongs on the status bar only. It is not repeated in the ribbon or Properties.
- Import dữ liệu có thể được gọi ở ribbon và empty-state Data vì đây là cùng hành động chính trong hai ngữ cảnh; không tạo luồng import thứ hai.
- Quản lý nguồn dùng chung nằm sau `Data settings`; thao tác chi tiết vẫn mở cửa sổ Data Source Manager hiện có.

### Icon phải mang nghĩa

- Icon đứng trước danh từ hoặc hành động: Layers, Data, Document, Import, Setup, Collapse.
- Không dùng icon chỉ để lấp khoảng trống. Icon không thay nhãn ở hành động quan trọng.
- Icon và chữ phải đủ tương phản ở trạng thái thường, hover, active và disabled. Trên nền xanh, không dùng bitmap tối không đổi màu.

## 3. Kiến trúc panel Workspace

| Mức ưu tiên | Nhóm | Trạng thái |
| --- | --- | --- |
| P0 | Tab `Layers` / `Data` | Luôn hiện, chuyển chức năng thật |
| P0 | No data linked + `Import Excel / CSV` | Hiện khi chưa có nguồn |
| P0 | Workbook/sheet, preview row, bảng Excel rows | Hiện khi có dữ liệu |
| P0 | Binding checks | Luôn hiện; xanh khi sạch, đỏ khi có lỗi |
| P1 | Template và object list | Tab `Layers` |
| P2 | Tracking, Copies, Transforms, Shared sources | `Data settings`, mặc định đóng |

Chiều rộng thiết kế WPF là 268 DIP. Data được chọn mặc định để giữ luồng sử dụng hiện có; người dùng có thể chuyển sang Layers mà không phải cuộn qua dữ liệu.

## 4. Kiến trúc panel Properties

### Không có object được chọn

- Empty-state compact ở đầu panel: icon 32 DIP, một tiêu đề, một dòng hướng dẫn.
- Card `Document` luôn hữu ích: kích thước tem, máy in, nguồn dữ liệu và nút `Label & printer setup`.
- Không hiển thị zoom trong panel.

### Có object được chọn

Thứ tự đọc và chỉnh:

1. Summary object và thanh tab luôn nằm trên cùng.
2. Tab `Label` là mặc định: Content → Behavior/Style theo loại object. Với Text/TextBox, nội dung, fit/overflow và typography phải xuất hiện trước các thông số hình học.
3. Tab `Layout`: X/Y/Width/Height để nhập số chính xác; canvas drag/resize vẫn là đường thao tác chính.
4. Tab `Advanced`: Rotate, Align, Distribute, Layer, Visible và Locked. Đây là tab xa nhất vì không thuộc luồng soạn nội dung tem thường xuyên.

Tab phải chuyển giữa nội dung thật, không chỉ là header trang trí. Khi đổi tab, binding và giá trị control phải được giữ nguyên.

Canvas drag/resize là đường thao tác chính cho hình học. X/Y/Width/Height vẫn chỉnh chính xác được khi mở utility row, nhưng không chiếm bốn ô nhập ở trạng thái mặc định.

Contract Text/TextBox là bất biến: việc sắp xếp panel không được thay đổi sizing, wrap, clip, padding, resize lifecycle hoặc output in. Text vẫn free-flow; TextBox vẫn là fixed frame và không cho glyph in ra ngoài object.

## 5. Kích thước và mật độ

- Workspace: 268 DIP; Properties: 280 DIP.
- Header panel: 42 DIP trong WPF; card padding 8–10 DIP; khoảng cách card 8–10 DIP.
- Control nhập liệu trong Properties giữ min-height 28 DIP.
- Empty-state không cao quá khoảng 90 DIP trước card Document.
- Một panel chỉ sở hữu một vertical scroll. Không đặt DataGrid vào một chuỗi nested scroll không có chiều cao rõ ràng.

## 6. Trạng thái và màu

- Surface: `#FFFFFF`; surface phụ: `#F8FAFC`; canvas: `#EEF2F6`.
- Primary: `#1464D2`; primary dark: `#0F4EA8`; primary soft: `#EAF3FF`.
- Border: `#D9E2EC`; text chính: `#1E293B`; text phụ: `#64748B`.
- Binding sạch: nền `#F0FDF4`, border `#86EFAC`.
- Binding lỗi: nền `#FEF2F2`, border `#FCA5A5`.

Màu chỉ hỗ trợ hierarchy và trạng thái; không dùng nhiều màu để phân biệt các card bình thường.

## 7. Quy tắc kiểm tra

- Kiểm tra ở minimum window 1024 × 600 và display scale 100%, 125%, 150%.
- Không clip chân chữ, icon hoặc header; không có horizontal scrollbar ở ribbon/panel mặc định.
- Tab phải chuyển được nội dung thật; Expander phải giữ nguyên binding khi đóng/mở.
- `Layout` và `Advanced` không lồng thêm một disclosure đóng mặc định: chọn tab là thấy ngay control của nhiệm vụ đó, tránh hai lần nhấp.
- Empty-state phải dẫn tới hành động hợp lệ, không có nút giả.
- Khi chọn Text và TextBox, kiểm tra Properties vẫn giữ nguyên contract đã khóa trong `AGENTS.md`.
- Runtime screenshot là bằng chứng chính cho layout; build/test chỉ chứng minh XAML và logic không hỏng.

## 8. Xác minh nguồn Excel trong Properties

- `Content` phải cho biết nguồn Excel có đáng tin để dùng cho dữ liệu in hay không, không chỉ lặp lại tên file.
- Năm trạng thái bắt buộc: chưa liên kết, đang kiểm tra, đã xác minh, dữ liệu cũ và lỗi. Chỉ `Verified` được dùng sau khi workbook mở thành công và sheet/header hợp lệ.
- Evidence của trạng thái xanh gồm workbook/sheet, số cột, số dòng và giờ kiểm tra. File thay đổi sau đó phải hạ trạng thái sang `Stale`; không được tiếp tục hiển thị success cũ.
- Hành động đổi theo trạng thái: `Link Excel...`, `Checking...`, `Recheck Excel link`, `Update & verify`. Lỗi phải hiển thị nguyên nhân và hướng dẫn relink khi cần.
- Figma component chuẩn là `Excel Link Verification` (`22:82`); WPF chuyển thiết kế đó sang control/style hiện có, không đưa React/Tailwind vào dự án.
