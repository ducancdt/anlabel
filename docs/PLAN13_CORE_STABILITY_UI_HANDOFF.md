# Plan 13 — handoff Figma → WPF cho các trạng thái lõi

Tài liệu này theo [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)
cho mọi thay đổi giao diện của kế hoạch
[`reinvention/13-ke-hoach-cai-to-loi.md`](reinvention/13-ke-hoach-cai-to-loi.md).
Việc chỉ lấy từ mục 9 của kế hoạch; tài liệu này không tạo việc.

## 1. Nhận diện slice

| Trường | Giá trị |
| --- | --- |
| Kết quả cho người vận hành | Không mất thiết kế khi Tạo mới/Mở/Đóng hoặc khi app gặp lỗi; lưu thành tệp mới; mở lại bản phục hồi; khởi động được dù cấu hình hỏng |
| Trong phạm vi | GĐ2 (F01–F07) đã làm; 1.5, GĐ3, D07, GĐ4, GĐ5 đã có frame và làm ở các slice sau |
| Ngoài phạm vi | Bố cục và hành vi Text/TextBox; mọi bề mặt ngoài lõi |
| Kế hoạch liên quan | `reinvention/13-ke-hoach-cai-to-loi.md` §4 GĐ2 và §9 |
| Ngày | 2026-10-13 |

## 2. Nguồn Figma

| Trường | Giá trị |
| --- | --- |
| File | https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-Shell?node-id=36-137 |
| Node gốc | `36:137` “ANLAbel — Plan 13 core stability states v0.307” |
| Tạo/cập nhật bằng @figma | Có (2026-10-13) |
| Tái sử dụng | Biến màu và khoảng cách `ANLAbel / Print Preview`, `ANLAbel / Print Preview Size`; font Inter như các frame hiện có; component mới `P13 / Nút hộp thoại / Chính` (`36:133`) và `/ Phụ` (`36:135`) |

| Thẻ | Node |
| --- | --- |
| F01 · Tiêu đề cửa sổ (chưa lưu / đã lưu / mẫu mới) | `36:141` |
| F01 · Hộp thoại “Lưu thay đổi?” | `36:150` |
| F02 · Thanh truy cập nhanh có “Lưu thành…” | `36:165` |
| F03 · Lỗi không mong muốn | `36:188` |
| F04 · Tìm thấy bản phục hồi | `36:201` |
| F05 · Đã khôi phục cấu hình | `36:216` |
| 1.5 · Chuẩn mã vạch theo bằng chứng | `37:142` |
| GĐ3 · Menu chuột phải canvas tiếng Việt | `37:208` |
| D07 · Thiếu phông chữ / ảnh | `37:259` |
| GĐ4 · “Cập nhật Excel” thất bại | `37:269` |
| GĐ5 · Lệnh in chưa rõ, nhật ký hỏng, dòng đã gửi | `37:280` |
| GĐ5 · Lệnh in bị dừng / bị chặn (giải thích tiếng Việt) | Chưa có node riêng: ngày 2026-10-13 hết lượt gọi Figma MCP (gói Starter). Bố cục là hộp thoại F03 `36:188` (tiêu đề, dòng vấn đề, dòng việc cần làm, “Chi tiết kỹ thuật”, một nút chính); nội dung chữ ở §3a. Tạo thẻ khi có lượt gọi |

## 3. Trạng thái

| Trạng thái | Node | Nguồn runtime | Hành động chính |
| --- | --- | --- | --- |
| Sạch (đã lưu/mới mở) | `36:141` dòng 2–3 | `MainViewModel.IsDirty == false` | — |
| Có thay đổi chưa lưu | `36:141` dòng 1 | `IsDirty == true`, `WindowTitle` có `*` | Lưu (Ctrl+S) |
| Hỏi trước khi thay tài liệu | `36:150` | `HasUnsavedChanges()` trước Tạo mới/Mở/Thư viện/Phiên bản/Đóng | Lưu |
| Đang lưu | — | `IsSaving` (lần lưu thứ hai bị từ chối, thanh trạng thái báo “Đang lưu mẫu tem…”) | — |
| Lưu thất bại | `36:150` kiểu lỗi | `SaveDocumentAsync` bắt lỗi, ghi bản phục hồi | Đóng, sửa nguyên nhân, lưu lại |
| Lỗi không mong muốn | `36:188` | `App.OnDispatcherUnhandledException` | Tiếp tục làm việc |
| Có bản phục hồi | `36:201` | `DocumentRecoveryService.FindOrphans()` | Mở bản phục hồi |
| Cấu hình hỏng | `36:216` | `MainViewModel.StartupNotices` | Đã hiểu |

### 3a. Lệnh in bị dừng hoặc bị chặn (GĐ5, H25)

Thông điệp kỹ thuật tiếng Anh của lõi giữ nguyên trong nhật ký và test;
giao diện ánh xạ chúng ở `Services/PrintFailureExplainer.cs` (lệnh in dừng)
và `Services/PreflightIssueText.cs` (vấn đề kiểm tra trước khi in). Thông điệp
chưa có quy tắc vẫn hiện nguyên văn, không bị giấu.

