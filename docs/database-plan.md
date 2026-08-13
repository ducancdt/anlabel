# Plan: Quản lý dữ liệu database/Excel đầu vào

**Ngày:** 2026-07-03 · **Trạng thái:** Giai đoạn 1, Giai đoạn 2, Giai đoạn TC (TC1–TC7) đều hoàn tất. Giai đoạn 3 item 8 (header-row picker + preview) ✅ xong (v0.079). Còn lại: CSV, lazy-load file lớn, ODBC/SQL — làm theo nhu cầu thực tế, chưa có yêu cầu cụ thể.

> **Current continuation (2026-08-13):** Các trạng thái và version trong file này là checkpoint lịch sử; không tự suy ra một release mới từ worktree đang có implementation chưa commit. Theo dõi reconciliation và ownership boundary ở [continuation handoff](reinvention/10-continuation-handoff-2026-08-13.md). Metadata Figma read-only cho luồng Excel link verification (Not linked / Checking / Verified / Stale / Failed, node `22:82`) được ghi trong [Figma → WPF handoff template](figma-ui-handoff-template.md) và [designer shell/panel/Excel owner packet](DESIGNER_SHELL_PANEL_EXCEL_VERIFICATION_DECISION_PACKET.md); đó là evidence UX, không thay thế runtime smoke/test.

## Định hướng từ chủ dự án (2026-07-02, ưu tiên cao nhất)

1. **Đẩy mạnh phần gắn database (binding)** — đây là tính năng lõi của sản phẩm; ưu tiên hoàn thiện luồng gắn dữ liệu vào object (Excel field / formula) mượt và rõ ràng hơn.
2. **Siết chặt độ tin cậy phần database** — mọi thao tác dữ liệu (import, refresh, relink, restore khi mở file) phải đoán được, không mất dữ liệu, không treo, sai ở đâu báo rõ ở đó. Xem "Giai đoạn TC — Siết tin cậy" bên dưới, làm **trước** phần mở rộng (GĐ3).
3. **Template thư viện KHÔNG tự gắn Excel nữa** — ✅ đã thực hiện (commit `c3da135`): gỡ `sample-data.xlsx` khỏi bundle, `DatabaseConfig.FilePath/SheetName` của mọi template thư viện để trống, test `template library standalone (no sample-data link)` bảo vệ quyết định này. Người dùng mở template rồi tự Import Excel của họ; `BindingExpression` placeholder giữ nguyên để map vào cột thật. Rule 8 trong `agent.md` đã cập nhật theo.

## Hiện trạng (đã audit)

Luồng dữ liệu hiện tại của một template `.anlabel`:

- `DatabaseConfig` trong file template lưu: `FilePath` (đường dẫn **tuyệt đối** tới file Excel), `SheetName`, `HeaderRowIndex`, `LastSelectedRow`, `AvailableFields` (mọi cột đọc được), `LabelFields` (cột được phép dùng trên tem).
- Import qua dialog `Import Excel` (`ExcelImportWindow`) → `ExcelDataService` (ClosedXML) đọc header dòng 1 + toàn bộ rows thành chuỗi.
- Mở lại template: nếu file Excel còn tồn tại đúng đường dẫn thì tự restore data + row cuối đã chọn; mất file thì báo link hỏng nhưng template vẫn mở được.
- Binding `{Field}` / `FIELD("...")` có cơ chế match mềm (bỏ khoảng trắng/gạch/hoa-thường) và tự repair khi header đổi nhẹ.
- Template thư viện: **từ commit `c3da135` không còn ship `sample-data.xlsx` và không tự gắn link Excel** — `DatabaseConfig` để trống, người dùng tự Import Excel sau khi mở template (quyết định chủ dự án 2026-07-02).

### Điểm yếu chính

