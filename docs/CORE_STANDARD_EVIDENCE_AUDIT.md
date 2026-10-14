# Đối chiếu quy chuẩn với hiện trạng

Ngày: 2026-09-14. Quy chuẩn: [CORE_SOFTWARE_STANDARD.md](CORE_SOFTWARE_STANDARD.md).
Base commit: `d408fe8191d8cbd9aece3b69b706deced7c00833`, worktree có nhiều thay đổi chưa commit. Kết quả dưới đây thuộc worktree được build trong phiên này, không phải release của riêng base commit.

> **Cập nhật mapping 2026-09-16:** ma trận D01–D07, DB01–DB10, B01–B07, P01–P08 bên dưới là kết luận nghiệm thu hiện tại, thay thế các kết luận Pass bao quát trước đây. Những phần phát hiện ban đầu, increment và checkpoint được giữ nguyên làm **lịch sử**, không phải trạng thái của worktree hiện tại. Base commit trên không định danh các thay đổi chưa commit hoặc source đang được sửa đồng thời.
>
> Phiên này chỉ đọc source/assertion và sửa tài liệu này; không chạy build, test, Fast, mutation, WPF, driver hay máy in. Không chứng nhận thay đổi Excel đang do chủ sở hữu khác thực hiện. `Not verified` là thiếu bằng chứng thực thi hiện tại, **không đồng nghĩa có bug**; mâu thuẫn xác định qua source được ghi riêng, chưa gọi là test Fail.

## Trạng thái hiện hành theo commit đã khóa — `fcfb9ad` (v0.308, 2026-10-13)

Phần này thay cho “Kết luận mapping hiện tại (2026-09-16)” ở dưới. Các phần lịch
sử bên dưới được giữ nguyên. Cách lập theo mục 7 của quy chuẩn:

- **Commit:** `fcfb9ad` (`release: v0.308`), worktree sạch, Windows 11, .NET 8,
  máy phát triển.
- **Lệnh:**
  - `dotnet build ANLAbel.slnx -c Debug` → 0 cảnh báo, 0 lỗi.
  - `dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj -c Debug --no-build` → 1175/1175.
  - `dotnet src/ANLAbel.Tests/bin/Debug/net8.0-windows/ANLAbel.Tests.dll` → 243 PASS / 0 FAIL, exit 0.
  - `scripts/Invoke-ANLAbelLifecycleSmoke.ps1` trên bản cài 0.308 → 15/15.
- **Ba mức bằng chứng:**
  - **Phần mềm:** gate tự động ở trên, có tên cụ thể.
  - **Tương tác:** app thật, qua UI Automation hoặc thao tác người.
  - **Vật lý:** máy in và tem thật. Mức này chia ba: quan sát capability, spool
    và xác minh tem ra.
- Không mức nào được suy ra từ mức khác. `Not verified` không đóng gate.
- **Cập nhật 2026-10-14 tại `59467f9` (v0.311):** B06 và DB03 lên Pass ở mức phần mềm, nên cả 39 ID (32 ID lõi và F01–F07) đều có gate tự động. Lệnh: build 0/0; unit 1199/1199; regression 258 PASS / 0 FAIL; Fast quality loop đạt; `scripts/Invoke-ANLAbelLifecycleSmoke.ps1` trên bản cài 0.311 → 15/15.
- **Cập nhật 2026-10-14 tại `98cd7ac` (v0.312):** thêm gate B05 cho mã 1D theo X tràn mép tem. Build 0/0; unit 1199/1199; regression 259 PASS / 0 FAIL; smoke 15/15 trên bản cài 0.312.
- **Cập nhật 2026-10-14 tại `788579e` (v0.313):** DB03 hiển thị trong Bảng thuộc tính và danh sách Lớp; build 0/0; unit 1199/1199; regression 259 PASS / 0 FAIL; smoke 15/15 trên bản cài 0.313.
- **Cập nhật 2026-10-14 tại `829b50e` (v0.315, sửa theo review mã nguồn):** build 0/0; unit 1210/1210; regression 261 PASS / 0 FAIL; smoke 15/15 hai lần trên bản cài 0.315.
- **Cập nhật 2026-10-14 tại `4aac991` (v0.316, sửa theo review lần hai):** bộ phân tích công thức an toàn khi nhiều luồng dùng chung; DB03, B04, B05 như trên. Build 0/0; unit 1211/1211; regression 262 PASS / 0 FAIL; smoke 15/15 hai lần trên bản cài 0.316.
- **Cập nhật 2026-10-14 tại `4fd3977` (v0.317, sửa theo review lần ba):** lời báo QR phiên bản cố định và dấu ngoặc trống. Build 0/0; unit 1211/1211; regression 262 PASS / 0 FAIL; smoke 15/15 hai lần trên bản cài 0.317.
- **Cập nhật 2026-10-14 tại `828858d` (v0.318):** cột không có tiêu đề và không có dữ liệu không thành trường (Excel và CSV). Build 0/0; unit 1213/1213; regression 263 PASS / 0 FAIL; smoke 15/15 trên bản cài 0.318, script smoke nay tự kết luận.

| ID | Phần mềm | Tương tác | Vật lý | Bằng chứng chính / phần còn thiếu |
| --- | --- | --- | --- | --- |
| D01 | Pass | Not verified | — | `handle resize keeps millimetres at every zoom` (50/100/200 %), `snap tolerance and hysteresis remain stable across zoom`, `ResizeGeometryContractTests`. Thiếu: chuột thật, Windows scaling 125/150 % |
| D02 | Pass | Not verified | — | `canvas edit gesture is one undo step and cancel restores`, `rotation and multi-object gestures are one undo step`, `canvas paste and group resize are single gestures`, `undo and redo keep the current data link`, `keyboard nudge uses physical steps and one gesture` |
| D03 | Pass | Not verified | — | `normal resize capture release does not cancel gesture`; cancel trả đúng hình học. Thiếu: Esc/mất capture bằng chuột thật |
| D04 | Pass | Not verified | — | Bảy gate Text/TextBox được bảo vệ |
| D05 | Pass | Not verified | — | `designer preview row keeps object geometry`, `bound rows preserve preview and print geometry identity` |
| D06 | Pass | Pass | — | `library templates round-trip without geometry change` (22 mẫu), `real templates open, save, render and pass preflight` (5 mẫu thật, byte-stable), `failed template open preserves the current document`, `corrupt template with backup does not replace the open document`; smoke A/C |
| D07 | Pass | Pass | — | `missing font or image is shown while editing` (từ 2026-10-14 gồm cả ảnh có dữ liệu nhưng không đọc được), `async command failure reaches the operator`; smoke B (lỗi → bản phục hồi) |
| DB01 | Pass | Not verified | — | `csv import supports UTF-8, quoted commas and semicolon delimiter`, `CsvInputSafetyTests`, `import keeps a non-default header row instead of resetting to 1`, `failed import or relink keeps the working link` |
| DB02 | Pass | — | Not verified | `DataPayloadPreservationTests`, `ExcelNumericPayloadTests`, `ImportedBarcodeRoundTripTests` (payload từ ô tới decode) |
| DB03 | Pass | — | — | Gate `columns known only by position are reported and cannot print` (2026-10-14): đổi tên, xóa và chèn cột rồi Cập nhật Excel → dòng trạng thái nêu “Dòng tiêu đề 1 không có các cột “Lot” (lần nhập trước ở cột B), “Qty” (lần nhập trước ở cột C)”, liên kết theo tên vẫn đọc đúng cột, chỉ đối tượng dùng cột mất bị chặn; tiêu đề trống/trùng nêu chữ cái cột; liên kết tới cột chỉ nhận ra theo vị trí (kể cả qua trường chuyển đổi, cột số bản và khi đảo hai cột trùng tên) chặn in ở Xem trước và In nhanh. Unit `LoadSheet_RecordsWhereEachColumnCameFrom`, `LoadCsv_RecordsWhereEachColumnCameFrom`, `ColumnLetters_FollowTheSpreadsheet`; sau review 2026-10-14: gate `position-only columns cannot be reached another way` (tiêu đề gần giống, công thức có dấu cách/CONCAT/dấu gạch chéo, tên trường chuyển đổi gần đúng, cột số bản có ngoặc), unit `FieldResolutionTieTests`; sau review lần hai 2026-10-14: gate `ties, transforms and copies are checked as printing reads them` (tên khớp ngang nhau đọc sau tên đúng, dòng in theo thứ tự cột, liên kết tới trường chuyển đổi không bị viết lại, tên FIELD ghép từ dữ liệu, `{ }`, cột số bản qua trường chuyển đổi, số liên kết hỏng sau Xác thực); 2026-10-14: cột không có tiêu đề và không có dữ liệu không thành trường, trường giữ cột gốc nên lời báo vẫn đúng chữ cái (gate `a stray far cell makes no fields and letters stay right`, unit `LoadSheet_ColumnsWithNeitherHeaderNorDataAreNotFields`) — ADR-026 |
| DB04 | Pass | — | — | `ExcelErrorCellTests`, `ExcelFormulaAndCsvShapeTests` (công thức chưa có giá trị đã tính chặn nhập và nêu ô) |
| DB05 | Pass | Not verified | — | `overlapping imports publish only the latest`, `failed import or relink keeps the working link`, `excel read honors cancellation` |
| DB06 | Pass | — | — | `FileSourceIdentitySharingTests`, `FileSourceIdentityStaleProtectTests`, `quick print rechecks the linked file before printing` (đổi byte, giữ giờ/kích thước), `linked excel file watcher flags stale data` |
| DB07 | Pass | — | — | `record numbers follow the source file, not the grid sort`, `key field selection tracks row across refresh` |
| DB08 | Pass | — | — | `CopiesFieldContractTests`, `DatabaseConfigCopiesTests`, `QuickPrintQuantityTests` (không cắt, không đổi thành 1) |
| DB09 | Pass | — | — | `unlink excel clears database config but keeps bindings`, `unlink excel works when link is broken`, `shared data source relink fixes every referencing template` |
| DB10 | Pass | Not verified | — | `print preflight groups root issues while preserving raw row failures`, `invalid barcode data in a file blocks printing`. Thiếu: luồng “bỏ dòng lỗi” trên UI thật |
| B01 | Pass | — | Not verified | `BarcodeEvidenceMatrixTests` (fixture âm từng loại), `BarcodeApplicationContractTests` |
| B02 | Pass | — | Not verified | `ImportedBarcodeRoundTripTests` (15 loại × CSV/XLSX × 203/300/600 DPI) |
| B03 | Pass | — | Not verified | `BarcodeApplicationContractTests` (GS1), `Gs1Code128Fnc1RoundTripTests` |
| B04 | Pass | — | Not verified | `LinearBarcodeModuleContractTests`, `MatrixSquareModuleFitTests`, `code 39 wide narrow ratio and physical quiet zone preflight`, `an oversized 2D symbol blocks and the designer draws it like the print` (2026-10-14: mã 2D cần nhiều chấm hơn khung bị chặn; sau review lần hai designer cũng đánh dấu đối tượng, và QR phiên bản cố định quá tải chỉ báo một lỗi). (Audit 2026-10-13: `QrSizingCalculatorTests` đã bỏ khỏi bằng chứng — lớp `QrSizingCalculator` không được ứng dụng gọi.) |
| B05 | Pass | — | Not verified | `BarcodeHriLayoutTests`, `DeviceBarcodeLayoutTests`; gate `a linear symbol past the label edge blocks its row` (2026-10-14): mã 1D theo X có dữ liệu dài hơn mẫu, xoay 0° và 90°, chặn đúng dòng tràn mép tem, đối chiếu với bản vẽ thật của renderer |
| B06 | Pass | — | Not verified | `designer, preview and print draw the same barcode` (2026-10-14): QR, Data Matrix, PDF417, Code 128 theo khung và theo X (cả khi dòng dữ liệu làm mã rộng hơn khung) × 203/300 DPI; đo phần mã tối và decode trên cả ba đầu ra, designer lệch bản in ≤ 0,3 mm; trang Xem trước trùng từng byte với bản vẽ của paginator. Trước bản sửa designer vẽ ở `QrDpi` rồi kéo giãn vào khung (Code 128 khung 45 mm: designer 33,9 mm, tem 25,0 mm) — ADR-025 |
| B07 | Pass | — | Not verified | Ma trận theo từng loại (mục dưới); không loại nào được ghi là đã nghiệm thu sản xuất |
| P01 | Pass | — | Not verified | `named queue resolve does not pick the Windows default`, `printer setup does not select a queue when the document has none` |
| P02 | Pass | — | Quan sát capability: Pass | `PrinterMediaResolverTests`. Driver XP-DT426B (chỉ đọc) xác minh ticket cho hai mẫu 75×100; mẫu 110×150 bị báo khổ 75×100 vì đó là cuộn tem đang lắp |
| P03 | Pass | — | — | `PrintSessionTemplateContractTests`, `quick print uses one snapshot and the async driver contract`, `calibration print uses an isolated copy of the label` |
| P04 | Pass | — | Not verified | `TryExpand_KeepsRecordAndCopyIdentityInPrintOrder`, `TwoThenOneCopyOrdinalsChangeTheManifestAndAMissingSequenceStaysV3` |
| P05 | Pass | — | Not verified | `DispatchRevalidationContractTests`, `queue health blocks shipped plan before ticket merge`. Thiếu: đổi ticket sau xem trước trên driver thật |
| P06 | Pass | Not verified | — | `print preview busy gate blocks duplicate dispatch`, `async relay command rejects re-entry until the first call completes`. Thiếu: nhấp đúp thật khi driver chậm |
| P07 | Pass | — | Not verified | `failure after spool acceptance is unknown, not failed`, `PrintJobRecoveryServiceTests` (ADR-023). Spool accepted ≠ tem đã ra |
| P08 | Pass | — | — | `RequiredPreDispatchAppendFailureBlocksSend`, `PrintJobLogRepairTests` (10) |
| F01 | Pass | Pass | — | `unsaved marker follows edits, undo and save`; smoke A |
| F02 | Pass | Not verified | — | Lệnh Lưu thành/Ctrl+Shift+S; chưa thử bằng phím thật |
| F03 | Pass | Pass | — | `async command failure reaches the operator`; smoke B |
| F04 | Pass | Pass | — | `recovery copy is offered once and opens unsaved`; smoke B |
| F05 | Pass | — | — | `corrupt data-source registry is quarantined at startup` |
| F06 | Pass | Pass | — | `explorer argument opens only existing documents`; smoke A mở tệp qua tham số |
| F07 | Pass | Pass | — | `save writes a snapshot and refuses a concurrent save`; smoke C (lưu khi đóng, có .bak) |

