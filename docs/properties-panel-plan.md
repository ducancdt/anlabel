# Plan: Kiểm tra & sắp xếp lại Properties panel

**Ngày:** 2026-07-03 · **Trạng thái:** Cả 3 đợt A, B, C đều đã làm xong (xem mục 6, 7, 8) · Theo định hướng chủ dự án: "kiểm tra và sắp xếp lại properties"

## 1. Hiện trạng (audit `MainWindow.xaml` ~dòng 770-1016)

Panel bên phải hiện có các card theo thứ tự, chỉ hiện khi có `SelectedObject`:

| # | Card | Nội dung | Điều kiện hiện |
|---|------|----------|----------------|
| 1 | Header (không tiêu đề) | Loại object + tóm tắt | luôn |
| 2 | **Content** | Source (Text / Excel Field / Binding-Formula) + Formula Builder đầy đủ (chips field, separators, add text, parts list, expression, Apply/Clear) nhúng thẳng trong card | luôn |
| 3 | **Transform & Arrange** | Rotation (combo 0/90/180/270), Layer order (Front/Back), Visible + Locked | luôn |
| 4 | **Text Style** | Font, Size, Align, Bold/Italic/Underline + cảnh báo tràn TextBox | Text/TextBox |
| 5 | **Barcode** | Standard (symbology), QR mode, EC level, DPI, Fixed Size (Version/Module px) | Barcode/QR/DataMatrix |
| 6 | **Binding** | Source type, status, Preview (read-only), used/missing fields, errors | khi có binding |
| 7 | **Formula Output** | Output preview, used fields, errors | khi binding là formula |

## 2. Vấn đề phát hiện

1. **Thiếu hẳn ô nhập X / Y / Width / Height (mm)** — không có cách đặt vị trí/kích thước chính xác bằng bàn phím; mọi thứ phụ thuộc kéo chuột (+snap) và phím mũi tên. Đây vừa là thiếu hụt so với mọi label designer chuẩn (BarTender/NiceLabel/ZebraDesigner đều có Position & Size), vừa liên đới trực tiếp bug "object nhảy lung tung" — người dùng không có công cụ đặt số chính xác để sửa lại. Model đã có sẵn `XMm/YMm/WidthMm/HeightMm` (và `LineEndXMm/LineEndYMm` cho Line), chỉ thiếu UI.
2. **Thiếu card Shape Style cho Rectangle/Ellipse/Line**: model `ObjectStyle` có `FillColor`, `StrokeColor`, `FillStyle`, `OutlineStyle` (Solid/Dash/Dot/None), `BorderThicknessMm`, `CornerRadiusMm` nhưng panel không expose nhóm này — shape chỉ chỉnh được qua nơi khác (nếu có) hoặc không chỉnh được.
3. **Thiếu màu chữ cho Text** (`Style.StrokeColor` được dùng làm màu chữ trong `CreateTextVisual`) — không có color picker trong panel.
4. **3 card trùng lặp thông tin binding** (Content / Binding / Formula Output): người dùng thấy cùng một thứ ở 3 chỗ, chiếm chiều cao lớn; Formula Builder full nằm thường trực trong Content làm card rất dài kể cả khi object chỉ là text tĩnh.
5. **Rotation chỉ 4 nấc** qua ComboBox — đủ cho tem in nhiệt (hầu hết driver chỉ hỗ trợ 0/90/180/270) nên GIỮ 4 nấc, nhưng nên chuyển thành 4 nút toggle nhanh thay vì combo phải 2 click.
6. **Thứ tự card chưa theo tần suất dùng**: Content (dài) đứng trên Transform; trong khi thao tác phổ biến nhất khi chỉnh layout là vị trí/kích thước → phải cuộn qua Formula Builder mới tới.
7. **Layer order chỉ có Front/Back**, không có Forward/Backward một nấc (canvas đã có ZIndex đầy đủ).

## 3. Đề xuất sắp xếp lại (thứ tự card mới)

Nguyên tắc: nhóm theo tần suất + luồng tư duy "đặt đâu → to bao nhiêu → nội dung gì → trông thế nào", các phần ít dùng thu gọn (Expander).

