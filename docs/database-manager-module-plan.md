# Plan: Module quản lý Database riêng (Database Manager)

**Ngày:** 2026-07-03 · **Trạng thái:** M1 + M2 + M3 + M4-copies-per-record đã xong (v0.080). Chỉ còn M4-filter/sort (tùy chọn) và Giai đoạn 3 phần CSV/lazy-load/ODBC (theo nhu cầu thực tế).
**Yêu cầu gốc từ chủ dự án:** "tạo thành 1 module riêng quản lý phần database này, bao gồm việc xóa, gỡ database, quản lý database" — sau khi audit phần gắn link Excel hiện tại và nghiên cứu cách NiceLabel làm.

> **Current continuation (2026-08-13):** M1–M4 và các version ở trên là checkpoint lịch sử; phần UI click-through vẫn cần người dùng smoke-test theo các lưu ý trong từng đợt. Reconciliation/ownership hiện tại nằm ở [continuation handoff](reinvention/10-continuation-handoff-2026-08-13.md). Metadata read-only của panels file, Page `0:1`, hiện chỉ liệt kê các frame `1:2`, `4:2`, `8:2`, `13:2`, `18:69`, `22:82`; chưa có node Database Manager riêng. Handoff state matrix và routing reuse-vs-new-reference hiện nằm ở [Database Manager UI handoff](DATABASE_MANAGER_UI_HANDOFF.md). Cross-surface source/read-model, Manager/Workspace ownership và runtime gate hiện được gom trong [R4 data surfaces owner decision packet](R4_DATA_SURFACES_OWNER_DECISION_PACKET.md). Nếu mở slice UI cho Manager, phải ghi node/state mới hoặc lý do dùng reference hiện có trong [Figma → WPF handoff template](figma-ui-handoff-template.md), rồi đóng bằng runtime evidence.

> **Concrete Manager/Cleanup owner gate (2026-08-13):** [`DATABASE_MANAGER_UI_DECISION_PACKET.md`](DATABASE_MANAGER_UI_DECISION_PACKET.md) records the existing WPF list/detail and cleanup states, shared-source persistence/fallback boundary, async request gap and read-only Figma reuse. It does not authorize a Manager redesign, registry rewrite or Text/TextBox change.

> Đọc kèm: `docs/database-plan.md` (nền tảng đã xong: registry, watcher, key field, log, relink). Plan này KHÔNG làm lại các phần đó — chỉ gom chúng về một chỗ và bổ sung phần còn thiếu.

---

## 1. Audit hiện trạng (2026-07-03, v0.074)

### Đã có (KHÔNG làm lại)

| Chức năng | Ở đâu |
|---|---|
| Import Excel (chọn file/sheet, cancel được) | `ExcelImportWindow` + `MainViewModel.ImportExcelAsync` |
| Refresh / Update Excel (skip nếu file chưa đổi) | `RefreshExcelDataCommand` |
| Relink khi link hỏng (per-template) | `RelinkExcelAsync` + nút trong panel Database |
| Shared Data Sources (registry `%AppData%\ANLAbel\data-sources.json`) | `DataSourceRegistry`, thẻ "Shared Data Sources" trong panel trái |
| Use / Relink / Remove shared source | `UseDataSourceAsync` / `RelinkDataSourceAsync` / `RemoveDataSource` (MainViewModel ~dòng 1180-1263) |
| Watcher báo dữ liệu cũ + Row Tracking Key | `OnLinkedExcelFileChanged`, `KeyField`/`KeyValue` |
| Nhật ký thao tác dữ liệu | `DataOperationLogService` (`.jsonl`) |

### Còn thiếu (phạm vi plan này)

1. **KHÔNG có cách "gỡ database" khỏi template** (unlink): một khi đã Import Excel, không có nút nào xóa `DatabaseConfig` (FilePath/SheetName/fields/rows trong RAM) để template quay về standalone. Người dùng muốn bỏ link phải sửa tay file `.anlabel`.
2. **Quản lý rải rác 3 chỗ**: ribbon (Import/Update Excel), panel cây Database, thẻ Shared Data Sources — không có một cửa sổ trung tâm kiểu NiceLabel Dynamic Data Manager.
3. **Xóa shared source không có xác nhận** và không cho biết có bao nhiêu template đang tham chiếu `DataSourceId` đó — xóa nhầm là mọi template dùng chung mất fallback registry (vẫn còn FilePath riêng, nhưng người dùng không được cảnh báo).
4. **Không có "Test connection"**: không kiểm tra nhanh được source còn đọc được không (file tồn tại? sheet còn? header hợp lệ?) mà phải Use thử.
5. **Không có preview dữ liệu** trước khi Use một shared source.
6. **Không sửa được sheet/header-row của shared source** sau khi tạo (chỉ đổi được Name và Relink file).