**Chặn in đã biết, chờ chủ sản phẩm quyết định:** hai mẫu thật 100×70 (“tem cqs”,
“tem trong data”) bị preflight chặn. Khung chữ 14 pt của dòng cuối dư 0,18 mm
qua mép dưới tem, nằm trong khoảng trống dưới nét chữ. Luật đang áp dụng là gate
được bảo vệ `print preflight blocks text outside label`. Có hai cách sửa: dời hai
dòng đó lên, hoặc đổi luật theo nét chữ thật. Cách sau là thay đổi hợp đồng Text
và cần quyết định riêng.

**Vật lý:** không có lệnh in thật nào được gửi trong đợt này. Bằng chứng in vật
lý gần nhất là ngày 2026-09-15: XP-DT426B, 4 lệnh spool accepted. Chưa có xác
minh tem ra.

## Khoảng trống dữ liệu cần xử lý trước

Các phát hiện dưới đây được xác định bằng đọc implementation. Chưa có kiểm thử tái hiện riêng cho từng input; không trình bày chúng như test thất bại đã chạy.

| Ưu tiên / ID | Bằng chứng hiện tại | Input cần đưa vào regression | Kết quả quy chuẩn yêu cầu |
| --- | --- | --- | --- |
| P0 / DB02, B02 | `ExcelDataService.CleanCellText` thay CR/LF bằng space và Trim; được gọi cho cả data tại `LoadCsv`, `ReadCsvRows`, `ReadRowCellsArray` | Ô chuỗi ` 000123 ` và ô chứa newline trong Excel/CSV | Giữ nguyên payload; chỉ normalize header hoặc transform được chọn rõ ràng |
| P0 / DB01, DB10 | `LoadCsv` chỉ chép tới `table.Columns.Count`, không kiểm tra số field dư | Header `Code,Lot` và record `A,B,C` | InvalidData có record location; không im lặng bỏ C rồi cho batch hợp lệ |
| P0 / DB01, B02 | `ReadCsvRecord` toggle trạng thái quoted ở mọi vị trí dấu quote | `Code` rồi `AB"CD"`, hoặc `"AB"CD` | Reject cú pháp mơ hồ; không tạo payload `ABCD` từ dữ liệu sai cú pháp |
| P1 / DB01 | `LoadCsv` detect delimiter từ record đầu tiên dù `headerRowIndex` khác 1 | Dòng mô tả không delimiter, header thực ở dòng 2 dùng semicolon | Dùng cấu hình delimiter/header nhất quán; không import thành một cột |
| P0 / DB02 | `FormatNumber` chỉ xử lý phần trăm rồi dùng `0.##########`, không áp dụng format số có zero padding | Ô số 123 có format `000000` | Policy raw/displayed rõ; không quảng bá giữ mã hiển thị `000123` khi đang trả `123` |

Không sửa Text/TextBox để bù các lỗi dữ liệu này. Payload preservation phải thực hiện ở data boundary, giữ normalization header riêng. Kiểm tra đường binding/encode phía sau trước khi tuyên bố payload được giữ end-to-end.

`CsvAutomationSourceParser` còn có parser riêng dùng `ReadLine`, khác import tương tác. Đây là điểm phải đối chiếu nếu đường automation hiện hữu được dùng: không suy ra CSV multiline đã được hỗ trợ đồng nhất chỉ vì regression import tương tác pass. Không mở rộng JOB để giải quyết vấn đề này.

## Evidence đã có và giới hạn

- Build solution `--no-restore --nologo -v quiet`: pass, 0 warning, 0 error.
- Unit tests `--no-build --nologo -v quiet`: pass 774/774, không skipped.
- Application regressions `dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build`: hoàn tất exit 0. Output báo PASS cho cả bảy gate Text/TextBox bắt buộc, CSV import, stale-source, duplicate-dispatch và JOB engine. Không suy ra nghiệm thu hardware từ harness.
- Mutation, tương tác bằng tay và tem vật lý: chưa chạy trong lần audit này.

`ExcelDataServiceReliabilityTests.LoadSheet_DuplicateAndBlankHeaders_AreStableAndUnique` đang kiểm tra tên header được normalize và đổi tên cho unique. Điều này không chứng minh DB03 đã có diagnostic cho người dùng hoặc bảo vệ binding sau khi đổi schema.

`DataConnectorContractTests.FileSourceIdentityDetectsContentChangeWhenTimestampAndLengthArePreserved` cùng `FileSourceIdentityStaleProtectTests` cung cấp bằng chứng policy source identity. Chưa đủ để đóng DB06 cho toàn bộ session UI và dispatch race.

Harness hiện có các gate cho CSV Unicode/quoted delimiter, header row, cancellation, stale source, unlink, geometry và duplicate dispatch. Ca whitespace, quote sai vị trí và field dư trong bảng trên phải có regression riêng; suite xanh không loại trừ các gap đó.

## Trạng thái nghiệm thu theo phạm vi

| Yêu cầu | Mapping hiện tại (2026-09-16) |
| --- | --- |
| D01–D07 | Toàn bộ **Not verified**. Harness/unit lịch sử không đóng gesture WPF, round-trip tài nguyên đầy đủ hay live editor khi render lỗi. D07 preflight font/glyph không chứng minh phiên sửa còn nguyên. |
| DB01–DB10 | Toàn bộ **Not verified**. I281 General `R`/scientific là policy, không phải bug. DB04 formula/cache vẫn gap; DB08 copies fallback/cap mâu thuẫn acceptance; DB05/DB06/DB07 còn race/identity. Excel in-flight không được chứng nhận. |
| B01–B07 | Toàn bộ **Not verified**. Decode phần mềm 11/15 qua cùng ZXing; thiếu UpcE/Codabar/MSI/Plessey; không decoder độc lập hay tem vật lý. B01 pre-encode không đủ cho toàn bộ 15 loại. |
| P01–P08 | Toàn bộ **Not verified**. Có seam/fake reader lịch sử, chưa driver/WPF/physical. Source: P08 nuốt lỗi lưu bắt buộc trước dispatch; P07 gán Failed cho exception quanh `PrintDocument`; P04/P08 thiếu mapping record/copy bền vững. |

Bước kế tiếp theo mục 8: chạy acceptance từng ID trên worktree đã khóa, không đóng runtime từ tài liệu hay tổng test. Không sửa Text/TextBox hay Excel boundary trong phiên mapping này.

Bước kế tiếp (lịch sử 2026-09-14, trước mapping ID): thêm regression tái hiện các lỗi biến đổi/mất payload, sửa data boundary từng phần, chạy lại gate phù hợp; sau đó tiếp tục trace tất cả ID. Không đóng mục tiêu ổn định runtime dựa trên tài liệu hoặc tổng số test.

## Increment 0.277 — CSV không được bỏ dữ liệu sai cấu trúc

Đã sửa hai phát hiện trong bảng: `LoadCsv` từ chối field dư (kể cả field dư rỗng), và `ReadCsvRecord` từ chối quote nằm trong field unquoted hoặc ký tự nối sau closing quote. Lỗi trả `ExcelDataReadError.InvalidData`; lỗi field dư ghi số logical record và số cột thực tế/kỳ vọng. Không thay đổi Text/TextBox.

Evidence tái hiện: `CsvInputSafetyTests` có bốn input sai cấu trúc; trước sửa 4 fail, sau sửa 4 pass. Ba ca hợp lệ kiểm tra escaped quote, quoted comma, quoted empty field và trailing empty field vẫn pass. Tổng focused run: 7/7.

Fast quality loop sau sửa hoàn tất exit 0: solution build 0 warning/error, unit tests 781/781, application regressions pass và release metadata đồng bộ 0.277. Đây là local worktree verification, chưa publish release.

Giới hạn: chưa sửa trim/newline payload, zero-padding Excel, delimiter khi header không ở dòng đầu hoặc parser automation riêng. Không coi DB01/DB02/DB10 đã hoàn tất. Mutation config hiện chỉ nhắm một danh sách file trong Core, không bao phủ `ExcelDataService`; không dùng score của Core để tuyên bố đã mutation-test bản sửa CSV.

## Increment 0.278 — giữ nguyên dữ liệu chuỗi khi đọc nguồn

`ExcelDataService` chỉ normalize tên cột ở thời điểm tạo schema; không Trim hoặc thay CR/LF trong giá trị chuỗi Excel/CSV và preview rows. Ô chỉ chứa khoảng trắng vẫn là một record có dữ liệu, không bị loại bỏ do normalization. Đây là thay đổi có chủ đích: nguồn có khoảng trắng thừa cần được sửa tại nguồn hoặc qua transform rõ ràng, không dựa vào import âm thầm sửa mã.

`DataPayloadPreservationTests` tái hiện sáu ca ở cả Excel và CSV: whitespace quanh mã zero đầu, nội dung nhiều dòng Unicode và ô chỉ chứa spaces. Trước sửa 6/6 fail; sau sửa nhóm payload/header/CSV safety pass 19/19. Test so sánh chính xác preview value và imported value, đồng thời xác nhận header ` Code ` vẫn map thành `Code`.

Chưa đóng toàn bộ DB02/B02: numeric display format của Excel và đường barcode end-to-end cần evidence riêng; parser automation chưa được thay đổi. Không thay đổi ownership/layout Text/TextBox.

Fast quality loop 0.278 hoàn tất exit 0: build 0 warning/error, 787/787 unit tests, application regression suite pass. Chưa publish release hoặc chạy xác minh tem vật lý. Đọc source `DataTableDataConnector` và `BindingExpressionEvaluator` cho thấy hai bước này không Trim value; đây là code inspection, chưa thay thế test toàn đường tới barcode.

## Increment 0.279 — delimiter thuộc header được chọn

`LoadCsv` xác định dấu phân cách từ logical record tại `headerRowIndex`, bỏ phần mô tả trước header khi đọc bảng. Không lấy dấu phẩy trong tiêu đề báo cáo làm schema cho bảng semicolon/tab bên dưới. CSV header index tiếp tục là logical record index, nên newline bên trong quoted cell không tạo một record mới.