| # | Vấn đề | Hệ quả thực tế |
|---|--------|----------------|
| 1 | `FilePath` tuyệt đối theo máy tạo template | Chuyển template sang máy khác / đổi tên thư mục là mất link Excel; đây chính là họ lỗi "mất link" người dùng gặp |
| 2 | Không có khái niệm "data source" độc lập với template | Mỗi template tự giữ 1 đường dẫn; 10 template dùng chung 1 file Excel thì đổi chỗ file phải sửa 10 template |
| 3 | Đọc toàn bộ sheet vào RAM dạng string | File Excel lớn (vài chục nghìn dòng) sẽ chậm/nặng; đã có 1 bug treo UI (đã sửa 2026-07-02) |
| 4 | Không theo dõi file Excel thay đổi | Người dùng sửa Excel bên ngoài phải nhớ bấm `Update Excel`; quên là in dữ liệu cũ |
| 5 | Chỉ hỗ trợ `.xlsx/.xlsm`, header luôn dòng 1 | Chưa nhận CSV, chưa chọn được dòng header, chưa lọc/tìm kiếm row |
| 6 | `LastSelectedRow` theo index | Excel bị chèn/xoá dòng là preview nhảy sang bản ghi khác mà không cảnh báo (chưa dùng `KeyField` dù model đã có sẵn trường này) |
| 7 | `ExtractSampleData()` **ghi đè** `sample-data.xlsx` mỗi lần khởi động app (`TemplateLibraryService.cs`, `File.Create(target)`) | Người dùng sửa file dữ liệu mẫu (nhiều người dùng nó làm data thật ban đầu) → mở lại app là mất sạch chỉnh sửa |
| 8 | Đọc Excel không có cancel/timeout | File trên network share bị treo thì wait cursor treo theo, không huỷ được (audit mục 5) |
| 9 | Không có báo cáo schema sau import | Cột bị đổi tên/xoá chỉ lộ ra dạng lỗi từng object trên canvas; không có tóm tắt "template cần cột X, Y, Z — thiếu Z" một chỗ |

## Kiến trúc đề xuất: Data Source Manager

### Giai đoạn 1 — Sửa họ lỗi "mất link" (ưu tiên cao, ít rủi ro)

1. ✅ **Lưu đường dẫn kép trong `DatabaseConfig`**: giữ `FilePath` tuyệt đối (tương thích ngược) + thêm `RelativePath` (tương đối so với vị trí file `.anlabel`). Khi mở template, thử theo thứ tự: tuyệt đối → tương đối → cùng thư mục với `.anlabel` → báo link hỏng.
2. ✅ **Nút re-link rõ ràng**: khi báo "Excel link could not be restored", hiện nút re-link (`RelinkExcelCommand`) — người dùng sửa được ngay tại chỗ.
3. ✅ **Test**: `template excel link survives folder move` — save template + Excel cùng thư mục, copy cả thư mục sang chỗ khác, xóa gốc, mở lại → tự resolve bằng RelativePath. 19/19 test PASS.
3b. ✅ **Không ghi đè `sample-data.xlsx` vô điều kiện** (điểm yếu 7): `ExtractSampleData()` đã bị xóa khỏi `TemplateLibraryService.cs`. Template thư viện giờ dùng embedded resource trực tiếp, không giải nén file Excel riêng.

### Giai đoạn 2 — Data source dùng chung + cập nhật chủ động

4. ✅ **Tách `DataSource` thành đối tượng quản lý riêng** (v0.066): nối `DataSourceRegistry` vào `MainViewModel` (load lúc khởi tạo, tham số tuỳ chọn để test dùng registry riêng). Panel `Data Sources` bên trái có thẻ "Shared Data Sources" mới:
   - Nút "Save current Excel link as shared source" (`AddCurrentAsDataSourceCommand`) tạo `DataSource` từ FilePath/SheetName/HeaderRowIndex đang link, gán `Template.DatabaseConfig.DataSourceId`.
   - Mỗi source hiển thị Name (sửa trực tiếp, tự lưu registry khi mất focus), FilePath, SheetName, và 3 nút: **Use** (`UseDataSourceCommand` — trỏ template hiện tại vào source này rồi import), **Relink...** (`RelinkDataSourceCommand` — mở dialog chọn file mới, cập nhật path/sheet của source, tự import lại nếu template đang dùng đúng source đó), **Remove** (`RemoveDataSourceCommand` — xoá khỏi registry, template quay về theo dõi bằng `FilePath` riêng, không tự xoá dữ liệu template).
   - **Fallback tương thích ngược đã hoạt động đúng như thiết kế**: khi mở template có `DataSourceId`, `RestoreLinkedExcelDataAsync` ưu tiên đọc FilePath/SheetName/HeaderRowIndex hiện tại từ registry (nếu source còn tồn tại) *trước khi* chạy chuỗi resolve tuyệt đối→tương đối→cùng thư mục — nghĩa là **relink 1 nguồn dùng chung sẽ tự sửa cho mọi template tham chiếu nó**, không cần relink từng template. Template không có `DataSourceId` (file cũ) vẫn chạy y hệt luồng trước đây.
   - Test end-to-end mới: `shared data source relink fixes every referencing template` — tạo source từ 1 template, lưu template, "di chuyển" file bằng cách sửa path trong registry (mô phỏng thao tác Relink), mở lại template trong phiên `MainViewModel` mới → xác nhận tự động trỏ đúng file mới, không báo link hỏng, đọc đúng dữ liệu mới.