## 2. Tham khảo NiceLabel (nghiên cứu 2026-07-03)

NiceLabel Desktop Designer quản lý database qua **Dynamic Data Manager** — một dialog trung tâm tách khỏi canvas:

- Ribbon riêng trong dialog: **Database Connections** (thêm kết nối mới theo loại — Excel/Access/SQL...), **Delete/Copy/Cut/Paste** data source — mọi thao tác xóa/sao chép data source đều làm trong dialog này, không rải rác.
- **Database Wizard** từng bước: chọn file → chọn table/sheet → chọn **Fields** (available/selected) → **Filter** (nhóm điều kiện) → **Sorting** (nhiều cột asc/desc) → **Data Preview** → **Label copies per record** (số tem in mỗi bản ghi, có thể lấy từ một cột).
- **Test Connection**: nút kiểm tra kết nối đọc được ngay trong properties của connection.
- Khi in: print dialog hiện toàn bộ record của database, tick chọn record cần in (ANLAbel đã có tương đương trong `PrintPreviewWindow`).

Điểm rút ra cho ANLAbel (app nhỏ hơn, chỉ Excel): cần **một cửa sổ trung tâm** + **wizard/flow rõ ràng** + **xóa/gỡ có kiểm soát**; KHÔNG cần bê nguyên độ phức tạp OLE DB/multi-database của NiceLabel.