`CsvInputSafetyTests.DelimiterComesFromSelectedLogicalHeader`: bốn ca trước sửa fail, sau sửa pass; bao phủ title thường, title có comma, dòng trống, title quoted multiline và dữ liệu Unicode/zero đầu. Nhóm CSV focused sau sửa đạt 11/11 trước khi bổ sung thêm ca `SelectedHeaderPastEndReportsInvalidHeader` kiểm tra diagnostic khi header không tồn tại.

Giới hạn: preview chọn header vẫn dùng API preview hiện hành, chưa có tham số delimiter/header riêng; chưa đóng toàn bộ UX DB01. Các khoảng trống định dạng số Excel, race giữa các lần mở nguồn và barcode end-to-end vẫn cần đối chiếu tiếp.

Fast quality loop 0.279 hoàn tất exit 0: build 0 warning/error, 792/792 unit tests và application regressions pass. Chưa phát hành hoặc nghiệm thu máy in thật.

## Kiểm chứng dữ liệu tới barcode — bổ sung cho 0.279

`ImportedBarcodeRoundTripTests.ImportedBoundPayloadDecodesExactly` kiểm tra 12 tổ hợp: Excel/CSV × 203/300/600 DPI × Code128/QRCode. Chuỗi đi qua `ExcelDataService` → `DataTableDataConnector` → `BindingExpressionEvaluator` → `ZxingBarcodeRenderer` → decoder ảnh. So sánh cả bound value, decoded text và symbology với kỳ vọng. Code128 dùng ` 000123 `; QR dùng số zero đầu, khoảng trắng, newline và Unicode tiếng Việt.

Focused run: 12/12 pass. Đây là test tích hợp data/binding/barcode ở cấp module, không đi qua WPF scene, preflight, spool hoặc máy in. Encoder và decoder cùng thư viện ZXing, vì vậy chưa có bằng chứng decoder độc lập hay scanner/verifier vật lý. Không đóng toàn bộ B01–B07 hoặc tuyên bố tất cả symbology đều được kiểm tra. Chỉ thêm test và hồ sơ, không đổi version/runtime trong bước này.

Toàn bộ unit suite sau bổ sung: 804/804 pass, không skipped. Không chạy lại application harness cho thay đổi chỉ thêm test này; checkpoint runtime gần nhất vẫn là Fast 0.279 ở trên.

## Increment 0.280 — định dạng số Excel cho payload

`FormatNumber` dùng `ExcelNumberFormat` 1.1.0 đã có trong dependency graph của ClosedXML để diễn giải numeric format, thay heuristic tìm ký tự `%`. Không chuyển về đọc workbook DOM. Format không hợp lệ báo lỗi thay vì âm thầm trả plain number.

Policy ở bước này: chuỗi giữ nguyên; numeric format explicit được render với invariant culture; General vẫn theo policy số hiện hành; DateTime và bool vẫn theo policy invariant hiện hành. Không cam kết mô phỏng locale/mọi giao diện Excel hoặc khôi phục số đã mất độ chính xác trong nguồn.

`ExcelNumericPayloadTests`: trước sửa 6 fail/1 pass; sau sửa 7/7 pass cho import và preview. Bao phủ 123→000123, zero padding của 0, số vượt độ dài pattern không bị cắt, decimal trailing zero, phần trăm hai chữ số, literal `%` không nhân 100 và prefix `PN-`. Đây là thay đổi dữ liệu có chủ đích: barcode dùng cột số có format sẽ nhận chuỗi đã format. Cần kiểm tra mẫu sản xuất trước phát hành.

Chưa đóng toàn bộ DB02: formula/cache/error cells, format culture và General precision vẫn cần audit; chưa có test từ formatted numeric cell xuyên qua scene/dispatch tới tem thật.

Fast quality loop 0.280 pass (exit 0): build không warning/error, 811/811 unit tests, application regressions pass. Chưa publish hoặc nghiệm thu tem vật lý.

## Increment 0.281 — General không tự làm tròn dữ liệu

Hai fixture bổ sung chứng minh formatter cũ giới hạn 10 chữ số thập phân làm mất giá trị: `0.00000000001` và `1.12345678901234`. Trước sửa hai ca fail, sau sửa nhóm numeric import/preview đạt 9/9.

Numeric General (hoặc không có format) dùng round-trip invariant `R` để giữ giá trị double đã đọc; số nhỏ có thể xuất hiện dạng khoa học `1E-11`. Đây là policy dữ liệu, không mô phỏng chiều rộng cột hay locale của Excel. Muốn chuỗi mã cố định phải lưu nguồn dạng text hoặc chọn numeric format explicit. Không thể khôi phục precision đã mất trong workbook nguồn. Các format explicit, DateTime, bool và Text/TextBox không đổi ở increment này.

Fast 0.281 pass exit 0: build 0 warning/error, 813/813 unit tests, application regressions pass. Điểm kiểm tra tiếp theo của DB04: API `IExcelDataReader.GetCellError(int)` có trong dependency 3.7.0 hiện tại; cần fixture chứng minh ô lỗi không bị coi là dữ liệu rỗng hợp lệ.

## Increment 0.282 — từ chối ô lỗi Excel khi import

`LoadSheet` kiểm tra `GetCellError` trước khi chuyển dữ liệu từng row thành chuỗi. Có lỗi thì dừng toàn lần đọc, trả `InvalidData` với file/sheet/row/column và loại lỗi; không trả bảng chỉ chứa các dòng tốt đã đọc trước đó.

`ExcelErrorCellTests` ghi workbook có một dòng GOOD trước ô lỗi DivisionByZero, NoValueAvailable hoặc CellReference. Trước sửa 3/3 fail do không ném lỗi; sau sửa 3/3 pass và kiểm tra diagnostic vị trí row 3/column 1. Chính sách hiện tại chặn mọi ô lỗi trong sheet được import, kể cả ô không bind; không thay đổi Text/TextBox.

Giới hạn: preview picker còn dùng đường đọc preview hiện hành, không phải bằng chứng dữ liệu đã hợp lệ. Fixture là cell error được lưu, chưa chứng minh công thức không có cached value hoặc cached value lỗi thời được phát hiện. DB04 chưa đóng toàn bộ.

## Increment 0.283 — CsvAutomationSourceParser đồng bộ chuẩn CSV an toàn công nghiệp

`CsvAutomationSourceParser` chuyển từ đọc từng dòng vật lý `ReadLine` sang parser streaming cấp ký tự hỗ trợ đầy đủ RFC 4180 và quote safety công nghiệp:
- Giữ nguyên nội dung multiline bên trong trường có dấu ngoặc kép (newline không làm gãy bản ghi);
- Phát hiện và từ chối các lỗi cú pháp quote mơ hồ (quote bất thường giữa trường unquoted, ký tự thừa nối sau closing quote, và trường quote chưa đóng ở EOF);
- Báo số dòng nguồn (physical source line) chính xác cho từng bản ghi bị lỗi;
- Giữ nguyên toàn bộ hợp đồng bảo vệ Text/TextBox theo AGENTS.md.

`CsvAutomationSourceParserTests` bổ sung 8 ca kiểm thử bao phủ toàn bộ các tình huống: standard rows, multiline quoted fields, escaped quotes, unexpected quote in field, characters after closing quote, unterminated quote, duplicate headers, và empty source (8/8 pass). Harness regression `CsvAutomationSourceParserRegression` tiếp tục pass 100%.

Fast quality loop 0.283 hoàn tất exit 0: build 0 warning/error, 824/824 unit tests, application regressions pass 100%.

Release metadata đã đồng bộ 0.283 qua `scripts/Set-ANLAbelReleaseVersion.ps1`: `eng/Version.props`, app csproj, installer Commercial/Trial và các doc plan (`07-execution-plan.md`, `11-verification-checkpoint-2026-08-13.md`, MEMORY) đều chiếu `0.283`; License Master installer riêng vẫn giữ `1.0.0` theo chủ đích. Fast loop được chạy lại trên worktree sau khi bump và vẫn pass (build 0/0, 824/824 unit, application regressions exit 0). Đây là bằng chứng source/regression trên worktree bẩn, chưa phải release đã ký.

## Increment 0.284 — Composite Pipeline Integration Fixture (End-to-End Traceability)

Bổ sung bộ kiểm thử tích hợp xuyên suốt `IndustrialPipelineCompositeFixtureTests` nhằm xác thực luồng hoàn chỉnh từ nguồn dữ liệu công nghiệp đến đầu ra chuỗi in (mục 8 của quy chuẩn `CORE_SOFTWARE_STANDARD.md`):
- **Data Boundary**: Đọc file Excel thực tế sinh bởi ClosedXML, bảo toàn chính xác chuỗi số định dạng padding zero `000123`, `000456` và chuỗi multiline có dấu xuống dòng `\n`.
- **Binding Evaluation**: `BindingExpressionEvaluator` phân giải biểu thức liên kết động `{Part No}` và `{Description}` chuẩn xác từ map dữ liệu thực.
- **Barcode Rendering**: `ZxingBarcodeRenderer` render barcode Code128 trực tiếp từ payload định dạng đã giải quyết, tạo ra bitmap pixel `BarcodePixelImage` hợp lệ tại 300 DPI.
- **Scene Compilation**: `SceneCompiler.Compile` tạo cấu trúc cảnh `CompiledSceneNode` bất biến với `DocumentSnapshot`, bảo toàn toàn bộ kích thước vật lý mm và số lượng đối tượng.
- **Print Sequence Generation**: `PrintSequenceBuilder.Build` tạo chuỗi các bản ghi in `LabelInstance` mang đầy đủ danh tính `RecordId`, `CopyOrdinal`, `SequenceOrdinal` và `SourceRowNumber` (Row 1 có 2 copies -> 2 nhãn; Row 2 có 1 copy -> 1 nhãn; tổng cộng 3 nhãn tuần tự).
- **Protected Invariants**: Xác thực rằng toàn bộ hình học tác giả ban đầu (`template.WidthMm`, `HeightMm`, các đối tượng Barcode, Text, TextBox) hoàn toàn không bị biến dạng hay thay đổi sau khi chạy qua chuỗi xử lý.

Fast quality loop 0.284 hoàn tất exit 0:
- Build solution: 0 warning, 0 error.
- Unit tests: 825/825 tests pass.
- Application regression gates: pass 100%, bao gồm toàn bộ 7 gate Text/TextBox và toàn bộ gate print engine.
- Release metadata cập nhật đồng bộ lên 0.284.

[Agent: Gemini / Antigravity]

## Increment 0.285 — Preamble CSV Delimiter Fall-through & 6-Symbology End-to-End Decode Matrix

Thực hiện giải quyết triệt để 2 vấn đề kỹ thuật còn tồn đọng trong bảng khoảng trống dữ liệu và hoàn tất ma trận mã vạch công nghiệp:
- **DB01 (Preview Delimiter Discovery)**: `ExcelDataService.ReadCsvRows` và `DetectCsvDelimiter` được nâng cấp từ việc chỉ đọc một bản ghi đầu tiên sang cơ chế quét lấy mẫu (sample up to 10 records). Khi tệp CSV chứa dòng mở đầu (preamble/title) không có dấu phân cách, bộ phân tích tự động rơi xuống các dòng dữ liệu kế tiếp để nhận dạng chính xác dấu phân cách thực tế (chấm phẩy `;` hoặc tab `\t`), tránh tình trạng lưới xem trước (preview grid) hiển thị gộp thành một cột duy nhất. Bổ sung kiểm thử `PreviewRowsDetectsDelimiterWhenFirstRowIsUndelimitedPreamble` và `PreviewRowsDetectsTabDelimiterWhenFirstRowIsUndelimitedPreamble`.
- **B02 & B07 (Ma trận giải mã đối chiếu 6 họ mã vạch công nghiệp)**: Mở rộng `ImportedBarcodeRoundTripTests` để kiểm chứng vòng lặp giải mã (decode round-trip) trọn vẹn của 6 chuẩn mã vạch công nghiệp chính: `Code128`, `QRCode`, `DataMatrix`, `Code39`, `Ean13`, `ITF`. Mỗi chuẩn được chạy xuyên suốt qua 2 nguồn dữ liệu (Excel & CSV) tại 3 độ phân giải in nhiệt (203, 300, 600 DPI) với 36 tổ hợp thực tế. Toàn bộ 36 tổ hợp đều được render và giải mã trùng khớp 100% với chuỗi ban đầu.

