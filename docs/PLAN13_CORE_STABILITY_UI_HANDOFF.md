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
| 1.5 · Chuẩn mã vạch theo bằng chứng | `37:142`. Chỉ phần tên nhóm tiếng Việt được áp dụng: ngày 2026-10-13 cả 15 loại đã đủ bằng chứng nên không có nhóm “mẫu cũ” |
| GĐ3 · Menu chuột phải canvas tiếng Việt | `37:208`. WPF: `LabelDesignerCanvas.ContextMenu` (Dính theo đối tượng, Dính theo lưới, Bước lưới, Đường gióng); gate `canvas context menu is Vietnamese without developer items` |
| D07 · Thiếu phông chữ / ảnh | `37:259`. WPF: chip `Shell.Status.MissingResources` (binding `MissingResourcesText`/`MissingResourcesDetail`), bấm để chọn đối tượng đầu tiên |
| GĐ4 · “Cập nhật Excel” thất bại | `37:269`. WPF: `OperatorDialog` “Chưa đọc được dữ liệu” (lệnh Cập nhật/Liên kết lại) và “Nhập dữ liệu thất bại” (cửa sổ nhập), nội dung từ `DataFailureExplainer` |
| GĐ5 · Lệnh in chưa rõ, nhật ký hỏng, dòng đã gửi | `37:280` |
| GĐ5 · Lệnh in bị dừng / bị chặn (giải thích tiếng Việt) | Chưa có node riêng: ngày 2026-10-13 hết lượt gọi Figma MCP (gói Starter). Bố cục là hộp thoại F03 `36:188` (tiêu đề, dòng vấn đề, dòng việc cần làm, “Chi tiết kỹ thuật”, một nút chính); nội dung chữ ở §3a. Tạo thẻ khi có lượt gọi. Ngày 2026-10-14: tài khoản Figma của MCP chỉ có ghế View nên không ghi được tệp; cần tài khoản có quyền sửa |

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

### 3b. Kiểm tra toàn ứng dụng (2026-10-13, v0.309) — chữ mới chưa có node Figma

Lượt gọi Figma MCP của gói hiện tại đã hết, nên các thay đổi dưới đây được ghi ở
đây thay cho node; cần vẽ thẻ tương ứng khi có lượt gọi. Không có bố cục mới:
chỉ hộp thoại `OperatorDialog` sẵn có, dòng trạng thái và chữ trong control cũ.