5. ✅ **Theo dõi file thay đổi** (v0.065): `FileSystemWatcher` (khởi động trong `StartWatchingExcelFile`, gọi mỗi lần Import/Refresh/Relink/Open thành công) theo dõi file Excel đang link, debounce 1s bằng `System.Threading.Timer` (không dùng `DispatcherTimer` để không phụ thuộc message loop — chạy được cả khi test không có `Application` WPF). Khi file đổi thật sự (so `LastWriteTimeUtc` để bỏ qua touch không đổi nội dung), panel Database hiện dòng cảnh báo `ExcelStaleNoticeText` + nút "↻ Update Excel" (bind `RefreshExcelDataCommand` có sẵn) — **không tự reload ngầm**, đúng yêu cầu tránh giật dữ liệu giữa lúc đang thiết kế/in. Watcher tự dừng khi tạo template mới hoặc mất link. Test: `linked excel file watcher flags stale data` — sửa file ngoài app, chờ cờ `IsExcelDataStale` bật lên, xác nhận dữ liệu trong RAM chưa đổi cho tới khi bấm Update, rồi mới nạp dữ liệu mới.
6. ✅ **Khoá theo `KeyField`** (hoàn tất qua TC5, v0.064 — xem "Giai đoạn TC" bên dưới): cho phép chọn 1 cột làm khoá; `LastSelectedRow` lưu thêm giá trị khoá, khi reload thì tìm lại đúng bản ghi theo khoá trước, theo index sau, lệch thì cảnh báo. UI đặt trong panel Database ("Row Tracking Key" ComboBox) thay vì trong dialog import như đề xuất ban đầu — hiệu quả tương đương, ít thay đổi dialog hiện có hơn.
6b. ✅ **Cancel/timeout khi đọc file** (điểm yếu 8): `CancellationToken` cho `GetSheetNames`/`LoadSheetAsync`, nút Cancel trên UI, timeout 30s cho đường dẫn UNC/network và mở stream bằng `FileShare.ReadWrite`. Luồng re-link cũng dùng API async, không đọc workbook trên UI thread.
6c. **Báo cáo schema sau import/refresh** (điểm yếu 9): gom mọi `BindingExpression`/formula trong template → danh sách cột cần có → so với header thực tế → hiện tóm tắt cột thiếu một chỗ (status bar + dialog khi mở template). Cache kết quả đọc theo `(path, sheet, LastWriteTimeUtc)` để refresh không đọc lại file chưa đổi.

### Giai đoạn TC — Siết tin cậy database (ưu tiên mới 2026-07-02, làm trước GĐ3)

Mục tiêu: mọi con đường dữ liệu đi vào tem đều **đoán được và kiểm chứng được**. Các hạng mục:

TC1. **Báo cáo schema một chỗ** — ✅ **HOÀN TẤT TOÀN BỘ**: `MainViewModel.GetBindingIssues()` + panel "Binding Issues" (`MainWindow.xaml` dòng ~638) liệt kê từng object có `BindingExpression` bị thiếu cột/lỗi, tự refresh sau `RaiseDatabaseFieldStateChanged()`. `StatusText` sau `ImportExcelAsync` nối thêm số lượng object có vấn đề binding (v0.060). Phần "chặn cứng lúc in khi thiếu cột" — xác nhận lại 2026-07-03: đã có từ trước qua `PrintPreflightValidator.ValidateBindingFieldsPresent` (kiểm cả binding `{Field}` qua `FieldNameResolver` lẫn formula qua `FormulaEvaluationResult.Errors`), test `print preflight blocks missing bound field` bảo vệ — ghi chú "còn thiếu" trước đó là lỗi thời, không phải việc còn tồn đọng.
TC2. ✅ **Kiểm chứng round-trip khi save/open** (v0.061): test `database config full round trip` — populate đầy đủ `DatabaseConfig` (DataSourceId, FilePath, RelativePath, SheetName, HeaderRowIndex, KeyField, KeyValue, LastSelectedRow, AvailableFields, LabelFields) qua `ProjectFileService.SaveAsync`/`LoadAsync`, assert từng field giữ nguyên sau round-trip JSON.
TC3. **Trạng thái link hiển thị tường minh** — ⚠️ **đã có sẵn phần lớn**: panel Database (`MainWindow.xaml` ~dòng 606-609) đã hiển thị `LinkedExcelSourceText` (file/sheet), `ExcelLinkStatusText` (cảnh báo đỏ khi hỏng) + nút `RelinkExcelCommand`. ✅ 2026-07-02 (v0.060) bổ sung `ExcelDataFreshnessText` ("Data read at HH:mm:ss") ngay dưới tên file/sheet — phần còn thiếu duy nhất là hiển thị số dòng ngay tại đây (hiện số dòng chỉ có trong `CurrentExcelRowText` ở khu vực DataGrid, không phải cùng chỗ với trạng thái link).
TC4. **Cache + so `LastWriteTimeUtc`** — ✅ **đã làm xong (v0.060)**: `MainViewModel` lưu `_excelDataSourceWriteTimeUtc` sau mỗi import; `RefreshExcelDataAsync()` gọi `TryGetFileWriteTimeUtc()` (stat file, không đọc nội dung) so với giá trị đã lưu — nếu trùng thì bỏ qua đọc lại, báo `"Excel data already up to date (Data read at HH:mm:ss) — file has not changed since last read"`; nếu khác thì đọc lại như cũ. Test: `excel refresh skips unchanged file` (PASS) — kiểm cả 2 nhánh (file không đổi → skip; file đổi nội dung + write time → đọc lại).
TC5. ✅ **Không mất lựa chọn dòng khi refresh** (v0.064): panel Data Sources có ComboBox `Row Tracking Key` lấy từ header Excel; chọn cột sẽ lưu `KeyField` + giá trị dòng hiện tại vào `KeyValue`, để refresh tìm lại đúng bản ghi trước khi fallback theo index. Có entry rỗng để trở về theo dõi bằng vị trí dòng. Test `key field selection tracks row across refresh` chèn một dòng phía trên bản ghi đã chọn rồi refresh, xác nhận vẫn giữ đúng `PartNo`.
TC6. ✅ **Nhật ký thao tác dữ liệu** (v0.061): `DataOperationLogService` (mới, `src/ANLAbel.Data/DataLogs/`) ghi 1 dòng JSON (`.jsonl`) mỗi lần import/refresh/relink/mở-lại-link vào `%LocalAppData%\ANLAbel\logs\data-operations.jsonl`: thời điểm, `Operation` (Import/Refresh/Relink/Open), đường dẫn Excel, sheet, số dòng, số cột, thành công/thất bại + thông điệp lỗi. Ghi kiểu fire-and-forget (`Task.Run`, không await) và nuốt mọi `IOException`/`UnauthorizedAccessException` — lỗi ghi log không bao giờ làm hỏng thao tác dữ liệu chính, đúng tinh thần "best-effort trace". Nối vào `MainViewModel.ImportExcelAsync` (điểm hội tụ chung của cả 4 luồng) qua overload private nhận `operation` label. Test: `data operation log records import success and failure` — kiểm cả nhánh thành công (ghi đúng RowCount) và nhánh thất bại (sheet không tồn tại → vẫn ném exception như cũ cho UI xử lý, đồng thời ghi log lỗi).
TC7. ✅ **Test bao phủ các ca hỏng** (v0.062): thêm `ExcelDataReadException` có mã `MissingFile`, `MissingSheet`, `InvalidWorkbook`, `InvalidHeaderRow` để UI nhận message có hành động rõ ràng. Bộ xUnit `ExcelDataServiceReliabilityTests` khóa 6 ca: file mất; sheet bị đổi tên (message liệt kê sheet hiện có); `.xlsx` hỏng; header trùng/rỗng + whitespace; file đang mở với `FileShare.ReadWrite`; header row vượt vùng dữ liệu. Các lỗi cancel/round-trip/log đã được khóa ở test trước. Phần "file biến mất đúng giữa lúc ClosedXML đang parse" không thể mô phỏng ổn định, nhưng đường mở file và exception vẫn đi qua `MissingFile`/log thất bại.