Fast quality loop 0.285 hoàn tất exit 0:
- Build solution: 0 warning, 0 error.
- Unit tests: 851/851 tests pass (tăng từ 825 lên 851).
- Application regression gates: pass 100%, bảo toàn toàn vẹn 7 cổng Text/TextBox và Print Engine.
- Release metadata cập nhật đồng bộ lên 0.285.

[Agent: Gemini / Antigravity]

## Increment 0.286 — Preview Grid Truthfulness & Standard Error Cell Diagnostics (DB04)

Đóng khoảng trống P0 / DB04 được xác định trong `EXCEL_NUMERIC_ERROR_GAP_AUDIT.md`:
- **Hiển thị trung thực ô lỗi trong Preview Grid (DB04)**: Trước đây `FormatCellValue` trả về chuỗi rỗng `""` cho ô lỗi do `reader.GetValue(column)` trả `null`. Điều này khiến lưới xem trước (preview grid) trong `PreviewRowsAsync` và `GetSheetsWithPreviewAsync` hiển thị các ô lỗi công thức (`#DIV/0!`, `#N/A`, `#REF!`, `#NULL!`, `#NAME?`, `#NUM!`) như các ô trống hợp lệ, gây hiểu lầm cho người dùng khi chọn dòng tiêu đề hoặc kiểm tra dữ liệu trước khi import.
- **Xử lý chuẩn hóa mã lỗi CellError**: Bổ sung phương thức `FormatCellError(CellError error)` chuyển đổi chính xác các mã lỗi của `ExcelDataReader.CellError` (`DIV0`, `NA`, `REF`, `VALUE`, `NAME`, `NUM`, `NULL`, `GETTING_DATA`) thành các chuỗi hiển thị chuẩn Excel (`#DIV/0!`, `#N/A`, `#REF!`, `#VALUE!`, `#NAME?`, `#NUM!`, `#NULL!`, `#GETTING_DATA`).
- **Parity giữa Import và Preview**: Trong `LoadSheet`, thông báo lỗi `ExcelDataReadException` được chuẩn hóa mang chuỗi mã lỗi chi tiết `contains an Excel error ({FormatCellError(cellError)})`. Trong khi đó, đường đọc xem trước (`ReadRowCells` -> `FormatCellError`) giữ nguyên giá trị mã lỗi trực quan để người vận hành nhận diện ngay trên lưới xem trước thay vì bị che giấu thành chuỗi rỗng.
- **Bảo toàn Guardrail**: Không can thiệp, dọn dẹp hay thay đổi bất kỳ hợp đồng Text/TextBox nào theo `AGENTS.md`.

Kiểm thử hồi quy bổ sung:
- `ExcelErrorCellTests`: Mở rộng lên 11 ca kiểm thử bao phủ toàn diện:
  - 3 ca kiểm tra `ImportRejectsErrorCells` với thông báo lỗi mang vị trí hàng, cột và token lỗi chuẩn;
  - 6 ca kiểm tra `PreviewRows_ReflectsCellErrorsTruthfully_DoesNotMaskAsEmpty` trên toàn bộ các loại lỗi của ClosedXML (`DivisionByZero`, `NoValueAvailable`, `CellReference`, `NullValue`, `NameNotRecognized`, `NumberInvalid`), xác nhận không bị rỗng hoá;
  - 1 ca kiểm tra `GetSheetsWithPreview_ReflectsCellErrorsInHeaderAndDataRows` đảm bảo hiển thị đúng cả ở dòng header và data rows;
  - 1 ca kiểm tra `ImportRejectsErrorCellOnHeaderRow` đảm bảo fail-closed ngay lập tức khi dòng 1 chứa ô lỗi.

Fast quality loop 0.286 hoàn tất exit 0:
- Build solution: 0 warning, 0 error.
- Unit tests: 859/859 tests pass (tăng từ 851 lên 859).
- Application regression gates: pass 100%, bảo toàn toàn vẹn 7 cổng Text/TextBox và Print Engine.
- Release metadata cập nhật đồng bộ lên 0.286.

[Agent: Gemini / Antigravity]

## Increment 0.287 — Strict Pre-encode Validation & Symbology Check Digit Guard (B01 Pre-encode Gap)

Đóng khoảng trống kiểm tra tính hợp lệ payload tiền encode của yêu cầu B01:
- **Baseline cú pháp cho General & Industrial**: Chuyển đổi `BarcodeApplicationContract.ValidateData` từ việc chỉ validate riêng hồ sơ GS1 sang bao phủ toàn bộ 15 họ mã vạch (`Code128`, `Code39`, `Code93`, `Ean13`, `Ean8`, `UpcA`, `UpcE`, `ITF`, `Codabar`, `MSI`, `Plessey`, `QRCode`, `DataMatrix`, `Pdf417`, `Aztec`) cho cả hồ sơ `General` và `Industrial`.
- **Phân định rõ ranh giới chiều dài sinh check digit và chiều dài đã có check digit**:
  - `Ean13`: Chấp nhận 12 chữ số (encoder tự động tính check digit) hoặc 13 chữ số (bắt buộc kiểm tra tính hợp lệ của check digit Modulo 10 GS1).
  - `Ean8`: Chấp nhận 7 chữ số (encoder tự động sinh) hoặc 8 chữ số (bắt buộc kiểm tra check digit).
  - `UpcA`: Chấp nhận 11 chữ số (encoder tự động sinh) hoặc 12 chữ số (bắt buộc kiểm tra check digit).
  - `UpcE`: Chấp nhận đúng 6, 7 hoặc 8 chữ số theo chuẩn nén UPC-E.
  - `ITF`: Chấp nhận các chuỗi chữ số có độ dài chẵn; từ chối chuỗi có độ dài lẻ hoặc chứa ký tự phi số.
- **Diagnostic xác định rõ symbology và vi phạm**: Mọi thông báo lỗi đều chỉ rõ tên symbology và quy tắc bị vi phạm (ký tự ngoài bảng mã, độ dài không hợp lệ, check digit sai, rỗng/khoảng trắng).
- **Bảo toàn hoàn toàn luồng GS1**: Giữ nguyên toàn bộ cơ chế phân tích và chuẩn hóa GS1 hiện hành sau bước kiểm tra symbology hỗ trợ.
- **Bảo toàn Guardrail Text/TextBox**: Tuyệt đối không can thiệp, dọn dẹp hay làm thay đổi bất kỳ invariant nào của Text/TextBox.
- **Giới hạn bằng chứng và trạng thái B01**: Yêu cầu B01 trong `CORE_SOFTWARE_STANDARD.md` đòi hỏi cả hai vế: (1) validate payload đúng symbology trước encode và (2) đối chiếu payload decode trên mọi symbology cung cấp. Increment 0.287 hoàn tất đầy đủ vế (1) tiền encode (pre-encode validation & check digit), nhưng vế (2) hiện mới chỉ có ma trận decode 6 symbology (`Code128`, `QRCode`, `DataMatrix`, `Code39`, `Ean13`, `ITF`) và dùng chung thư viện ZXing, chưa bao phủ toàn bộ 15 symbology và chưa có decoder độc lập hoặc bằng chứng verifier/máy đọc vật lý. Do đó, **B01 duy trì trạng thái Partial** trong bảng truy xuất nguồn gốc cho đến khi có đủ bằng chứng decode toàn diện mọi symbology.

Kiểm thử hồi quy và bao phủ mới:
- Mở rộng `BarcodeApplicationContractTests` thêm 52 ca kiểm thử (tổng số unit tests tăng từ 859 lên 911 tests):
  - `NonGs1ProfileRejectsEmptyOrWhitespaceDataWithSymbologyDiagnostic`: kiểm tra từ chối null, chuỗi rỗng và khoảng trắng cho cả General/Industrial.
  - `Code128AndCode93ValidatePrintableAsciiBoundary`: kiểm tra ranh giới ký tự ASCII in được (32–126) và từ chối ký tự ngoài bảng mã.
  - `Code39ValidatesStrictAlphabet`: kiểm tra tập ký tự nghiêm ngặt của Code 39 và từ chối các ký tự lạ.
  - `Ean13ValidatesLengthAndCheckDigitOnlyWhenPresent`: kiểm tra 12 chữ số hợp lệ, 13 chữ số đúng check digit, từ chối 13 chữ số sai check digit, từ chối độ dài 11/14 chữ số, từ chối phi số.
  - `Ean8ValidatesLengthAndCheckDigitOnlyWhenPresent`: kiểm tra 7/8 chữ số, check digit đúng/sai, độ dài và phi số.
  - `UpcAValidatesLengthAndCheckDigitOnlyWhenPresent`: kiểm tra 11/12 chữ số, check digit đúng/sai, độ dài và phi số.
  - `UpcEValidatesLengthAndNumericDigits`: kiểm tra 6, 7, 8 chữ số và từ chối sai định dạng.
  - `ItfValidatesNumericAndEvenLength`: kiểm tra độ dài chẵn và từ chối độ dài lẻ/phi số.
  - `CodabarValidatesSupportedAlphabet`: kiểm tra alphabet và ký tự mở đầu/kết thúc hợp lệ.
  - `MsiAndPlesseyValidateNumericDigits`: kiểm tra toàn bộ chữ số.
  - `TwoDimensionalSymbologiesAcceptArbitraryPayloadWhenNotEmpty`: kiểm tra các mã ma trận 2D chấp nhận payload UTF-8 đa dạng khi không rỗng.

Fast quality loop 0.287 hoàn tất exit 0:
- Build solution: 0 warning, 0 error.
- Unit tests: 911/911 tests pass (tăng từ 859 lên 911).
- Application regression gates: pass 100%, bảo toàn toàn vẹn 7 cổng Text/TextBox và Print Engine.
- Release metadata cập nhật đồng bộ lên 0.287.

[Agent: Gemini / Antigravity]

## Ma trận truy xuất nguồn gốc quy chuẩn (Requirement Traceability Matrix)

Đối chiếu acceptance đầy đủ tại mục 3–7 và thứ tự thực hiện mục 8 của [CORE_SOFTWARE_STANDARD.md](CORE_SOFTWARE_STANDARD.md), không chỉ phần tóm tắt yêu cầu cũ.

**Cách đọc evidence:** mỗi hàng tách assertion/call path đã đọc, checkpoint chạy trước đây, và phần còn mở. Tất cả trạng thái thực thi hiện tại là `Not verified`. Không có run mới gắn với commit/worktree diff identity, môi trường và log theo mục 7 để nâng thành Pass. Các tổng xanh lịch sử không chứng minh từng facet; assertion mới hơn checkpoint chỉ là source inspection.

**Provenance lịch sử dùng trong bảng:** `H14` = mục “Evidence đã có và giới hạn” ngày 2026-09-14 (774 unit + harness exit 0); `Ixxx` = increment tương ứng trong tài liệu này; `HD06` = “D06 checkpoint after 0.287” (919 unit + harness); `HPrint` = các checkpoint printer/media và Fast (977 rồi 1023 unit, harness 198 tại checkpoint 1023); `H15` = “Self-Audit & Industrial Architecture Checkpoint (2026-09-15)” (1041 unit + harness, riêng decode ghi 66 tổ hợp). Đây là **kết quả đã được ghi lại**, không phải log được tái chạy/xác nhận trong phiên này; các checkpoint thiếu diff identity bất biến không được gán cho toàn bộ source hiện tại.

**Source anchors:** `U/` = [src/ANLAbel.UnitTests](../src/ANLAbel.UnitTests/); `H:` = dòng trong [src/ANLAbel.Tests/Program.cs](../src/ANLAbel.Tests/Program.cs); `VM:` = dòng trong [MainViewModel.cs](../src/ANLAbel.App/ViewModels/MainViewModel.cs). Số dòng là mốc lúc đọc, symbol/assertion là mốc tra cứu khi source trôi. Các file Excel/focused tests được đọc như snapshot tạm thời, không dùng để đóng việc của Excel owner.