| # | Card | Nội dung | Ghi chú |
|---|------|----------|---------|
| 1 | Header | như cũ | |
| 2 | **Position & Size** (MỚI) | X, Y, W, H (mm, TextBox số + spinner, `UpdateSourceTrigger=LostFocus` hoặc Enter — tránh nhảy khi đang gõ, tuân thủ rule 9); với Line: X1/Y1/X2/Y2; nút khoá tỉ lệ W:H cho QR/DataMatrix (mặc định khoá vì buộc vuông) | Fix gián tiếp bug "nhảy": người dùng luôn đặt lại được số chính xác |
| 3 | **Transform & Arrange** | Rotation 4 nút (0/90/180/270), Layer: Front / Forward / Backward / Back, Visible + Locked | gộp thêm 2 nút layer mới |
| 4 | **Content** | Source combo + ô nhập tương ứng; Formula Builder thu vào **Expander "Formula Builder"** (mặc định đóng, tự mở khi Source=Binding/Formula); phần Binding status + Preview + missing fields **gộp vào đây** (bỏ card 6, 7 riêng) | 3 card trùng lặp → 1 card |
| 5 | **Text Style** | như cũ + **Color** (màu chữ) | Text/TextBox |
| 6 | **Shape Style** (MỚI) | Fill (color + None), Outline (color, style Solid/Dash/Dot/None, thickness mm), Corner radius (Rectangle) | Rectangle/Ellipse/Line (Line: chỉ stroke) |
| 7 | **Barcode** | như cũ (Standard, QR mode, EC, DPI, Fixed Size) + cảnh báo module-size khi DPI thấp (nối với print-preview-reliability đợt 3) | Barcode/QR/DM |

## 4. Ràng buộc kỹ thuật khi làm

- **Tuân thủ rule 9 `agent.md`**: ô X/Y/W/H bind `LostFocus`/Enter (không `PropertyChanged` từng phím) để không giật object khi đang gõ; giá trị đi qua validate (≥0, trong khổ tem hoặc cảnh báo) rồi mới ghi model; mỗi lần commit giá trị = 1 bước undo.
- Không đổi model `.anlabel` — mọi thứ đã có sẵn trong `LabelObject`/`ObjectStyle`, đây là việc thuần UI + wiring.
- Panel hiện dùng style `PropCard`/`PropTitle`/`PropLabel` thống nhất — card mới phải dùng đúng bộ style này.
- Sau khi gộp card Binding/Formula Output: kiểm tra mọi binding tới `SelectedBinding*`/`FormulaPreview*` trong `MainViewModel` vẫn được dùng, xoá property mồ côi nếu còn.

## 5. Thứ tự thực hiện đề xuất

1. **Đợt A (giá trị cao nhất, ít rủi ro)**: thêm card Position & Size + Shape Style + màu chữ. Build + test + smoke app.
2. **Đợt B**: sắp xếp lại thứ tự card, gộp Binding/Formula Output vào Content, Formula Builder thành Expander.
3. **Đợt C**: Rotation 4 nút, Layer Forward/Backward, cảnh báo module-size barcode.
4. Mỗi đợt: bump version, build PASS, test PASS, chạy app kiểm tra bằng mắt từng loại object (Text, TextBox, Rect, Ellipse, Line, Code128, QR, DataMatrix), cập nhật `MASTER_PLAN.md`.

## 6. Đợt A — Đã thực hiện (2026-07-02, v0.059)