### Giai đoạn 3 — Mở rộng nguồn và hiệu năng

7. **CSV** (UTF-8, chọn delimiter) qua cùng interface `IDataSourceReader` — `ExcelDataService` hiện tại trở thành 1 implementation. CHƯA LÀM (chưa có yêu cầu cụ thể; là thay đổi kiến trúc lớn hơn — đổi tên/khái quát hoá luồng `ImportExcelAsync` khắp `MainViewModel`/`ExcelImportWindow` — nên để dành khi có nhu cầu thật thay vì đoán trước).
8. ✅ **Chọn dòng header + preview trước khi import** (v0.079): `ExcelDataService.PreviewRowsAsync(filePath, sheetName, maxRows, ct)` (method mới, không sửa method đọc dữ liệu chính sẵn có) đọc tối đa N dòng vật lý đầu tiên kèm đúng số dòng tuyệt đối trong sheet (`ExcelPreviewRow.RowNumber` — khớp trực tiếp với `DatabaseConfig.HeaderRowIndex`/tham số `headerRowIndex` của `LoadSheetAsync`, kể cả khi dữ liệu không bắt đầu từ dòng 1). Cửa sổ mới `ExcelHeaderRowWindow` hiện bảng preview cho người dùng click chọn đúng dòng chứa tên cột, nối vào `ExcelImportWindow.Browse_Click` (sau khi chọn sheet, trước khi Import) — preview lỗi thì fallback im lặng về dòng 1 như hành vi cũ, không chặn import. Test: `excel preview rows use absolute row numbers and respect maxRows`.
9. **Đọc lười (lazy) cho file lớn**: nếu > ~20.000 dòng, chỉ nạp trang đang xem cho DataGrid; print pipeline stream theo batch thay vì giữ cả DataTable string. CHƯA LÀM — chưa có bằng chứng thực tế (file khách hàng lớn cỡ nào) để thiết kế đúng ngưỡng/chiến lược cache mà không đoán mò; rủi ro sửa sai cao hơn lợi ích nếu làm khi chưa có ca thật.
10. (Tuỳ nhu cầu khách) ODBC/SQL Server/Google Sheets — chỉ làm khi có yêu cầu thật, giữ qua cùng interface. CHƯA LÀM, đúng như định hướng ban đầu.

## Ràng buộc phải giữ

- Tương thích ngược: mọi file `.anlabel` cũ (chỉ có `FilePath` tuyệt đối) phải mở được y như trước.
- Template thư viện luôn standalone (không link Excel, không ship file dữ liệu) — mọi thay đổi không được phá test `template library standalone (no sample-data link)`.
- Mọi thao tác đọc file phải chạy nền (rule 7 trong `agent.md`), có wait indicator, bắt riêng `IOException` file đang bị Excel khoá.
- Mỗi giai đoạn xong phải: build PASS, test PASS, bump version hiển thị, cập nhật `PLAN.md`/`MASTER_PLAN.md`.

## Thứ tự làm đề xuất

Giai đoạn 1 ✅ xong. Giai đoạn TC ✅ xong (TC1–TC7). Giai đoạn 2 ✅ xong toàn bộ (item 4 Data Source Registry UI, item 5 watcher, item 6 KeyField UI). Việc còn lại chỉ còn Giai đoạn 3 — làm theo nhu cầu thực tế (CSV, header-row picker, lazy-load file lớn, ODBC/SQL). Mỗi đợt: build + test PASS, bump version, cập nhật `MASTER_PLAN.md`.