| Trạng thái / chỗ hiển thị | Nguồn runtime | Nội dung |
| --- | --- | --- |
| Tệp đã được lưu ở nơi khác | `SaveDocumentAsync` bắt `DocumentChangedOnDiskException` | Tiêu đề “Tệp đã được lưu ở nơi khác”; nút “Lưu thành tệp khác…” (mặc định), “Ghi đè”, “Hủy” |
| Windows chưa trả lời danh sách máy in | `PrinterQueueLoader` sau 20 giây | Tiêu đề “Windows chưa trả lời”; nêu Print Spooler hoặc máy in mạng; nút “Đóng” |
| Không mở được mẫu tem | `OpenDocumentPathAsync` + `TemplateFailureExplainer` | Vấn đề (tệp hỏng, bản mới hơn, bị khóa, có bản sao lưu đọc được…), việc cần làm, “Mẫu đang mở không bị thay đổi.”; chi tiết kỹ thuật ở dòng phụ |
| Chip máy in, thanh trạng thái khi mở app | `PrinterQueueStatusMessage` + `PrintFailureExplainer.ExplainQueueStatus` | Ví dụ “Chưa chọn máy in cho mẫu này. Mở Cài đặt máy in…”; không nói “chưa gửi tem” vì chưa in |
| Sau khi in | `PrintStatusText.Queue` / `.Result` | “Lệnh hàng đợi #12: đang in. Chưa xác minh tem đã ra.” |
| Bảng Thuộc tính, danh sách Lớp, danh sách chọn | `DisplayNames`, `DisplayNameConverter`, `FormulaErrorText` | Tên loại như hộp công cụ (Chữ, Ô chữ, Mã vạch…); giá trị lưu vẫn là tên enum |
| Trung tâm in, Lịch sử in, Phiên bản mẫu tem | `PrintCenterText`, `PrintHistoryViewModel`, `RevisionText` | Trạng thái, thao tác, hàng đợi, lý do và kết quả bằng tiếng Việt; nhật ký giữ chữ gốc làm bằng chứng |
| Cài đặt máy in | `MediaCompatibilityText`, `CapabilityStatusText` | Thẻ Tương thích và trạng thái trình điều khiển tiếng Việt; chẩn đoán gốc nằm trong tooltip “Chi tiết kỹ thuật” |
| Đóng khi đang in nhanh (v0.310) | `MainWindow_Closing` khi `IsQuickPrinting` | Tiêu đề “Đang gửi lệnh in”; nút “Chờ in xong” (mặc định, Esc) và “Vẫn đóng”; cài bản cập nhật trong lúc in thì báo chờ in xong |
| Không ghi được kết quả lệnh in (v0.310) | `_printEvidenceDropped` sau khi gửi | Thêm vào dòng kết quả: “Chưa ghi được kết quả lệnh in vào nhật ký nên lệnh này sẽ hiện trong Trung tâm in như chưa rõ kết quả; kiểm tra tem đã ra trước khi in lại.” |
| Sau khi nhập/Cập nhật Excel (DB03, 2026-10-14) | `MainViewModel.StatusText`, `DataHeaderText.DescribeImport` | Ví dụ “Đã nhập 2 dòng từ data.xlsx / Data — Dòng tiêu đề 1 không có các cột “Lot” (lần nhập trước ở cột B), “Qty” (lần nhập trước ở cột C); 2 đối tượng dùng các cột này chưa in được.” Cột đang dùng có tiêu đề trùng/trống: “Tiêu đề “Part No” lặp ở cột A, D (ANLAbel tạm gọi là “Part No”, “Part No_2”); cột C không có tiêu đề (ANLAbel tạm gọi là “Column3”); 3 đối tượng dùng các cột này chưa in được cho tới khi mỗi cột có tiêu đề riêng.” Không dùng tới: “Dòng tiêu đề 1 để trống ở cột C; tiêu đề “Part No” lặp ở cột A, D.” |
| Vấn đề nội dung / In nhanh bị chặn (DB03) | `DataHeaderCheck` + `PreflightIssueText` | “Mẫu · “Mã sau” (chữ): trường “Part No_2” lấy từ cột D, nhưng tiêu đề “Part No” lặp ở các cột A, D nên chỉ nhận ra được theo vị trí cột; đặt tên riêng cho từng cột trong tệp dữ liệu rồi bấm Cập nhật Excel” |
| Bảng thuộc tính · Ràng buộc, danh sách Lớp (DB03, sau v0.312) | `MainViewModel.WithPositionalColumns`, `DataHeaderText.DescribeColumn` | Lỗi đỏ: “Cột D có tiêu đề “Part No” trùng với cột A (ANLAbel tạm gọi là “Part No_2”).” hoặc “Cột C không có tiêu đề (ANLAbel tạm gọi là “Column3”).”; trạng thái: “Liên kết đọc cột chỉ nhận ra được theo vị trí nên chưa in được. Đặt tiêu đề riêng cho cột trong tệp rồi bấm Cập nhật Excel.”; danh sách Lớp: “Cột chỉ theo vị trí: Part No_2” |
| Chip “Thiếu phông/ảnh” (D07, 2026-10-14) | `MainViewModel.RefreshMissingResources` | Thêm “1 ảnh hỏng” (hoặc “Thiếu 1 phông chữ; 1 ảnh hỏng”); chú thích: “Ảnh “…” bị hỏng, không đọc được; hãy chèn lại ảnh.” Bố cục chip giữ như Figma `37:259` |
| Sau review 2026-10-14 (DB03, B04, B05) | `PreflightIssueText`, `DataHeaderText` | “trường “PartNo” khớp ngang nhau với các cột A (Part No), E (Part-No) nên chỉ thứ tự cột quyết định cột nào được in; …”; “mã 2D của dòng này không vừa khung ở 203 DPI khi mỗi ô cần ít nhất một chấm in nên sẽ bị bóp hoặc bị cắt; …”; B05 thành “mã vạch của dòng này rộng hơn tem theo độ rộng vạch …” để gộp “Dòng 2, 3”; dòng trạng thái thêm “cột số bản in dùng các cột này chưa in được”, “Tên “…” khớp ngang nhau với …” và giữ “1 đối tượng có liên kết bị thiếu/hỏng.” cạnh lời báo tiêu đề |
| Sau review lần hai 2026-10-14 (DB03, B04, B05) | `DataHeaderText`, `MainViewModel.WithPositionalColumns`, `BuildObjectTreeBindingIssueText`, `PreflightIssueText`, `LabelDesignerCanvas.GetBarcodeError` | Lời báo nhập khi chỉ có tên khớp ngang nhau: “…; 2 đối tượng dùng các cột này chưa in được cho tới khi mỗi tên chỉ khớp một cột.” (có cả tiêu đề trống/trùng: “…cho tới khi mỗi cột có tiêu đề riêng và mỗi tên chỉ khớp một cột.”); trạng thái Ràng buộc: “Tên trong liên kết khớp ngang nhau với nhiều cột nên chưa in được. Đổi tên một trong các cột đó trong tệp hoặc liên kết đúng tên tiêu đề, rồi bấm Cập nhật Excel.”; danh sách Lớp: “Tên khớp nhiều cột: PartNo” / “2 tên khớp nhiều cột”; B05 thành “mã vạch của dòng này tràn ra ngoài mép tem ở độ rộng vạch đã đặt; …”; tooltip lỗi trên canvas: “Mã 2D không vừa khung ở 203 DPI khi mỗi ô cần ít nhất một chấm in nên bản in sẽ bị bóp hoặc bị cắt. Nới khung hoặc rút ngắn dữ liệu.”; dòng trạng thái sau Mở, Xác thực, Liên kết lại giữ “1 đối tượng có liên kết bị thiếu/hỏng.” |
| Sau review lần ba 2026-10-14 (v0.317) | `LabelDesignerCanvas.GetBarcodeError`, `MainViewModel.EvaluateBinding`, `BuildObjectTreeBindingIssueText`, `PreflightIssueText` | Tooltip lỗi của QR phiên bản cố định quá tải: “Dữ liệu (306 byte) vượt dung lượng phiên bản QR đã chọn; chọn phiên bản lớn hơn hoặc Tự động.”; dấu ngoặc trống: Bảng thuộc tính “Dấu ngoặc {} chưa có tên cột. Nhập tên cột vào giữa hai dấu ngoặc hoặc xóa chúng.”, danh sách Lớp “Lỗi liên kết” (liên kết công thức vẫn là “Lỗi công thức”), lệnh in dừng “dấu ngoặc {} chưa có tên cột nên đối tượng sẽ in trống; nhập tên cột vào giữa hai dấu ngoặc hoặc xóa chúng” |
| Designer vẽ mã vạch (B06, 2026-10-14) | `LabelDesignerCanvas.UpdateBarcodePanel` | Không có chữ hay bố cục mới: mã vạch trên canvas có đúng kích thước sẽ in (ADR-025) |
| Chưa đổi | Thẻ Ô chữ trong Bảng thuộc tính | Banner và tùy chọn của Ô chữ thuộc hợp đồng Text/TextBox; chờ chủ sản phẩm duyệt |

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
| Chữ mục 3b (v0.309) | `printer, print, template and data failures read in Vietnamese` — PASS; `saving over another window's save asks first`, `printer list wait is bounded and reuses a stuck enumeration` — PASS; UIA smoke 15/15 trên bản cài v0.309 |
| Chữ mục 3b sau review lần hai (2026-10-14) | `ties, transforms and copies are checked as printing reads them`, `a linear symbol past the label edge blocks its row`, `an oversized 2D symbol blocks and the designer draws it like the print`, `printer, print, template and data failures read in Vietnamese` — PASS; trên bản cài 0.316, QR khung 5 mm liên kết giá trị dài có khung lỗi đỏ (chụp cửa sổ, chưa rê chuột thật để xem tooltip) |

## 7. Điều kiện hoàn tất

Slice GĐ2 hoàn tất khi node Figma, ánh xạ WPF, bằng chứng tự động và bản phát
hành khớp nhau. Các thẻ 1.5, GĐ3, D07, GĐ4, GĐ5 đã có node; phần WPF của chúng
được ghi vào bảng này khi làm.