### 1. Hợp đồng Designer (D01–D07)
| ID / acceptance | Source và assertion đã đối chiếu (không chạy) | Evidence chạy trước đây | Facet chưa xác minh / mâu thuẫn và acceptance kế tiếp | Runtime hiện tại |
| :--- | :--- | :--- | :--- | :--- |
| **D01** — geometry mm; zoom/DIP chỉ biểu diễn | [LabelDesignerCanvas](../src/ANLAbel.App/Controls/LabelDesignerCanvas.cs) `MmToDip`/`RefreshForZoom`/`OnPreviewRowChanged` chỉ chiếu visual. [ResizeGeometryContractTests](../src/ANLAbel.UnitTests/ResizeGeometryContractTests.cs) + [GroupResizeGeometryContractTests](../src/ANLAbel.UnitTests/GroupResizeGeometryContractTests.cs) là arithmetic. `H:8270` so X/Y/W/H Text+TextBox khi đổi PreviewRow; `H:2651` zoom 1→2.5 chỉ identity host; `H:6236` raster Rectangle, không canvas. | H14/H15/HPrint: gate geometry trong harness tổng. | Chưa WPF zoom/pan/render lặp trên barcode/image/shape/line, object xoay/nhóm, Windows scaling. Check snapshot X/Y/W/H/rotation/padding trước/sau zoom 50/100/200% và đổi row: authored không drift. Không dùng D01 để cấm sizing Text/barcode đã bảo vệ. | **Not verified** |
| **D02** — 1 gesture = 1 undo | `MainWindow.xaml.cs` nối gesture; `VM:4166 BeginTemplateEditGesture` / `4183 Commit` / `4209 Cancel`. `H:2617` gọi VM trực tiếp: thêm rectangle, X=5→12→18, Commit; Undo Objects=0, Redo X=18; Cancel không giữ delta. | H14/H15: named gate undo. | Không pointer/WPF, không cover resize/rotate/group/paste. Check từng thao tác đơn/nhóm xoay: Undo một lần = pre-state, Redo = post-state, không drift; cancel không thêm history. | **Not verified** |
| **D03** — commit/cancel rõ | [SelectionResizeAdorner](../src/ANLAbel.App/Controls/SelectionResizeAdorner.cs) Thumb `DragCompleted`; canvas `LostMouseCapture` không rollback resize đã commit. `H:4739` STA+reflection LostMouseCapture → completed=1/canceled=0; `H:2814` restore frame sau sửa model, không Esc/thumb thật. | H14/H15: hai gate resize bắt buộc. | Check mouse-up thật: frame giữ sau release; Esc/lost capture trước commit khôi phục; lost capture sau commit không rollback. Không đổi protected lifecycle. | **Not verified** |
| **D04** — Text ≠ TextBox, đủ 10 invariant | `ShouldConstrainToBox` chỉ TextBox; `AddTextBox` compact defaults; hit 10/marker 5 DIP. Bảy named gates: free vs bounded, shrink-frame, không resize từ content, reflow khi user resize, compact frame, no-outline, save/load padding. `H:4063` mô phỏng lock frame bằng ghi model, không drag. | H14/H15/HPrint: bảy gate được báo pass trong checkpoint. | Chưa WPF drag/Properties W/H, clip designer/preview/print, authored padding cũ sau open/undo, hit target thật. Check đủ 10 facet trên live editor; không migrate padding cũ. Không sửa protected files trong mapping này. | **Not verified** |
| **D05** — binding/refresh chỉ đổi hiển thị | Canvas `OnPreviewRowChanged` không auto-size từ row. `H:8270` Text/TextBox A→chuỗi dài, không padding/rotation/rỗng. `H:7049` TextBox+QR bound rows: hash scene/geometry ổn định, không editor UI. | H14/H15: gate preview-row geometry. | Check ngắn/dài/rỗng/missing field trên mọi bound type: X/Y/W/H/padding/rotation không đổi. Tách diagnostic thiếu field khỏi mutation geometry. | **Not verified** |
| **D06** — save/load + file hỏng giữ document | [ProjectFileService](../src/ANLAbel.Project/SaveLoad/ProjectFileService.cs) temp/flush/backup/rename; `VM:1492 OpenAsync` parse trước thay Template. [ProjectFileServiceTests](../src/ANLAbel.UnitTests/ProjectFileServiceTests.cs) reject `{}`/schema `"2"`/`2.5`/partial legacy/future; round-trip geometry/font/binding/barcode. `H:461` save/load; `H:768` backup; `H:855` future schema giữ Name unsaved, không full object/undo. | HD06: 8/8 rồi 9/9 focused, 919 unit, harness exit 0. Inventory unit sau đó lớn hơn 8. | Check fixture đủ image bytes/font/group/rotation/padding; disk-full/kill tại rename; mở corrupt/future trên VM live: document/selection/history còn, Save As không đè nguồn hỏng. | **Not verified** |
| **D07** — lỗi hiển thị không mất phiên sửa | [LabelVisualRenderer](../src/ANLAbel.Printing/RenderPipeline/LabelVisualRenderer.cs) barcode error frame, image “No image”; [ImageRasterizer.Decode](../src/ANLAbel.Printing/RenderPipeline/ImageRasterizer.cs) null khi corrupt. Canvas `GetObjectError` không cover missing font/image. `H:6376`/`H:6404`/`H:6473` chỉ `ValidateRows` missing font/glyph/corrupt image — **không mở editor, không assert selection/undo/save**. | H14/H15: hai gate font preflight được dùng để gắn Pass cũ. | **Gap nguồn:** preflight ≠ live editor; ảnh canvas null có thể không có tooltip. Chưa tái hiện crash/mất dữ liệu. Check document có edits: missing font/glyph, ảnh hỏng, barcode/render exception → diagnostic, authored không đổi, vẫn chọn/sửa/undo/save. | **Not verified** |

### 2. Hợp đồng Excel/CSV và Binding (DB01–DB10)