Nguồn: [Dynamic Data Manager](https://help.nicelabel.com/hc/en-001/articles/4406559638289-Dynamic-Data-Manager), [Databases](https://help.nicelabel.com/hc/en-001/articles/4402152654865-Databases), [Printing from Databases](https://help.nicelabel.com/hc/en-001/articles/4402145584913-Printing-from-Databases), [Excel OneDrive connection (Loftware)](https://help.loftware.com/cloud/Designer/Dynamic-Data-Sources/Databases/Manual/Excel-Microsoft-OneDrive.html).

## 3. Thiết kế đề xuất

Một cửa sổ WPF mới **`DatabaseManagerWindow`** (mở từ ribbon, nhóm Data — nút "Database Manager") + lệnh **Unlink** độc lập. Chia 4 đợt, mỗi đợt tự đứng được (build + test + bump version + smoke test theo `agent.md` rule 1-3).

### Đợt M1 — Gỡ database khỏi template (Unlink) — ưu tiên cao nhất, ít rủi ro — ✅ ĐÃ XONG (v0.075)

1. ✅ `MainViewModel.UnlinkExcel()` (public, không có dialog bên trong — dialog xác nhận nằm ở code-behind `MainWindow.xaml.cs` `UnlinkExcel_Click`, đúng quy ước "MessageBox chỉ nằm ở code-behind" đã thấy ở các handler Save/Open/Template Library/Print Preview khác trong file này — giữ ViewModel gọi được trực tiếp từ test tự động mà không treo chờ click chuột thật).
   - Thực hiện: `StopWatchingExcelFile()`; `ExcelDataView=null`, `ExcelHeaders.Clear()`, `SelectedDataItem=null`, xóa `SelectedAvailableDatabaseField`/`SelectedLabelDatabaseField`/`SelectedExcelField`; **reset toàn bộ `Template.DatabaseConfig = new DatabaseConfig()`** (không giữ `LabelFields`/`AvailableFields` — quyết định cuối cùng theo đúng phương án ban đầu, để tránh trạng thái nửa vời); `BindingExpression` trên từng `LabelObject` KHÔNG bị đụng tới (rule 9 `agent.md` tôn trọng — không chạm geometry, và ở đây không chạm cả `BindingExpression`).
   - Cập nhật `IsExcelLinkBroken=false`, `IsExcelDataStale=false`, mọi property phụ thuộc (`HasLinkedExcelSource`, `LinkedExcelSourceText`, `ExcelLinkStatusText`, `ExcelDataFreshnessText`, `CurrentExcelRowText`, `KeyFieldOptions`, `SelectedKeyFieldName`), gọi `RaiseDatabaseFieldStateChanged()`, `StatusText` báo rõ, ghi `DataOperationLogService` với `Operation="Unlink"`.
2. ✅ UI: nút "⛔ Unlink Excel" trong panel Database (`MainWindow.xaml`, cạnh nút Update Excel), `Click="UnlinkExcel_Click"` (không dùng `Command` binding vì cần hỏi xác nhận trước khi gọi ViewModel), chỉ hiện khi `HasLinkedExcelSource` (kể cả khi link đang hỏng — unlink là cách thoát khỏi link hỏng không cần tìm lại file cũ).
3. ✅ Test (`ANLAbel.Tests/Program.cs`): `unlink excel clears database config but keeps bindings` (import Excel, bind 1 object vào `PartNo`, unlink → assert `DatabaseConfig.FilePath`/`SheetName` rỗng, `HasExcelData=false`, `LabelDatabaseFields.Count=0`, object vẫn giữ nguyên `BindingExpression`, re-import cùng schema chạy tiếp ngay không cần rebind) và `unlink excel works when link is broken` (mở template có link hỏng do xóa file Excel, unlink → hết báo đỏ, `HasLinkedExcelSource=false`). 41/41 test PASS.
4. Build PASS, `ANLAbel.Tests` 39/39 → 41/41 PASS, `ANLAbel.UnitTests` 45/45 PASS, smoke app PASS (khởi động sạch, tắt sạch). Version `0.074` → `0.075`.

### Đợt M2 — Cửa sổ Database Manager (trung tâm quản lý) — ✅ ĐÃ XONG (v0.076)

1. ✅ File mới `src/ANLAbel.App/DatabaseManagerWindow.xaml(.cs)`, mở từ nút "Manage Data Sources..." trong thẻ Shared Data Sources (panel Database bên trái) qua `MainWindow.xaml.cs` `ManageDataSources_Click` (`ShowDialog()`, `Owner=this`). Không thêm nút ribbon riêng (không cần thiết — thẻ Shared Data Sources đã ở vị trí người dùng quen thao tác Data).
2. ✅ Bố cục 2 cột: **Trái** — `ListBox` mọi `DataSource` trong `MainViewModel.DataSources` (`DisplayMemberPath=DisplayName`) + nút "+ Save current template's Excel link" (tái dùng `AddCurrentAsDataSourceCommand`). **Phải** — chi tiết source đang chọn: Name (TextBox, LostFocus persist), FilePath (readonly + nút Relink... tái dùng `RelinkDataSourceCommand`), Sheet (ComboBox editable + nút "Load sheets" gọi `ExcelDataService.GetSheetNamesAsync`), Header row (TextBox số, LostFocus parse+persist), nút **Test Connection**, nút **Preview data...** (DataGrid readonly hiện dưới, dùng `LoadSheetAsync` có sẵn — không viết parser riêng), nhãn "Used by the currently open template: Yes/No" (tự cập nhật qua `MainViewModel.PropertyChanged` lắng nghe `HasLinkedExcelSource`), nút "Use for current template" (tái dùng `UseDataSourceCommand`) và "Remove..." (xem mục 4). Không có mục ảo "(This template's own link)" trong danh sách trái như phác thảo ban đầu — không cần thiết vì nút "+ Save current template's Excel link" đã đủ để đưa link hiện tại vào registry trước khi quản lý.
3. ✅ `ExcelDataService.TestConnectionAsync(path, sheet, headerRow, ct)` (method **THÊM MỚI**, không sửa method sẵn có nào trong file — tôn trọng ranh giới vùng của agent khác) trả `(bool Ok, string Message)`, gọi lại `LoadSheetAsync` sẵn có (đã có cancel + timeout mạng + `FileShare.ReadWrite` — không viết lại logic đọc file), bắt mọi exception (trừ `OperationCanceledException`) thành `(false, message)` thay vì ném ra ngoài.
4. ✅ **Remove có xác nhận**: `Remove_Click` trong code-behind hỏi `MessageBox` nêu rõ template hiện tại có đang dùng source này không (so `Template.DatabaseConfig.DataSourceId`), rồi mới gọi `RemoveDataSourceCommand`. Không quét toàn ổ đĩa tìm template khác tham chiếu (không khả thi) — đúng như plan gốc.
5. ✅ Mọi ghi registry đi qua `DataSourceRegistry`/`PersistDataSources()` sẵn có; mọi đọc Excel đi qua `ExcelDataService` async có wait cursor (rule 7 `agent.md`).
6. ✅ Thẻ "Shared Data Sources" trong panel trái (`MainWindow.xaml`) đã **thu gọn**: chỉ còn danh sách tên nguồn readonly (`TextBlock` × N) + nút "Manage Data Sources...". Đã **xóa hẳn** `TextBox` inline-edit Name + 3 nút Use/Relink/Remove cũ + handler `DataSourceNameTextBox_LostFocus` trong `MainWindow.xaml.cs` — không còn 2 nơi cùng sửa 1 thứ.
7. ✅ Test: `test connection reports ok, missing sheet, and missing file` (3 nhánh trên file `.xlsx` thật tạo trong test). Việc "sửa sheet/header của shared source rồi template dùng chung tự reload" đã được bảo vệ gián tiếp bởi test có sẵn `shared data source relink fixes every referencing template` (cùng cơ chế `DataSourceRegistry.Upsert` + `UseDataSourceAsync`) — không cần test trùng lặp.
8. **Lưu ý cho agent sau**: luồng click-through đầy đủ trong cửa sổ `DatabaseManagerWindow` (chọn source → sửa sheet → Test Connection → Preview → Use/Remove) **mới được verify bằng build + test tự động + smoke test khởi động app, CHƯA được bấm thử bằng tay** (không có công cụ UI automation cho WPF desktop trong môi trường này). Đề nghị người dùng tự mở "Manage Data Sources..." và thử qua 1 lượt trước khi coi M2 là hoàn toàn xong.

### Đợt M3 — Usage tracking + dọn dữ liệu — ✅ ĐÃ XONG (v0.077)

1. ✅ **"Recently used by"**: `DataSource` (`src/ANLAbel.Core/Models/DataSource.cs`) thêm `LastUsedUtc` (`DateTime?`) và `RecentTemplates` (`List<string>`, tối đa 10, mới nhất trước, khử trùng lặp theo đường dẫn). `MainViewModel.RecordDataSourceUsage(DataSource source)` (helper mới) cập nhật 2 trường này rồi `Upsert`+`Save` registry; gọi tại `UseDataSourceAsync` (sau `ImportExcelAsync` thành công) và `RestoreLinkedExcelDataAsync` (khi mở template có `DataSourceId` trỏ tới source còn tồn tại, sau import thành công). Bỏ qua ghi `RecentTemplates` nếu template chưa được lưu (`CurrentFilePath` rỗng) — không có đường dẫn để ghi.
   - Backward/forward compat: 2 trường mới có default an toàn (`null`/`new List<string>()`), registry JSON cũ (ghi trước khi có 2 trường này) load bình thường không cần migrate.
2. ✅ **Xóa dữ liệu mồ côi**: nút "Clean up..." trong `DatabaseManagerWindow` (panel trái, dưới danh sách source) → tính danh sách source có `!File.Exists(FilePath)` VÀ (`LastUsedUtc == null` HOẶC cũ hơn 30 ngày) → nếu có, mở `DataSourceCleanupWindow` (file mới) liệt kê từng source kèm checkbox + dòng phụ "last used yyyy-MM-dd" hoặc "never used" → nút "Remove Selected" hỏi xác nhận 1 lần cho cả lô rồi gọi `RemoveDataSourceCommand` cho từng mục đã tick. Nếu không có source nào mồ côi, hiện thông báo thay vì mở cửa sổ trống.
3. ✅ Test: `data source records recent template usage` (dùng shared source 2 lần từ cùng 1 template → `LastUsedUtc` được set, `RecentTemplates` chỉ có 1 mục không trùng lặp, sống sót qua save/reload registry), `registry with unknown extra fields still loads` (JSON viết tay thiếu 2 trường mới → load không lỗi, default `null`/rỗng, re-save không hỏng file).
4. Build PASS, `ANLAbel.Tests` 42/42 → 44/44 PASS, `ANLAbel.UnitTests` 45/45 PASS, smoke app PASS. Version `0.076` → `0.077`.
5. **Lưu ý cho agent sau (giống M2)**: nút "Clean up..." → cửa sổ `DataSourceCleanupWindow` → "Remove Selected" mới verify được bằng build+test+smoke, chưa bấm thử bằng tay.

### Đợt M4 — (Tùy chọn, học NiceLabel, làm khi có nhu cầu thật)

- ✅ **Copies per record từ cột Excel** (v0.080): `DatabaseConfig.CopiesField` (string, tên cột) + `DatabaseConfig.ResolveCopiesForRow(copiesField, row)` (method tĩnh, thuần — parse int cột đó cho 1 dòng, `<0`/không parse được/thiếu cột → mặc định 1, chặn trần 999). `PrintPreviewWindow.RefreshPreview()` gọi hàm này để đặt `TrackingRowViewModel.Copies` ban đầu cho từng dòng thay vì luôn cứng `1` — người dùng vẫn sửa tay được từng dòng trong danh sách tracking như trước, cột Excel chỉ set giá trị khởi tạo. UI chọn cột: ComboBox "Copies Per Record" mới trong panel Database (`MainWindow.xaml`, cạnh "Row Tracking Key"), dùng chung `KeyFieldOptions` cho danh sách cột, bind `SelectedCopiesFieldName` (property mới trong `MainViewModel`, cùng khuôn mẫu `SelectedKeyFieldName`). Test: `copies-per-record resolves from Excel column, defaults to 1` (8 nhánh: không cấu hình, row null, giá trị hợp lệ, thiếu cột, rỗng, âm, không phải số, quá lớn bị chặn trần) + mở rộng `database config full round trip` với `CopiesField`.
- **Filter/Sort record** trong Manager trước khi in (NiceLabel Filter/Sorting tab) — CHƯA LÀM, chỉ làm nếu khách cần; `PrintPreviewWindow` đã cho tick chọn dòng nên nhu cầu thấp.
- CSV/ODBC: đã nằm ở `database-plan.md` GĐ3, không lặp lại ở đây, vẫn CHƯA LÀM (theo nhu cầu thực tế).

## 4. Ràng buộc bắt buộc (cho agent thực hiện)

- **Tương thích ngược tuyệt đối**: `.anlabel` cũ và `data-sources.json` cũ phải mở được y nguyên. `DatabaseConfig` không đổi schema trong M1/M2 (M3 chỉ thêm trường vào `DataSource` với default).
- **Không đụng vùng của agent khác**: `LabelDesignerCanvas.cs`, `ExcelDataService.cs` phần đọc workbook lõi (chỉ THÊM `TestConnectionAsync`, không sửa method sẵn có), `MmConverter.cs`, installer `.iss`, `DesignerPreferencesService*`, `PLAN.md`. Kiểm `git status` trước mỗi đợt.
- Rule 7 `agent.md`: mọi I/O Excel trong cửa sổ mới phải async + cancel được + bắt `IOException` file khóa.
- Rule 8: không được làm template thư viện tự link Excel trở lại; test `template library standalone (no sample-data link)` phải luôn PASS.
- Mỗi đợt: build `dotnet build ANLAbel.slnx` PASS, `ANLAbel.Tests` + `ANLAbel.UnitTests` PASS, bump version (csproj + `App.xaml.cs` + `MainWindow.xaml` title + `BuildChannelText`), smoke test app, cập nhật `MASTER_PLAN.md` + file plan này (tick ✅ từng mục), chạy `deploy-desktop.ps1` nếu người dùng đang cần bản cài mới.
- Không commit khi chưa được yêu cầu.

## 5. Thứ tự làm & định nghĩa "xong"

| Đợt | Nội dung | Xong khi |
|---|---|---|
| M1 | Unlink Excel khỏi template | 2 test mới PASS, nút hiện đúng điều kiện, log ghi `Unlink` |
| M2 | DatabaseManagerWindow + Test Connection + Remove có xác nhận + sửa sheet/header | 2 test mới PASS, panel trái thu gọn, không còn 2 nơi sửa trùng |
| M3 | Usage tracking + Clean up | 2 test mới PASS, registry cũ vẫn load |
| M4 | Copies-per-record/Filter (tùy chọn) | chỉ làm khi chủ dự án yêu cầu |
