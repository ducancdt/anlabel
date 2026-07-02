# Plan: Quản lý dữ liệu database/Excel đầu vào

**Ngày:** 2026-07-02 · **Trạng thái:** Giai đoạn 1 hoàn tất; Giai đoạn TC: TC1/TC2/TC3/TC4/TC6 xong, còn TC5 (UI KeyField) + TC7 (test thêm ca hỏng)

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

4. **Tách `DataSource` thành đối tượng quản lý riêng** (registry theo máy, lưu `%AppData%\ANLAbel\data-sources.json`): mỗi source có Id, tên hiển thị, đường dẫn, sheet, header row. Template tham chiếu `DataSourceId` (giữ fallback đường dẫn cũ để tương thích). Panel `Data Sources` bên trái nâng cấp thành danh sách source thật sự: thêm/sửa/xoá/re-link một chỗ, mọi template dùng chung hưởng theo.
   - ✅ Đã có phần nền: model `DataSource`, `DataSourceRegistry` CRUD + JSON round-trip, `DatabaseConfig.DataSourceId` và test registry.
   - ⏳ Chưa nối registry vào `MainViewModel`/panel Data Sources; template hiện vẫn chạy theo `FilePath` cũ.
5. **Theo dõi file thay đổi**: `FileSystemWatcher` trên file Excel đang link (debounce ~1s); khi file đổi, hiện badge "Dữ liệu đã thay đổi — bấm Update" (không tự reload ngầm để tránh giật preview giữa chừng khi đang thiết kế/in).
6. **Khoá theo `KeyField`**: cho phép chọn 1 cột làm khoá trong dialog import; `LastSelectedRow` lưu thêm giá trị khoá, khi reload thì tìm lại đúng bản ghi theo khoá trước, theo index sau, lệch thì cảnh báo.
   - ✅ Backend đã lưu `KeyValue` và restore theo key trước, index sau.
   - ⏳ Chưa có UI chọn `KeyField` trong dialog import.
6b. ✅ **Cancel/timeout khi đọc file** (điểm yếu 8): `CancellationToken` cho `GetSheetNames`/`LoadSheetAsync`, nút Cancel trên UI, timeout 30s cho đường dẫn UNC/network và mở stream bằng `FileShare.ReadWrite`. Luồng re-link cũng dùng API async, không đọc workbook trên UI thread.
6c. **Báo cáo schema sau import/refresh** (điểm yếu 9): gom mọi `BindingExpression`/formula trong template → danh sách cột cần có → so với header thực tế → hiện tóm tắt cột thiếu một chỗ (status bar + dialog khi mở template). Cache kết quả đọc theo `(path, sheet, LastWriteTimeUtc)` để refresh không đọc lại file chưa đổi.

### Giai đoạn TC — Siết tin cậy database (ưu tiên mới 2026-07-02, làm trước GĐ3)

Mục tiêu: mọi con đường dữ liệu đi vào tem đều **đoán được và kiểm chứng được**. Các hạng mục:

TC1. **Báo cáo schema một chỗ** — ⚠️ **đã có sẵn phần lớn khi audit lại code** (không phải làm từ đầu): `MainViewModel.GetBindingIssues()` + panel "Binding Issues" (`MainWindow.xaml` dòng ~638) đã liệt kê từng object có `BindingExpression` bị thiếu cột/lỗi, tự refresh sau `RaiseDatabaseFieldStateChanged()` (gọi sau Import/Refresh/thêm-xoá field). ✅ 2026-07-02 (v0.060) bổ sung thêm: `StatusText` sau `ImportExcelAsync` giờ nối thêm số lượng object có vấn đề binding, vd. `"Imported 40 rows from data.xlsx / Sheet1 — 2 object(s) have missing/broken bindings"` — để người dùng thấy ngay không cần mở panel. Còn thiếu (chưa làm): chặn/cảnh báo cứng lúc **in** khi thiếu cột (nối với preflight, xem `docs/print-preview-reliability-plan.md`).
TC2. ✅ **Kiểm chứng round-trip khi save/open** (v0.061): test `database config full round trip` — populate đầy đủ `DatabaseConfig` (DataSourceId, FilePath, RelativePath, SheetName, HeaderRowIndex, KeyField, KeyValue, LastSelectedRow, AvailableFields, LabelFields) qua `ProjectFileService.SaveAsync`/`LoadAsync`, assert từng field giữ nguyên sau round-trip JSON.
TC3. **Trạng thái link hiển thị tường minh** — ⚠️ **đã có sẵn phần lớn**: panel Database (`MainWindow.xaml` ~dòng 606-609) đã hiển thị `LinkedExcelSourceText` (file/sheet), `ExcelLinkStatusText` (cảnh báo đỏ khi hỏng) + nút `RelinkExcelCommand`. ✅ 2026-07-02 (v0.060) bổ sung `ExcelDataFreshnessText` ("Data read at HH:mm:ss") ngay dưới tên file/sheet — phần còn thiếu duy nhất là hiển thị số dòng ngay tại đây (hiện số dòng chỉ có trong `CurrentExcelRowText` ở khu vực DataGrid, không phải cùng chỗ với trạng thái link).
TC4. **Cache + so `LastWriteTimeUtc`** — ✅ **đã làm xong (v0.060)**: `MainViewModel` lưu `_excelDataSourceWriteTimeUtc` sau mỗi import; `RefreshExcelDataAsync()` gọi `TryGetFileWriteTimeUtc()` (stat file, không đọc nội dung) so với giá trị đã lưu — nếu trùng thì bỏ qua đọc lại, báo `"Excel data already up to date (Data read at HH:mm:ss) — file has not changed since last read"`; nếu khác thì đọc lại như cũ. Test: `excel refresh skips unchanged file` (PASS) — kiểm cả 2 nhánh (file không đổi → skip; file đổi nội dung + write time → đọc lại).
TC5. **Không mất lựa chọn dòng khi refresh**: giữ đúng bản ghi theo `KeyField`/`KeyValue` (backend đã có, `FindRowIndexByKeyField`), thêm UI chọn KeyField + cảnh báo khi key không còn tồn tại sau refresh (backend đã cảnh báo qua `StatusText`, còn thiếu UI chọn field). **Còn thiếu UI chọn KeyField.**
TC6. ✅ **Nhật ký thao tác dữ liệu** (v0.061): `DataOperationLogService` (mới, `src/ANLAbel.Data/DataLogs/`) ghi 1 dòng JSON (`.jsonl`) mỗi lần import/refresh/relink/mở-lại-link vào `%LocalAppData%\ANLAbel\logs\data-operations.jsonl`: thời điểm, `Operation` (Import/Refresh/Relink/Open), đường dẫn Excel, sheet, số dòng, số cột, thành công/thất bại + thông điệp lỗi. Ghi kiểu fire-and-forget (`Task.Run`, không await) và nuốt mọi `IOException`/`UnauthorizedAccessException` — lỗi ghi log không bao giờ làm hỏng thao tác dữ liệu chính, đúng tinh thần "best-effort trace". Nối vào `MainViewModel.ImportExcelAsync` (điểm hội tụ chung của cả 4 luồng) qua overload private nhận `operation` label. Test: `data operation log records import success and failure` — kiểm cả nhánh thành công (ghi đúng RowCount) và nhánh thất bại (sheet không tồn tại → vẫn ném exception như cũ cho UI xử lý, đồng thời ghi log lỗi).
TC7. ✅ **Test bao phủ các ca hỏng** (v0.062): thêm `ExcelDataReadException` có mã `MissingFile`, `MissingSheet`, `InvalidWorkbook`, `InvalidHeaderRow` để UI nhận message có hành động rõ ràng. Bộ xUnit `ExcelDataServiceReliabilityTests` khóa 6 ca: file mất; sheet bị đổi tên (message liệt kê sheet hiện có); `.xlsx` hỏng; header trùng/rỗng + whitespace; file đang mở với `FileShare.ReadWrite`; header row vượt vùng dữ liệu. Các lỗi cancel/round-trip/log đã được khóa ở test trước. Phần "file biến mất đúng giữa lúc ClosedXML đang parse" không thể mô phỏng ổn định, nhưng đường mở file và exception vẫn đi qua `MissingFile`/log thất bại.

### Giai đoạn 3 — Mở rộng nguồn và hiệu năng

7. **CSV** (UTF-8, chọn delimiter) qua cùng interface `IDataSourceReader` — `ExcelDataService` hiện tại trở thành 1 implementation.
8. **Chọn dòng header + preview 50 dòng đầu trước khi import** (dialog import thêm bảng preview và ô "Header row").
9. **Đọc lười (lazy) cho file lớn**: nếu > ~20.000 dòng, chỉ nạp trang đang xem cho DataGrid; print pipeline stream theo batch thay vì giữ cả DataTable string.
10. (Tuỳ nhu cầu khách) ODBC/SQL Server/Google Sheets — chỉ làm khi có yêu cầu thật, giữ qua cùng interface.

## Ràng buộc phải giữ

- Tương thích ngược: mọi file `.anlabel` cũ (chỉ có `FilePath` tuyệt đối) phải mở được y như trước.
- Template thư viện luôn standalone (không link Excel, không ship file dữ liệu) — mọi thay đổi không được phá test `template library standalone (no sample-data link)`.
- Mọi thao tác đọc file phải chạy nền (rule 7 trong `agent.md`), có wait indicator, bắt riêng `IOException` file đang bị Excel khoá.
- Mỗi giai đoạn xong phải: build PASS, test PASS, bump version hiển thị, cập nhật `PLAN.md`/`MASTER_PLAN.md`.

## Thứ tự làm đề xuất

Giai đoạn 1 ✅ xong. Thứ tự tiếp theo (định hướng 2026-07-02): **Giai đoạn TC (siết tin cậy) → hoàn thiện GĐ2 (nối registry vào UI, watcher, KeyField UI) → GĐ3 theo nhu cầu thực tế.** Mỗi đợt: build + test PASS, bump version, cập nhật `MASTER_PLAN.md`.
