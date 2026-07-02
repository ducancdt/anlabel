# Plan: Kiểm tra & sắp xếp lại Properties panel

**Ngày:** 2026-07-02 · **Trạng thái:** Đợt A đã làm xong (xem mục 6); Đợt B/C chờ duyệt · Theo định hướng chủ dự án: "kiểm tra và sắp xếp lại properties"

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