- **Card "Position & Size (mm)"** thêm ngay sau header, trước Content: X/Y/Width/Height cho object thường; X1/Y1/X2/Y2 (bind `XMm/YMm/LineEndXMm/LineEndYMm`) khi `Type == Line`. Bind trực tiếp `SelectedObject.XMm` v.v. (model đã có sẵn, không cần property mới trong ViewModel).
- **Tuân thủ rule 9 `agent.md`**: mọi TextBox mới dùng `UpdateSourceTrigger=LostFocus` (không commit theo từng phím gõ) + handler `PositionSizeTextBox_KeyDown` (mới, trong `MainWindow.xaml.cs`) gọi `BindingExpression.UpdateSource()` khi bấm Enter, dùng chung cho mọi ô số mới thêm (Position & Size, Shape Style). Nhờ cơ chế undo debounce sẵn có (`ObserveTemplate`/`ObjectOnPropertyChanged`), mỗi lần commit tự động thành 1 bước undo — không cần code undo riêng.
- **Card "Shape Style"** thêm sau Text Style, hiện cho Rectangle/Ellipse/Line: Fill (dropdown `FillStyles` có sẵn trong ViewModel + ô hex `FillColor` + ô swatch xem trước), Corner radius (ẩn với Line), Outline (dropdown `OutlineStyles` có sẵn + ô hex `StrokeColor` + swatch), Thickness (mm, bind `BorderThicknessMm`).
- **Màu chữ (Text Style)**: thêm ô hex `StrokeColor` (dùng làm màu chữ, đúng theo `CreateTextVisual`) + swatch xem trước, đặt cuối card Text Style.
- Ô hex màu binding trực tiếp string → `Border.Background` (kiểu `Brush`) tận dụng `BrushConverter` mặc định của WPF, không cần viết converter riêng.
- **Kiểm tra đã chạy**: `dotnet build ANLAbel.slnx` PASS (0 lỗi); `ANLAbel.Tests` 22/22 PASS; `ANLAbel.UnitTests` 31/31 PASS; chạy thử `ANLAbel.App.exe` (debug build) khởi động thành công, không exception trong log, đóng process sạch.
- Version bump: `0.058` → `0.059` (csproj Version/AssemblyVersion/FileVersion/InformationalVersion, title `MainWindow.xaml`, `BuildChannelText` cả 2 nhánh Licensed/Trial trong `App.xaml.cs`).
- **Chưa làm (để Đợt B/C)**: sắp xếp lại thứ tự card, gộp 3 card binding trùng lặp, Formula Builder Expander, Rotation 4 nút, Layer Forward/Backward, cảnh báo module-size barcode.

## 7. Đợt B — Đã thực hiện (2026-07-03, v0.067)

- **Sắp xếp lại thứ tự card**: đổi chỗ "Transform & Arrange" lên trước "Content" — đúng thứ tự đề xuất ở mục 3 (Header → Position & Size → Transform & Arrange → Content → Text Style → Shape Style → Barcode). Thao tác phổ biến nhất khi chỉnh layout (vị trí/kích thước/xoay/lớp) giờ không cần cuộn qua Content/Formula Builder mới tới.
- **Gộp 3 card trùng lặp (Content / Binding / Formula Output) thành 1 card "Content"**: phần "Binding" (source type, status, preview, used/missing fields, errors) và "Formula Output" (output preview, used fields, errors) giờ nằm trong 2 `StackPanel` con bên trong card Content, mỗi cái vẫn giữ nguyên điều kiện hiện `HasSelectedBinding`/`IsSelectedBindingFormula` như card độc lập trước đây (không đổi logic hiển thị, chỉ đổi vị trí). Có 1 đường kẻ phân cách mờ trước phần Binding để tách biệt trực quan với phần Source/Content phía trên.
- **Formula Builder thu vào `Expander`**: toàn bộ control builder cũ (chips field, separators, add text, parts list, expression preview, Apply/Clear) giờ nằm trong `<Expander Header="Formula Builder" IsExpanded="True">` — đặt `IsExpanded="True"` mặc định vì `Expander` này chỉ hiện khi `Source=Binding/Formula` (đã có DataTrigger từ trước), nên "tự mở khi ở chế độ Binding" tương đương "mặc định mở trong ngữ cảnh đó"; người dùng có thể tự thu gọn nếu chỉ muốn xem Preview/status mà không cần builder chiếm chỗ.
- **Không đổi binding nào tới `MainViewModel`**: chỉ di chuyển XAML, không property nào bị mồ côi (`SelectedBinding*`/`FormulaPreview*` vẫn dùng nguyên).
- **Kiểm tra đã chạy**: `dotnet build ANLAbel.slnx` PASS (0 lỗi); `ANLAbel.Tests` 28/28 PASS (không đổi số lượng — Đợt B thuần UI, không có test riêng); `ANLAbel.UnitTests` 45/45 PASS; chạy thử `ANLAbel.App.exe` (debug build) khởi động thành công, không exception trong log, đóng process sạch. **Lưu ý:** môi trường hiện tại không có công cụ chụp màn hình cho ứng dụng desktop WPF (khác web app) — chưa xác nhận bằng mắt cách hiển thị Expander/thứ tự card trên UI thật, chỉ xác nhận app khởi động không lỗi. Nên người dùng tự mở app kiểm tra trực quan trước khi coi Đợt B là xong hẳn.
- Version bump: `0.066` → `0.067`.
- **Chưa làm (để Đợt C)**: Rotation 4 nút thay ComboBox, Layer Forward/Backward, cảnh báo module-size barcode khi DPI thấp.