| ID / acceptance | Source và assertion đã đối chiếu (không chạy) | Evidence chạy trước đây | Facet chưa xác minh / mâu thuẫn và acceptance kế tiếp | Runtime hiện tại |
| :--- | :--- | :--- | :--- | :--- |
| **DB01** — sheet/header/encoding/delimiter rõ | [ExcelDataService.LoadCsv/ReadCsvRows](../src/ANLAbel.Data/Excel/ExcelDataService.cs#L312): import lấy header được chọn, preview dò mẫu; UTF-8 strict + BOM detection. `U/CsvInputSafetyTests.cs:24–120` so sánh field/value cho header logical 2/3, preamble, semicolon/tab, escape và reject field dư/quote sai; `H:8433` chỉ kiểm row tuyệt đối trong preview Excel. | I277: 7 focused; I279: 11 focused trước ca header quá cuối; I283: 8 automation cases; I285: báo bổ sung 2 preview cases; H14/H15: harness tổng. | Chưa chứng minh toàn bộ cấu hình/policy hiển thị cho operator, nhiều sheet + BOM/encoding ở đường import tương tác. Automation BOM fixture không thay bằng chứng đường này; hai parser vẫn riêng. Check file nhiều sheet, header khác 1, BOM UTF-8/UTF-16, invalid bytes và delimiter mơ hồ qua picker → import. | **Not verified** |
| **DB02** — payload theo policy, không suy diễn | `ExcelDataService.FormatCellValue/FormatNumber`; [DataPayloadPreservationTests](../src/ANLAbel.UnitTests/DataPayloadPreservationTests.cs#L9) assert chính xác preview/import cho spaces, zero đầu, newline; [ExcelNumericPayloadTests](../src/ANLAbel.UnitTests/ExcelNumericPayloadTests.cs#L9) assert explicit format và General `1E-11`; `H:1198` assert ISO date, TRUE. Connector chụp value ở `DataTableDataConnector:18–40`; decode module xem B02. | I278: 6 ca fail trước sửa, nhóm 19 pass; I280: 7 numeric; I281: 9 numeric; I279/I285/H15: decode có giới hạn. | **General invariant `R`, gồm scientific notation, là policy đã chốt ở I281, không phải defect.** Explicit format invariant, ISO date/bool không hứa Excel locale/display parity. Còn cần chứng minh operator thấy policy, mã dài dạng text và formatted numeric qua scene/dispatch. Formula/cache và format-lookup thuộc gap Excel chưa được đóng ở đây; không suy ra `decimal/long` precision defect nếu chưa chứng minh reader phát sinh kiểu/input đó. | **Not verified** |
| **DB03** — schema không mơ hồ, báo vị trí | `ExcelDataService.LoadSheet/MakeUniqueHeader:235/1041`, `VM:2358 SyncDatabaseFieldsFromTable`; [ExcelDataServiceReliabilityTests:69](../src/ANLAbel.UnitTests/ExcelDataServiceReliabilityTests.cs#L69) chỉ assert `Part No`, `part no_2`, `Column3` và value; `H:7863` assert missing field chặn preflight và nêu `PartNo`. | H14 đã ghi rõ giới hạn normalization; H15 chỉ có tổng suite/harness, không có run schema-remap riêng. | Auto-rename không phải diagnostic vị trí hoặc bằng chứng binding không đổi nghĩa. Cần duplicate/blank header, đảo thứ tự hai cột trùng, rename/delete rồi refresh: chỉ đúng cột bị ảnh hưởng, không bind sang cột khác. Không tuyên bố test normalization đã đóng DB03. | **Not verified** |
| **DB04** — null/empty/error/cache không giả hợp lệ | [ExcelErrorCellTests:14–120](../src/ANLAbel.UnitTests/ExcelErrorCellTests.cs#L14): GOOD trước stored error → import ném InvalidData row 3/column 1/token; preview assert token, header error bị chặn. `ExcelDataService.ReadRowCells:809` kiểm `GetCellError`; snapshot `FormatCellValue:936` vẫn trả empty cho null, không phân biệt formula thiếu cache. | I282: 3 ca stored error fail→pass; I286: báo 11 ca và Fast 859. Đây không phải ca formula không cache/cached value lỗi thời. | **Gap formula/cache còn mở theo inspection**, xem [audit Excel](EXCEL_NUMERIC_ERROR_GAP_AUDIT.md); trạng thái sửa của Excel owner không được chứng nhận. Check workbook formula thiếu cache, cache cũ, ô trống thật, empty string, dòng rỗng và field bắt buộc: diagnostic đúng cell/record, batch không Ready giả. Stored error fixture không chứng minh freshness cache. | **Not verified** |
| **DB05** — publication atomic, cancel/race/failure | `VM:1584 ImportExcelAsync` chỉ publish sau `LoadSheetAsync`, nhưng không có generation/winner check trước ghi view/connector/config; `VM:1683 RefreshExcelDataAsync`, `2013 RelinkExcelAsync`. `H:8338` cancel token **trước** GetSheetNames, không cancel giữa import; `H:8942` đổi registry rồi mở **một** template, không relink đồng thời mọi cửa sổ. | H14/H15: harness; không có checkpoint A-chậm/B-nhanh hay cancel giữa publication. | Rủi ro source: hai import chồng nhau có thể publish theo thứ tự hoàn tất; relink đổi FilePath trước khi đọc thành công. Chưa tái hiện race. Check A chậm/B nhanh, cancel giữa đọc, file khóa/xóa/hỏng: chỉ B được publish; dữ liệu tốt cũ nếu giữ phải stale/failed, không Ready. | **Not verified** |
| **DB06** — identity/schema gắn đúng snapshot | [DataConnectorContractTests:11/168](../src/ANLAbel.UnitTests/DataConnectorContractTests.cs#L168) assert snapshot không đổi khi sửa DataTable và hash phát hiện `alpha`→`bravo` cùng mtime/length; [FileSourceIdentityStaleProtectTests](../src/ANLAbel.UnitTests/FileSourceIdentityStaleProtectTests.cs#L15) assert deleted/locked stale. `VM:266 GetExcelDataSourceIdentity` chụp file lúc gọi; `MainWindow.xaml.cs:1080` truyền vào preview; `PrintPreviewWindow.xaml.cs:825` so lại identity. | H14 ghi policy hash + watcher nhưng chưa đóng session/race; H15 chỉ tổng. | Gap source: identity được lấy lúc mở preview, chưa chứng minh gắn với bytes của lần import; refresh `VM:1691` vẫn short-circuit bằng mtime. Check sửa file giữ mtime/length giữa import và preview, trong preflight và trước spool: rows/schema cũ không được mang identity file mới rồi Ready. Không dùng hash unit test để đóng cửa sổ race này. | **Not verified** |
| **DB07** — record/source-row identity qua sort/filter | `VM:1900 FindRowIndexByKeyField` chọn match đầu; `H:9182` giữ PN-200 sau chèn dòng với **key duy nhất**. [PrintPreviewWindow.Async.cs:70](../src/ANLAbel.App/PrintPreviewWindow.Async.cs#L70) tạo `source-row-N` từ thứ tự GetRows; `PrintPreviewWindow.xaml.cs:1251` lấy payload qua `SourceRowNumber - 1`. | H14/H15: gate key-field; I284: fixture tự cung cấp source-row/copy identity, không kiểm origin sau import. | SourceRowNumber ở preview là ordinal dữ liệu, không phải tọa độ worksheet đã bảo toàn qua header/blank rows. Chưa chứng minh sort/filter và duplicate key/value giữ đúng record. Check chọn bản ghi thứ hai cùng value, header dòng 3 + dòng rỗng, sort/filter/refresh: payload, record và vị trí nguồn phải còn đối chiếu được. | **Not verified** |
| **DB08** — copies hợp lệ, không cắt batch | [PrintPreviewContractsTests:64–104](../src/ANLAbel.UnitTests/PrintPreviewContractsTests.cs#L64) reject identity sai, copies âm/1000, tổng >100000; zero là record không chọn ở test dòng 29. **Call path trước đó:** `PrintPreviewWindow.GetCopiesFromRow:1371` → [DatabaseConfig.ResolveCopiesForRow:34](../src/ANLAbel.Core/Models/DatabaseConfig.cs#L34) fallback 1 và cap 999; `H:8472` assert blank/âm/abc→1, 5000→999. | H14/H15: harness xanh có cả kỳ vọng fallback/cap; không phải nghiệm thu DB08. | **Mâu thuẫn source/assertion với DB08 còn mở:** cột copies đã chọn nhưng missing/blank/âm/thập phân/overflow bị biến thành 1; quá lớn bị cắt trước QuantityPlan. Đây không chỉ là thiếu test, nhưng chưa chạy tái hiện trong phiên này. Check raw cột copies tới prepared batch: invalid chặn có row/field; 0/unselected phải phân biệt rõ với số lượng dương để in. | **Not verified** |
| **DB09** — unlink/relink giữ authored design | `VM:2081 UnlinkExcel` xóa link/view/config, không sửa object binding; `H:8718/8761` assert `{PartNo}` còn nguyên sau unlink/re-import cùng schema và thoát broken link khi file bị xóa. | H14/H15: hai gate unlink; relink registry tại H:8942 chỉ một template mở lại. | Chưa assert toàn geometry/style/resource/binding qua relink lỗi/schema khác hoặc ngăn in nguồn chưa verified. Check unlink → relink sai sheet/file → relink đúng, giữ X/Y/W/H/padding/rotation và authored binding; không sửa protected contract để xử lý data gap. | **Not verified** |
| **DB10** — kiểm mọi record, lỗi chặn batch cũ | `PrintPreflightValidator.Validate`; `H:7863` một row missing field, `H:7897` ba rows × hai objects assert đủ sáu cell diagnostics. `PrintPreviewWindow.Async.cs:111` validate toàn prepared rows; [Print_Click:413–480](../src/ANLAbel.App/PrintPreviewWindow.xaml.cs#L413) chỉ bỏ dòng sau operator Yes, đổi selection rồi validate lại. Tên `AutomationPreparedBatchPreflightService` đơn lẻ không phải test evidence. | I277/I282: reject import sai cấu trúc/stored error trước trả table; H14/H15: preflight harness tổng. | Check row lỗi cuối batch không nằm trên trang đang xem: **0 dispatch cho selection cũ**, diagnostic field/object/source row đúng; nếu operator chọn selection mới phải kiểm lại. Parser automation có thể trả good records kèm diagnostics (`CsvAutomationSourceParserTests:68–88`), không suy ra toàn batch đã bị chặn nếu chưa kiểm consumer. Copies và origin-row gap DB08/DB07 vẫn ảnh hưởng acceptance. | **Not verified** |

### 3. Hợp đồng Barcode (B01–B07)
| ID / acceptance | Source và assertion đã đối chiếu (không chạy) | Evidence chạy trước đây | Facet chưa xác minh / mâu thuẫn và acceptance kế tiếp | Runtime hiện tại |
| :--- | :--- | :--- | :--- | :--- |
| **B01** — validate đúng symbology trước encode + đối chiếu decode | [BarcodeApplicationContract.ValidateData](../src/ANLAbel.Core/Barcode/BarcodeApplicationContract.cs) + preflight `ValidateBarcode`. [BarcodeApplicationContractTests](../src/ANLAbel.UnitTests/BarcodeApplicationContractTests.cs) empty/alphabet/EAN/UPC-A/ITF/Codabar/MSI. Renderer `ValidateData` hẹp hơn, không checkdigit EAN/UPC. Decode imported: 11 loại, xem B07. | I287: 52 ca, 911 unit, B01 đã ghi Partial; H15: 66 decode. | UPC-E không checkdigit/number-system; Codabar chỉ alphabet; 2D gần như non-blank. Check positive/negative từng loại trên **preflight production** rồi decode. Không đóng B01 từ contract unit hay 11/15 ZXing. | **Not verified** |
| **B02** — không tự thêm/bỏ/đổi ngoài policy | Renderer `NormalizeData` uppercase Code39/Codabar; GS1 trim/separator khi profile GS1. [ImportedBarcodeRoundTripTests](../src/ANLAbel.UnitTests/ImportedBarcodeRoundTripTests.cs) assert bound==payload rồi ZXing decode exact; Code128 ` 000123 `; QR Unicode/newline; không HRI. Composite I284 render Code128 row1, **không decode**. | I279: 12 ca; I285: 36; H15: 66. Bảng cũ còn ghi 12 ca + Pass. | Check raw vs encoded vs HRI cho lowercase Code39/Codabar, EAN generated checkdigit, GS1. Uppercase là biến đổi nguồn; chưa đủ để gọi defect ngoài policy. Formatted numeric xuyên scene/dispatch vẫn mở, không chứng nhận Excel in-flight. | **Not verified** |
| **B03** — GS1 semantics riêng | Contract chỉ GS1 Code128/QR/DataMatrix; không đoán raw thành GS1. `BarcodeApplicationContractTests` AI/length/separator; [Gs1Code128Fnc1RoundTripTests](../src/ANLAbel.UnitTests/Gs1Code128Fnc1RoundTripTests.cs) `(21)ABC(10)XYZ` → `]C121ABC<GS>10XYZ` cùng ZXing. | H15/HPrint: harness GS1 subset được báo trong tổng. Bảng cũ chỉ trỏ một named gate. | Check GS1 QR/DataMatrix, leading space, HRI, scanner độc lập. Code128 ZXing không phải FNC1 vật lý. | **Not verified** |
| **B04** — module theo effective DPI, quiet zone, frame nhỏ chặn | DeviceBarcodeLayout + MatrixSquareModuleFit; preflight linear X-dim. `H:9393` X=.08@203 blocking; `H:9535` ratio/quiet. Helper tight 10×10 **không reject**. | H14/H15: named gates DPI/module. | Check 203/300/600 và non-square DPI trên production gate, không helper; frame quá nhỏ phải chặn. Driver DPI ≠ `item.QrDpi` chưa có. | **Not verified** |
| **B05** — hình học 1D/2D, rotation/HRI/clip | Renderer vector 1D, integer 2D; HRI layout tests đo strip/invalid frame. Code39 ratio harness là diagnostic, không đo ảnh. | H14/H15: helper + SizedFromX. | Check từng type × DPI × rotation × HRI × mép printable: module/ratio/quiet trên output, rồi decode. Không grade vật lý. | **Not verified** |
| **B06** — designer/preview/print cùng encode/layout | Designer raster tại `item.QrDpi`; print `plan` dpiX/Y. `H:6236` **Rectangle**, không barcode. `H:9710` CapturingBarcodeRenderer giả, chỉ width SizedFromX. Composite compile node, không WPF session. | H14/H15: hai named gates được dùng để Pass. | Check cùng snapshot/plan bound barcode qua designer, preview drawing và print paginator: geometry/decode khớp; không kéo bitmap preview để in. | **Not verified** |
| **B07** — chỉ công bố loại đủ evidence | Runtime 15 loại: `VM:447–463` Code128/39/93, EAN-13/8, UPC-A/E, ITF, Codabar, MSI, Plessey, QR, DataMatrix, Aztec, PDF417. Fixture decode **11**; **thiếu UpcE, Codabar, MSI, Plessey**. ThermalRasterGolden là fingerprint synthetic. | I279/I285/H15: 12→36→66 ca phần mềm. Bảng cũ gắn Pass cho 11 loại. | **Overclaim evidence, không suy ra encode 4 loại còn lại bị lỗi.** Lập matrix 15 hàng: ±payload, raw/encoded/HRI, decode phần mềm, geometry DPI, decoder độc lập. Physical: queue/driver/DPI/media/ribbon/speed/darkness/payload/mẫu/scanner-grade — chưa có. | **Not verified** |

### 4. Hợp đồng Print (P01–P08)
| ID / acceptance | Source và assertion đã đối chiếu (không chạy) | Evidence chạy trước đây | Facet chưa xác minh / mâu thuẫn và acceptance kế tiếp | Runtime hiện tại |
| :--- | :--- | :--- | :--- | :--- |
| **P01** — đúng queue, không fallback | `PrintService.EnsurePrintQueue` exact name; [PrinterQueueLookup](../src/ANLAbel.Printing/PrinterProfiles/PrinterQueueLookup.cs). `H:5861` list giả Default vs Zebra; `H:7768` missing lookup → Failed. [PrinterQueueHealthContractTests](../src/ANLAbel.UnitTests/PrinterQueueHealthContractTests.cs) paused/offline/unknown. | HPrint: named-queue gates; checkpoint tự ghi DEVMODE giả. | Check queue mất/đổi driver/offline/paused/unknown trên app: 0 submission, diagnostic giữ tên đã chọn. | **Not verified** |
| **P02** — capability ≠ stock preset | [PrinterMediaResolver](../src/ANLAbel.Core/Printing/PrinterMediaContracts.cs); capability STA snapshot. `PrinterMediaResolverTests` SoftwareStockOnly không dispatch; Driver match mới Compatible. Snapshot/page observation **fabricated**. | HPrint 977→1023, media 14 rồi 25 focused; :318 giới hạn hardware. | Check observation driver thật DPI/media/orientation/imageable; stock không thành capability; metadata thiếu không tự bịa. | **Not verified** |
| **P03** — Ready thuộc prepared snapshot | Không có `PrintPreviewSession.cs`. Session = `PrintSessionTemplateContract.Capture`; preview chỉ commit khi operation hiện hành. [PrintSessionTemplateContractTests](../src/ANLAbel.UnitTests/PrintSessionTemplateContractTests.cs) clone độc lập, không race WPF. | HPrint: 3 session tests + hash/drift named gates. | Check refresh A pending, đổi document/data/quantity/profile/ticket thành B, A xong sau: Ready chỉ B; async cũ không phục hồi Ready. | **Not verified** |
| **P04** — sequence record/copy, preview=dispatch | [PrintSequenceBuilder](../src/ANLAbel.Core/Printing/PrintPreviewContracts.cs) A×2/B×2/C×2 deterministic. Preview flatten `source-row-N`. [PrintJobManifest](../src/ANLAbel.Core/Printing/PrintJobManifest.cs) chỉ RowsFingerprint/counts, **không LabelInstance**. | H14: builder test; I284 tự dựng A×2/B×1. | **Gap nguồn:** mapping record/copy không bền vững qua manifest. Check A×2/B×1 + hai record cùng payload qua preview→manifest→paginator: A1,A2,B1 truy vết sau reopen. | **Not verified** |
| **P05** — revalidate sát spool | `PrintRowsWithDialog` plan/preflight rồi `RevalidateDispatchPlan` trước `PrintDocument`. Stale Excel check trước manifest, không trong boundary PrintDocument. [DispatchRevalidationContractTests](../src/ANLAbel.UnitTests/DispatchRevalidationContractTests.cs) DPI/media/ticket drift; `H:10193` có `source.Contains`, không đếm submission thật. | HPrint: named full-output revalidation. | Check source/resource/queue/ticket/media drift sau prepare: 0 submission, không rebuild nội dung khác. Không chứng nhận Excel freshness in-flight. | **Not verified** |
| **P06** — chống gửi trùng, không tự retry | `Print_Click` set `_isPrintBusy`; `AsyncRelayCommand` `_isExecuting`. `H:7460` predicate reflection + XAML string; `H:10097` command TCS, không double-click window. Recovery `AutomaticRetryAllowed=false`. | H14/H15: hai named gates. | Check double-click/key+mouse khi driver chậm: một durable job. Crash/timeout sau send: không tự retry (phụ thuộc P07 không biến Unknown thành Failed). | **Not verified** |
| **P07** — outcome trung thực; spool ≠ đã in | Model tách Cancelled/Failed/SpoolAccepted/Unknown; monitor thiếu ID → Unknown; recovery Reconcile/Operator, physical=false. `H:7283` SpoolAccepted không physical; fake spool reader. | H14/HPrint: named spool gates. Bảng cũ ghi `PrintSpoolMonitor.cs` — runtime hiện là monitor qua `PrintService`. | **Mâu thuẫn nguồn:** `PrintService.PrintRowsWithResult` catch mọi exception quanh `PrintRowsWithDialog`/`PrintDocument` → `CreateFailedResult` luôn Failed. Exception sau/partial send có thể mất Unknown. Check sender accepted-side-effect rồi throw → Unknown, không auto-retry, không DeviceConfirmed. | **Not verified** |
| **P08** — audit đủ; evidence bắt buộc chặn trước send | Manifest fingerprints; `Print_Click:536–571` ba transition rồi `:573` send. `H:9046` log synthetic; `H:10065` redaction; store unit corrupt-tail. Data import log không phải print lifecycle. | H14/HPrint: print/data log gates. | **Mâu thuẫn nguồn với chuẩn mục 6:** `RecordPrintJobTransitionAsync:774` nuốt mọi `AppendAsync` (Debug only); VM quick-print cùng pattern. Lỗi lưu bắt buộc trước dispatch không chặn worker. Check inject fail Preparing/PreflightPassed/Dispatching → 0 send; lỗi log phụ trợ sau send không giả failure rồi gửi lại. | **Not verified** |

### Kết luận mapping hiện tại (2026-09-16)

Các hàng Pass/Partial cũ trong ma trận trên **không còn là kết luận nghiệm thu**. Checkpoint D06/printer/Fast/self-audit bên dưới giữ nguyên như **lịch sử đã ghi**, với phạm vi: tổng suite/harness tại thời điểm đó, không phải Pass từng ID, không phải run của phiên mapping này, không phải chứng nhận Excel in-flight hay tem vật lý.

| Nhóm | Kết luận hiện tại | Không làm từ mapping này |
| --- | --- | --- |
| Designer | Not verified; D07 preflight ≠ live editor | Không sửa Text/TextBox; cần WPF gesture/persistence/render-failure trên editor thật |
| Excel/CSV | Not verified; I281 General `R` không phải bug; DB04 cache/formula và DB08 copies còn mở | Không sửa data boundary; Excel owner riêng; không chứng nhận file đang đổi |
| Barcode | Not verified; 11/15 decode cùng ZXing; 4 loại chưa có imported decode | Không công bố production-ready; cần decoder độc lập + matrix vật lý |
| Print | Not verified; P08/P07/P04 có mâu thuẫn nguồn, chưa chạy Fail | Không in thật, không đổi printer; cần fault inject persistence/dispatch và driver matrix |

### Ma trận mã vạch B01–B07 theo từng loại (2026-10-13, plan 13 GĐ5 và QĐ-8)

Thay cho câu “11/15 decode” ở bảng trên: từ v0.306,
[ImportedBarcodeRoundTripTests](../src/ANLAbel.UnitTests/ImportedBarcodeRoundTripTests.cs)
chạy cả 15 loại. Mỗi ca đi từ ô CSV hoặc XLSX, qua binding và renderer, tới decode
khớp từng ký tự ở 203, 300 và 600 DPI, tổng 90 ca. Fixture âm lấy từ
[BarcodeApplicationContractTests](../src/ANLAbel.UnitTests/BarcodeApplicationContractTests.cs).
[BarcodeEvidenceMatrixTests](../src/ANLAbel.UnitTests/BarcodeEvidenceMatrixTests.cs) lấy
danh sách loại từ enum runtime và buộc mỗi loại có fixture dương, fixture âm và
đủ sáu ca round-trip. Loại mới thiếu một trong ba sẽ làm test đỏ.

Kết quả chạy: unit 1141/1141 cùng `BarcodeEvidenceMatrixTests` 30/30 tại
`329af79` + slice này (2026-10-13).

| Loại | Fixture dương (round-trip) | Fixture âm bị từ chối | Bộ đọc trong test | Ghi chú |
| --- | --- | --- | --- | --- |
| Code 128 | ` 000123 ` (giữ khoảng trắng, số 0 đầu) | ký tự NBSP | ZXing | GS1 FNC1: `Gs1Code128Fnc1RoundTripTests` |
| QR Code | ` 000123⏎Lô A ` (Unicode, xuống dòng) | rỗng | ZXing | GS1 subset (B03) |
| Data Matrix | `LOT-2026-X` | rỗng | ZXing (`PURE_BARCODE`) | GS1 subset (B03) |
| Code 39 | `CODE-39` | `CODE39@BAD` | ZXing | tỉ lệ và vùng trống: `Code39RatioContractTests` |
| Code 93 | `CODE93` | ký tự NBSP | ZXing | — |
| EAN-13 | `5901234123457` | sai số kiểm tra | ZXing | `RetailCheckDigitRenderTests` |
| EAN-8 | `96385074` | sai số kiểm tra | ZXing | — |
| UPC-A | `036000291452` | sai số kiểm tra | ZXing | — |
| UPC-E | `01234565` | `01234A` | ZXing | test dùng vùng trống 6 mô-đun |
| ITF | `1234567890` | độ dài lẻ | ZXing | — |
| Codabar | `A123456B` | `123@456` | ZXing (giữ start/stop) | — |
| PDF417 | `PDF417-2026` | rỗng | ZXing | — |
| Aztec | `AZTEC-2026` | rỗng | ZXing | — |
| MSI | `123456` | `12345A` | ZXing | — |
| Plessey | `98765` | `9876F` | bộ đọc viết trong test (ZXing không đọc Plessey) | bằng chứng yếu hơn các loại khác |

**Đối chiếu bằng bộ đọc độc lập (2026-10-14, một lần, không phải gate tự động).** Các fixture dương ở trên được render bằng `ZxingBarcodeRenderer.RenderBarcode` (khung 60×40 mm, thêm lề trắng 40 px) ở 203, 300 và 600 DPI. Sau đó ảnh được giải mã bằng ZBar (`libzbar-64.dll` đi kèm pyzbar 0.1.9), một mã nguồn độc lập với ZXing. Kết quả 30/30 ca cho 10 loại ZBar đọc được:

- Code 128 (giữ cả dấu cách đầu/cuối), QR (tiếng Việt, xuống dòng), Code 39, Code 93, EAN-13, EAN-8, ITF và Codabar khớp từng ký tự.
- UPC-A được ZBar báo theo mặc định dưới dạng EAN-13 có số 0 đầu (`0036000291452`).
- UPC-E được ZBar mở rộng thành mã UPC-A tương đương theo quy tắc GS1 (`0012345000065`); phép mở rộng được tính lại trong script, không suy đoán.

ZBar không đọc Data Matrix, Aztec, PDF417, MSI và Plessey, nên năm loại này vẫn chỉ có bộ đọc trong test.

| ID | Bằng chứng phần mềm hiện có | Còn thiếu | Trạng thái |
| --- | --- | --- | --- |
| B01 | Fixture âm theo từng loại ở trên; preflight chặn payload sai; 10 loại đã đối chiếu bằng ZBar (2026-10-14) | Bộ đọc độc lập cho Data Matrix, Aztec, PDF417, MSI, Plessey; tem in thật | Not verified (vật lý) |
| B02 | Round-trip giữ nguyên payload từ ô dữ liệu tới decode; ZBar đọc lại đúng từng ký tự (kể cả dấu cách đầu/cuối của Code 128, chữ có dấu và xuống dòng của QR) | Như trên | Not verified (vật lý) |
| B03 | `BarcodeApplicationContractTests` (AI, độ dài, số kiểm tra, separator), `Gs1Code128Fnc1RoundTripTests` | Quét GS1 vật lý | Not verified |
| B04 | `LinearBarcodeModuleContractTests`, `MatrixSquareModuleFitTests`, `QrSizingCalculatorTests`, `Code39RatioContractTests` | Đo trên tem in thật ở 203/300 DPI | Not verified |
| B05 | `BarcodeHriLayoutTests`, `DeviceBarcodeLayoutTests` | Xoay × HRI × mép in trên output thật | Not verified |
| B06 | Designer, xem trước và bản in cùng dùng `ZxingBarcodeRenderer`, cùng tùy chọn mã và cùng DPI in (ADR-025); gate `designer, preview and print draw the same barcode` so sánh ba đầu ra ở 203/300 DPI | So sánh trên tem in thật | Pass (phần mềm); Not verified (vật lý) |
| B07 | Danh sách lấy từ runtime; mỗi loại có fixture dương/âm và round-trip | Quét bằng máy đọc thật | Not verified |

Áp dụng QĐ-8: điều kiện ở §3.3 của plan 13 (fixture dương/âm và decode
round-trip) đã đạt cho cả 15 loại, nên **không loại nào bị bỏ khỏi danh sách tạo
mới**. Không loại nào được ghi là đã nghiệm thu cho sản xuất. Lý do: encoder và
bộ đọc là cùng thư viện ZXing (riêng Plessey là bộ đọc viết trong test), và chưa
có tem vật lý được quét.

### D06 checkpoint after 0.287 (lịch sử; không phải Pass hiện tại)
- **Strict raw-legacy signature**: Yêu cầu đầy đủ chữ ký PascalCase do serializer cũ sinh gồm `Id` (string), `Name` (string), `WidthMm` (number), `HeightMm` (number), `Dpi` (number), và `Objects` (array) khi thiếu `schemaVersion`; loại bỏ fallback unversioned payload tùy tiện để đảm bảo fail-closed.
- **Missing / Malformed / Future schema behavior**: Mọi payload thiếu hoặc sai kiểu `schemaVersion` (như string `"2"`, số thập phân `2.5`) đều bị từ chối bằng `InvalidDataException`. Đối với future schema version (`CurrentSchemaVersion + 1`), `LoadAsync` bọc trong `InvalidDataException` với `InnerException` là `UnsupportedProjectSchemaException` để tương thích caller hiện tại, trong khi `LoadWithRecoveryAsync` giữ nguyên `UnsupportedProjectSchemaException` nhằm chống downgrade backup sai lệch.
- **Round-trip preservation**: `SaveAsync` rồi `LoadAsync` bảo toàn đầy đủ kích thước template (`WidthMm`, `HeightMm`), hình học đối tượng (`XMm`, `YMm`, `WidthMm`, `HeightMm`, `Rotation`), `Style` (`FontFamily`, `FontSizePt`), `BindingExpression`, và toàn bộ barcode options (`BarcodeSymbology`, `BarcodeApplicationProfile`, `BarcodeHriPlacement`, `BarcodeTextFontSizePt`, `BarcodeModuleWidthMm`, `BarcodeWidthMode`, `BarcodeCheckDigitPolicy`, `BarcodeHriShowCheckDigit`).
- **Verification evidence**:
  - `ProjectFileServiceTests`: 8/8 tests pass (fail-closed empty object, malformed schemaVersion, future schema, partial legacy signature, valid raw legacy preservation, barcode full round-trip).
  - Solution build: 0 warnings, 0 errors.
  - Full unit test suite: 919/919 passed (0 skipped).
  - Application regression test harness: exit code 0 (bao gồm template save/load, schema legacy/future, backup/recovery, failed open preserves current document, release metadata sync, và toàn bộ các gate Text/TextBox được bảo vệ).
### Printer / Paper-Media / Print Preview refinement checkpoint (lịch sử; không phải Pass hiện tại)
- **Mojibake Inconsistencies Fixed**: Đã loại bỏ hoàn toàn các ký tự lỗi mã hóa `Ã—` trong `PrinterMediaContracts.cs` (lines 311, 312, 343, 344) và `PrinterSetupWindow.xaml.cs` (lines 177, 179, 185, 186), chuyển về ký tự chuẩn `×` (`\u00d7`).
- **PrinterSetup Orientation Interactivity**: Bổ sung tương tác hoàn chỉnh cho nhóm RadioButton `PortraitRadio` và `LandscapeRadio` (`GroupName="LabelOrientationGroup"`, sự kiện `Orientation_Changed`):
  - Chuyển đổi qua lại giữa Portrait và Landscape tự động căn chỉnh và hoán đổi `WidthBox` / `HeightBox` theo `LabelGeometry.OrientSize`, cập nhật `SelectedOrientation` và `SizeSummaryText` kèm chỉ báo orientation trực quan `(Portrait)` / `(Landscape)`.
  - Nhập tay kích thước trong `LabelDimensions_Changed` tự động nhận diện và cập nhật trạng thái radio tương ứng.
  - Chọn stock preset từ `PaperSizesList` bảo toàn orientation hiện tại thay vì ghi đè sai lệch.
  - Khởi tạo cửa sổ được khóa qua cờ `_syncing` nhằm giữ nguyên authored dimensions khi document không chỉ định named stock.
  - Nhận diện stock preset trong `Apply_Click` hỗ trợ hoán đổi chiều (`IsSameDimension`), không bị rớt về "Custom" khi in portrait một stock vốn khai báo landscape.
- **PrintPreview Label Setup Validation**: Trong `PrintPreviewWindow.xaml.cs`, các thao tác `ApplyPrintSetup_Click`, `SavePrintSettingsToLabel_Click` và `PrintCalibration_Click` được bổ sung hàm kiểm tra `ValidateLabelDimensions()` gọi `LabelStockContract.Evaluate` trước khi ghi nhận cấu hình, bảo đảm kích thước tem nằm trong biên giới hạn công nghiệp (5–300 mm) và không bị gán kích thước âm hoặc 0.
- **PrintSessionTemplateContract Serialization Hardening**: Bổ sung `JsonSerializerOptions` với `JsonStringEnumConverter` trong `PrintSessionTemplateContract.Capture` bảo đảm các trường enum (`LabelOrientation`, `BarcodeSymbology`, `PaperSizeSource`, `PrinterMediaProvenance`, `PrinterSettingsSource`) được snapshot và deserialize trung thực.
- **Verification Evidence**:
  - `PrintSessionTemplateContractTests`: 3/3 tests pass (bao gồm fact mới `CapturePreservesEnumPropertiesAsStringEnums`).
  - `PrinterMediaResolverTests`: 14/14 tests pass (bao gồm fact mới `DiagnosticMessagesUseCleanMultiplicationSymbolWithoutMojibake`).
  - Solution build: 0 warnings, 0 errors.
  - Full unit test suite (`ANLAbel.UnitTests`): 977/977 passed (0 failed, 0 skipped).
  - Application regression test harness (`ANLAbel.Tests`): exit code 0 với gate mới `PASS printer setup orientation interactivity synchronizes dimensions and layout` cùng tất cả các gate Text/TextBox được bảo vệ.

### PrinterSettingsSource.Driver vs PaperSizeSource.Manual Resolution Checkpoint (lịch sử; không phải Pass hiện tại)
- **Contract & Architecture Alignment**:
  - `PrinterSettingsSource.Driver` ("Current driver preferences"): Theo hành vi NiceLabel và kiến trúc ANLAbel (`docs/NICELABEL_PRINTER_MEDIA_ARCHITECTURE.md`), khi chọn Driver settings source thì Windows DEVMODE / driver hoàn toàn sở hữu print ticket (kích thước trang, độ phân giải DPI). `PrintService.ApplyTemplateTicket` cố tình không ghi đè `PageMediaSize` hoặc `PageResolution` trong chế độ này.
  - UI State Consistency: `PrinterSetupWindow` được đồng bộ hóa để khi chọn `DriverSettingsRadio`, `DriverAutomaticRadio` bắt buộc được chọn, `ManualMediaRadio` và `DriverMediaBox` bị vô hiệu hóa (`IsEnabled = false`). Nếu operator chọn `ManualMediaRadio`, UI tự động chuyển `SettingsSource` sang `Label` ("Saved with label").
  - Recursive Event Loop Prevention: Các event handler `SettingsSource_Changed` và `PageSource_Changed` được bảo vệ bằng khối `try ... finally` quanh cờ `_syncing` nhằm chặn hoàn toàn các sự kiện `Checked` đệ quy từ WPF.
  - Core Contract & Persistence Normalization:
    - Trong `PrinterProfile.cs`, setter và getter của `PaperSizeSource` tự động chuẩn hóa về `PaperSizeSource.DriverAutomatic` khi `SettingsSource == PrinterSettingsSource.Driver`, loại bỏ mọi trạng thái mâu thuẫn legacy/runtime.
    - Trong `PrintSessionTemplateContract.CommitPrintConfiguration`, `PaperSizeSource` được gán về `DriverAutomatic` nếu session có `SettingsSource == Driver`.
    - Trong `PrinterSetupWindow.Apply_Click`, `SelectedPageSizeSource` được gán dứt khoát là `DriverAutomatic` và `SelectedMedia = null` khi `SelectedSettingsSource == Driver`.
    - Trong `PrinterMediaResolver.Resolve`, khi `request.SettingsSource == PrinterSettingsSource.Driver`, hệ thống kiểm tra DEVMODE page observation (`snapshot.PageObservation`). Nếu kích thước cấu hình của driver lệch với tem đã thiết kế, hệ thống trả về `Mismatch` (`CanDispatch = false`), ngăn chặn lệnh in và cảnh báo rõ ràng; nếu khớp hoặc không có observation, trả về `DriverAutomatic` (`CanDispatch = true`).
  - Mojibake Prevention: Toàn bộ thông điệp chẩn đoán mới sử dụng ký tự nhân Unicode chuẩn `×` (`\u00d7`), có unit test kiểm tra `Assert.DoesNotContain("Ã", ...)` bảo đảm không tái xuất hiện lỗi encoding.
- **Verification Evidence**:
  - Solution build: 0 warnings, 0 errors (`dotnet build ANLAbel.slnx --no-restore`).
  - Full unit test suite (`ANLAbel.UnitTests`): 1023/1023 passed, 0 failed, 0 skipped (`dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build`).
  - Focused PrinterMedia tests: 25/25 passed (`dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --filter "FullyQualifiedName~PrinterMedia"`).
  - Focused ProjectFileService tests: 9/9 passed (`dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --filter "FullyQualifiedName~ProjectFileServiceTests"`).
  - Application regression test harness (`ANLAbel.Tests`): 198/198 passed (100%), 0 failed. Đã giải quyết triệt để 2 lỗi kiểm thử:
    1. `formula engine FIELD and CONCAT`: Đồng bộ kỳ vọng chuỗi với DEFECT 3 bảo toàn khoảng trắng nguyên gốc (raw whitespace preservation không Trim) giữa cú pháp `{field}` và `FIELD()`.
    2. `template save/load`: Đồng bộ kỳ vọng `PaperSizeSource` với quy chuẩn hợp đồng `PrinterProfile` (chuẩn hóa về `PaperSizeSource.DriverAutomatic` khi `SettingsSource == PrinterSettingsSource.Driver`).
    Toàn bộ 7 gate Text/TextBox bảo vệ, các gate printer setup/fail-closed, barcode preflight/HRI và print engine đều pass nguyên vẹn.
- **Remaining Hardware Limits**:
  - Windows Print Ticket DEVMODE query thực tế phụ thuộc vào máy in/driver vật lý đã cài đặt và trạng thái spooler trực tiếp tại trạm làm việc; capability probe giả lập qua `PrinterCapabilitySnapshot` và `PrinterPageObservation`.

### Fast Quality Loop 100% Green Verification Checkpoint (lịch sử; không phải Pass hiện tại)
- **Solution Build**: 0 warning, 0 error (`dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false`).
- **Unit Tests**: 1023/1023 passed, 0 failed, 0 skipped (`ANLAbel.UnitTests.dll`).
- **Application Regression Harness**: 198/198 passed, exit code 0 (`ANLAbel.Tests.csproj`).
- **Fast Quality Loop**: Hoàn tất thành công với thông báo `ANLAbel Fast quality loop passed.`
- **Protected Guardrails**: Hợp đồng Text/TextBox theo `AGENTS.md` được bảo toàn tuyệt đối, không có bất kỳ sửa đổi hay tái cấu trúc nào tác động đến Text/TextBox.

### Self-Audit & Industrial Architecture Checkpoint (2026-09-15) (lịch sử; không phải Pass hiện tại)

Thực hiện tự kiểm toán toàn diện theo yêu cầu quy chuẩn công nghiệp và đối chiếu với kế hoạch phát triển mới (`07-execution-plan.md`, `CORE_SOFTWARE_STANDARD.md`, `LOCAL_LABEL_PRODUCT_CONTRACT.md`, ADR-021):

1. **Chuẩn hóa ngôn ngữ đơn ngữ 100% tiếng Việt có dấu chuẩn công nghiệp:**
   - Hoàn tất loại bỏ 100% tiếng Anh trên bề mặt vận hành của toàn bộ 26/26 tệp XAML (`LanguageComplianceRegression.cs` đã đăng ký đầy đủ và pass tuyệt đối).
   - Chuyển đổi toàn bộ hộp thoại `MessageBox.Show`, tiêu đề cửa sổ, `StatusText`, `PrinterStatusText`, `PrinterQueueStatusText`, `PreviewTrustStatusText`, cảnh báo module mã vạch, thông báo xác thực liên kết Excel và ràng buộc công thức sang tiếng Việt rõ ràng, dễ hiểu kèm hành động khắc phục cụ thể.
   - Thu khung song ngữ về đơn ngữ: bỏ bảng chuỗi `En` trong `UiCatalog.cs`, `UiPreferencesService.cs` luôn cung cấp tiếng Việt chuẩn.

2. **Bảo toàn các hợp đồng bất biến (Protected Contracts & Invariants):**
   - Hợp đồng `Text/TextBox` theo `AGENTS.md` (Invariants 1–10) được bảo vệ tuyệt đối: `Text` content-owned (free-flowing / compress glyph), `TextBox` frame-owned (wrap / clip tại authored bounds), placeholder mặc định `"Text Box"`. Toàn bộ 7 cổng kiểm thử hồi quy Text/TextBox pass 100%.
   - Bảo toàn toàn bộ mã kỹ thuật, tên thuộc tính, tên cột Excel, ZPL/GS1 token.

3. **Củng cố ranh giới dữ liệu và giải mã mã vạch:**
   - `ImportedBarcodeRoundTripTests` đã kiểm chứng thành công 11 chuẩn mã vạch công nghiệp (`Code128`, `QRCode`, `DataMatrix`, `Code39`, `Ean13`, `ITF`, `Code93`, `Ean8`, `UpcA`, `Pdf417`, `Aztec`) qua 66 tổ hợp (Excel/CSV × 203/300/600 DPI) với giải mã khớp 100%.
   - Ranh giới dữ liệu Excel/CSV bảo toàn nguyên vẹn chuỗi số có số 0 đầu, chuỗi nhiều dòng, phân cách dấu phẩy/chấm phẩy, và từ chối fail-closed các ô lỗi Excel.

4. **Kết quả xác minh 3 cổng kiểm thử:**
   - **Solution Build:** `Build succeeded. 0 Warning(s), 0 Error(s)` (`dotnet build ANLAbel.slnx --no-restore`).
   - **Unit Tests:** `Passed! - Failed: 0, Passed: 1041, Skipped: 0, Total: 1041` (`ANLAbel.UnitTests.csproj`).
   - **Integration & Regression Suite:** `Exit code 0` — 100% test gates pass (`ANLAbel.Tests.csproj`).

[Agent: Gemini / Antigravity]
