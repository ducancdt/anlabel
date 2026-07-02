# Plan: Quản lý dữ liệu database/Excel đầu vào

**Ngày:** 2026-07-02 · **Trạng thái:** Giai đoạn 1 hoàn tất; Giai đoạn 2 đang làm nền

## Hiện trạng (đã audit)

Luồng dữ liệu hiện tại của một template `.anlabel`:

- `DatabaseConfig` trong file template lưu: `FilePath` (đường dẫn **tuyệt đối** tới file Excel), `SheetName`, `HeaderRowIndex`, `LastSelectedRow`, `AvailableFields` (mọi cột đọc được), `LabelFields` (cột được phép dùng trên tem).
- Import qua dialog `Import Excel` (`ExcelImportWindow`) → `ExcelDataService` (ClosedXML) đọc header dòng 1 + toàn bộ rows thành chuỗi.
- Mở lại template: nếu file Excel còn tồn tại đúng đường dẫn thì tự restore data + row cuối đã chọn; mất file thì báo link hỏng nhưng template vẫn mở được.
- Binding `{Field}` / `FIELD("...")` có cơ chế match mềm (bỏ khoảng trắng/gạch/hoa-thường) và tự repair khi header đổi nhẹ.
- Template thư viện đóng gói sẵn dùng file nhúng `sample-data.xlsx`, giải nén ra `%LocalAppData%\ANLAbel\` và tự vá `FilePath` lúc Materialize.

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

### Giai đoạn 3 — Mở rộng nguồn và hiệu năng

7. **CSV** (UTF-8, chọn delimiter) qua cùng interface `IDataSourceReader` — `ExcelDataService` hiện tại trở thành 1 implementation.
8. **Chọn dòng header + preview 50 dòng đầu trước khi import** (dialog import thêm bảng preview và ô "Header row").
9. **Đọc lười (lazy) cho file lớn**: nếu > ~20.000 dòng, chỉ nạp trang đang xem cho DataGrid; print pipeline stream theo batch thay vì giữ cả DataTable string.
10. (Tuỳ nhu cầu khách) ODBC/SQL Server/Google Sheets — chỉ làm khi có yêu cầu thật, giữ qua cùng interface.

## Ràng buộc phải giữ

- Tương thích ngược: mọi file `.anlabel` cũ (chỉ có `FilePath` tuyệt đối) phải mở được y như trước.
- Không phá cơ chế template thư viện (`sample-data.xlsx` nhúng + vá path lúc Materialize) — Giai đoạn 1 phải xử lý trường hợp này như một source đặc biệt.
- Mọi thao tác đọc file phải chạy nền (rule 7 trong `agent.md`), có wait indicator, bắt riêng `IOException` file đang bị Excel khoá.
- Mỗi giai đoạn xong phải: build PASS, test PASS, bump version hiển thị, cập nhật `PLAN.md`/`MASTER_PLAN.md`.

## Thứ tự làm đề xuất

Giai đoạn 1 (mục 1-3) làm trước — giải quyết trực tiếp nỗi đau hiện tại, chạm ít code. Giai đoạn 2 làm khi người dùng xác nhận cần dùng chung source giữa nhiều template. Giai đoạn 3 theo nhu cầu thực tế.