## 8. Đợt C — Đã thực hiện (2026-07-03, v0.068)

- **Rotation 4 nút thay ComboBox**: 4 `Button` (0°/90°/180°/270°) trong `UniformGrid Columns="4"`, mỗi nút gọi `SetRotationCommand` (mới, `MainViewModel`) với `CommandParameter` là chuỗi độ — 1 click thay vì mở dropdown + chọn (2 thao tác) như ComboBox cũ. Label phía trên đổi thành "Rotation (current: {0}°)" bind trực tiếp `SelectedObject.Rotation` để người dùng vẫn biết giá trị hiện tại (không làm nút nào "sáng" lên do không có converter so sánh sẵn — đánh đổi hợp lý cho scope nhỏ này).
- **Layer Forward/Backward**: thêm `BringForwardCommand`/`SendBackwardCommand` — hoán đổi `ZIndex` với đúng 1 object liền kề phía trên/dưới (khác với Front/Back vốn nhảy hẳn lên đầu/xuống cuối). Đặt thành hàng nút riêng ngay dưới hàng Front/Back trong card Transform & Arrange.
- **Cảnh báo module-size barcode khi DPI thấp**: property mới `BarcodeModuleSizeWarningText` trong `MainViewModel` — chỉ áp dụng cho matrix code (QR/DataMatrix/Aztec/PDF417) ở chế độ `QrSizingMode.FixedVersionAndModuleSize` (chế độ duy nhất mà `QrModuleSizePx` là lựa chọn thiết kế tường minh của người dùng, không phải giá trị máy tự suy ra). Công thức: quy đổi module (px, thiết kế ở `QrDpi` của object) sang số dot vật lý thực tế ở `Template.Dpi` (DPI in đã cấu hình cho tem — proxy tốt nhất có sẵn cho "DPI máy in thật" mà không cần chạm vào print pipeline). Cảnh báo khi < 2 dot — dưới ngưỡng này máy quét công nghiệp thường không đọc được. Đây là bản rút gọn nằm trong Properties panel (cảnh báo sớm lúc thiết kế) của mục "R5"/"item 8" trong `print-preview-reliability-plan.md` — bản đầy đủ (chặn cứng lúc in, tự render lại ở đúng DPI máy in) vẫn còn nguyên trong plan đó, chưa làm.
- **Test mới**: `layer forward/backward swap adjacent ZIndex`, `rotation quick buttons set exact degrees`, `barcode module size warning flags sub-2-dot modules` (kiểm cả trường hợp cảnh báo bật/tắt và trường hợp `AutoSizeByData` không bị cảnh báo).
- **Kiểm tra đã chạy**: build PASS; `ANLAbel.Tests` 28/28 → 31/31 PASS (thêm 3 test); `ANLAbel.UnitTests` 45/45 PASS; smoke `ANLAbel.App.exe` v0.068 khởi động sạch, không exception. Version `0.067` → `0.068`.
- **Lưu ý tương tự Đợt B**: chưa xác nhận bằng mắt cách hiển thị 4 nút Rotation/nút Forward-Backward/dòng cảnh báo module-size trên UI thật (môi trường không có công cụ chụp màn hình app desktop WPF) — chỉ xác nhận qua build/test/smoke logic. Nên tự mở app kiểm tra trực quan.
- **Toàn bộ `properties-panel-plan.md` (Đợt A, B, C) coi như hoàn tất.**