| Trạng thái | Nguồn runtime | Nội dung |
| --- | --- | --- |
| In thất bại (Xem trước) | `PrintPreviewWindow.Print_Click`, `OperatorDialog` tông lỗi | Tiêu đề “In thất bại”; dòng 1 = vấn đề (kèm “Chưa có tem nào được gửi.” khi dừng trước khi gửi); dòng 2 = việc cần làm; “Chi tiết kỹ thuật: …”; nút “Đóng” |
| Lệnh in chưa hoàn tất | `printResult.Outcome == Failed` | Như trên, tông cảnh báo |
| Chưa xác nhận được cài đặt máy in | `CreateEffectivePlanAsync` lỗi | Vấn đề + “Kiểm tra máy in đang bật, mở Cài đặt máy in rồi thử lại.” |
| Lệnh in bị chặn (kiểm tra trước khi in) | `PreflightIssueText.Describe` | “Kiểm tra trước khi in phát hiện N vấn đề. Chưa có tem nào được gửi.”; mỗi dòng `- Dòng 2 · “SKU” (mã QR): dữ liệu thiếu cột Part; đối tượng sẽ in trống`; số dòng là dòng dữ liệu, không phải số thứ tự tem |
| Bỏ qua dòng lỗi | Hộp thoại Có/Không/Hủy | “N dòng có lỗi và sẽ được bỏ qua: dòng …” + danh sách như trên |
| Danh sách “Vấn đề nội dung” | `PreviewContentIssue.Summary` / `NavigationText` | `PreflightIssueText.Line`; “Đi tới tem đầu tiên bị ảnh hưởng” hoặc “Xem lại thiết kế hoặc cài đặt tem” |
| In nhanh | `MainViewModel.StatusText` | “In thất bại: ” + vấn đề + việc cần làm; với lỗi kiểm tra thì nêu đối tượng đầu tiên và “Mở Xem trước để xem các dòng và đối tượng bị lỗi” |
| Lệnh bất đồng bộ thất bại | `App.ReportFailedOperation` | Cùng bộ giải thích; chi tiết ghi vào `logs\errors.log` |

## 4. Bố cục

| Yêu cầu | Quyết định |
| --- | --- |
| Hộp thoại | Rộng 460, cao theo nội dung, không đổi kích thước, giữa cửa sổ chủ; dòng chữ tự xuống dòng |
| Thứ tự phím | Nút chính = Enter, nút Hủy/Để sau/Tiếp tục = Esc và nút đóng cửa sổ |
| 1024 × 600 và 100–200 % | Hộp thoại không phụ thuộc kích thước cửa sổ chính; nút truy cập nhanh mới dùng cùng style `QuickAccessButton` |
| Chữ dài | Tên mẫu dài xuống dòng trong hộp thoại; tiêu đề cửa sổ do Windows cắt |

## 5. Ánh xạ Figma → WPF

| Node | WPF | AutomationId | Nguồn dữ liệu |
| --- | --- | --- | --- |
| `36:150`, `36:188`, `36:201`, `36:216`, `37:269` | `OperatorDialog` | `OperatorDialog`, `OperatorDialog.Heading`, `OperatorDialog.Choice.<id>` | Lời gọi trong `MainWindow.DocumentLifecycle.cs`, `App.xaml.cs` |
| `36:141` | `MainWindow.Title` (binding `WindowTitle`) | — | `MainViewModel.WindowTitle` |
| `36:165` | Nút truy cập nhanh `SaveAs_Click`, ảnh `Icons/save_as.png` (`tools/IconGenerator`, `DrawSaveAs`) | `Shell.QuickAccess.SaveAs` | `ApplicationCommands.SaveAs`, Ctrl+Shift+S |
| `36:133` / `36:135` | Nút trong `OperatorDialog` (chính: nền `AppPrimaryBrush`; phụ: style Button mặc định) | `OperatorDialog.Choice.<id>` | — |

## 6. Bằng chứng runtime

| Gate | Kết quả (2026-10-13) |
| --- | --- |
| XAML biên dịch 0 cảnh báo | Đạt (`dotnet build ANLAbel.slnx`) |
| Trạng thái chưa lưu, Undo về trạng thái đã lưu, Lưu | `unsaved marker follows edits, undo and save` — PASS |
| Lưu từ bản chụp, từ chối lưu chồng | `save writes a snapshot and refuses a concurrent save` — PASS |
| Bản phục hồi: chỉ phiên đã chết, mở ở trạng thái chưa lưu, không chạm tệp gốc | `recovery copy is offered once and opens unsaved` — PASS |
| Cấu hình hỏng không chặn khởi động | `corrupt data-source registry is quarantined at startup` — PASS; unit `DocumentLifecycleFoundationTests` |
| Mở tệp từ Explorer | `explorer argument opens only existing documents` — PASS |
| Lỗi lệnh bất đồng bộ tới người vận hành | `async command failure reaches the operator` — PASS |
| Round-trip mẫu thư viện, hình học không đổi | `library templates round-trip without geometry change` — PASS |
| Chuỗi tiếng Việt | `OperatorDialog.xaml` nằm trong danh sách cưỡng chế của bộ quét Việt hóa — PASS |
| Thao tác chuột thật trên hộp thoại | Chưa kiểm tra bằng tay; hộp thoại dùng `ShowDialog` nên test tự động không bấm nút |
| Lệnh in dừng/bị chặn nói tiếng Việt, đúng dòng dữ liệu | `print stops are explained in plain Vietnamese`, `preflight issues name the data row in Vietnamese` — PASS; mỗi quy tắc neo vào chuỗi lõi nên đổi chuỗi lõi sẽ làm gate đỏ |

## 7. Điều kiện hoàn tất

Slice GĐ2 hoàn tất khi node Figma, ánh xạ WPF, bằng chứng tự động và bản phát
hành khớp nhau. Các thẻ 1.5, GĐ3, D07, GĐ4, GĐ5 đã có node; phần WPF của chúng
được ghi vào bảng này khi làm.
